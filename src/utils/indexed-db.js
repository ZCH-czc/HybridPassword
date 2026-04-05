const DB_NAME = "password-manager-vault";
const DB_VERSION = 1;
const META_STORE = "vaultMeta";
const ENTRY_STORE = "passwordEntries";
const VAULT_CONFIG_KEY = "vault-config";
const APP_SETTINGS_KEY = "app-settings";

let dbPromise = null;

function cloneForIndexedDb(value) {
  if (value == null) {
    return value;
  }

  // IndexedDB 不能直接保存 Vue 的响应式代理，这里统一转成纯数据。
  return JSON.parse(JSON.stringify(value));
}

function ensureIndexedDb() {
  if (!globalThis.indexedDB) {
    throw new Error("当前 WebView 不支持 IndexedDB，无法保存密码数据。");
  }
}

function requestToPromise(request) {
  return new Promise((resolve, reject) => {
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error || new Error("IndexedDB 请求失败。"));
  });
}

function transactionToPromise(transaction) {
  return new Promise((resolve, reject) => {
    transaction.oncomplete = () => resolve();
    transaction.onerror = () => reject(transaction.error || new Error("IndexedDB 事务失败。"));
    transaction.onabort = () => reject(transaction.error || new Error("IndexedDB 事务已中止。"));
  });
}

export async function openVaultDatabase() {
  ensureIndexedDb();

  if (!dbPromise) {
    dbPromise = new Promise((resolve, reject) => {
      const request = globalThis.indexedDB.open(DB_NAME, DB_VERSION);

      request.onupgradeneeded = () => {
        const database = request.result;

        if (!database.objectStoreNames.contains(META_STORE)) {
          database.createObjectStore(META_STORE, { keyPath: "key" });
        }

        let entryStore;
        if (!database.objectStoreNames.contains(ENTRY_STORE)) {
          entryStore = database.createObjectStore(ENTRY_STORE, { keyPath: "id" });
        } else {
          entryStore = request.transaction.objectStore(ENTRY_STORE);
        }

        if (!entryStore.indexNames.contains("usernameNormalized")) {
          entryStore.createIndex("usernameNormalized", "usernameNormalized", { unique: false });
        }

        if (!entryStore.indexNames.contains("updatedAt")) {
          entryStore.createIndex("updatedAt", "updatedAt", { unique: false });
        }
      };

      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error || new Error("打开 IndexedDB 失败。"));
    });
  }

  return dbPromise;
}

export async function getVaultConfigRecord() {
  const database = await openVaultDatabase();
  const transaction = database.transaction(META_STORE, "readonly");
  const store = transaction.objectStore(META_STORE);
  const result = await requestToPromise(store.get(VAULT_CONFIG_KEY));
  await transactionToPromise(transaction);
  return result || null;
}

export async function getMetaRecord(key) {
  const database = await openVaultDatabase();
  const transaction = database.transaction(META_STORE, "readonly");
  const store = transaction.objectStore(META_STORE);
  const result = await requestToPromise(store.get(key));
  await transactionToPromise(transaction);
  return result || null;
}

export async function saveMetaRecord(key, value) {
  const database = await openVaultDatabase();
  const transaction = database.transaction(META_STORE, "readwrite");
  const store = transaction.objectStore(META_STORE);

  store.put(
    cloneForIndexedDb({
      key,
      ...value,
    })
  );

  await transactionToPromise(transaction);
}

export async function saveVaultConfigRecord(config) {
  await saveMetaRecord(VAULT_CONFIG_KEY, config);
}

export async function getAppSettingsRecord() {
  const result = await getMetaRecord(APP_SETTINGS_KEY);
  return result || null;
}

export async function saveAppSettingsRecord(settings) {
  await saveMetaRecord(APP_SETTINGS_KEY, settings);
}

export async function listPasswordRecords() {
  const database = await openVaultDatabase();
  const transaction = database.transaction(ENTRY_STORE, "readonly");
  const store = transaction.objectStore(ENTRY_STORE);
  const result = await requestToPromise(store.getAll());
  await transactionToPromise(transaction);
  return Array.isArray(result) ? result : [];
}

export async function putPasswordRecord(record) {
  const database = await openVaultDatabase();
  const transaction = database.transaction(ENTRY_STORE, "readwrite");
  const store = transaction.objectStore(ENTRY_STORE);
  store.put(cloneForIndexedDb(record));
  await transactionToPromise(transaction);
}

export async function putPasswordRecords(records) {
  const database = await openVaultDatabase();
  const transaction = database.transaction(ENTRY_STORE, "readwrite");
  const store = transaction.objectStore(ENTRY_STORE);

  records.forEach((record) => {
    store.put(cloneForIndexedDb(record));
  });

  await transactionToPromise(transaction);
}

export async function replaceVaultSnapshotData(config, records) {
  const database = await openVaultDatabase();
  const transaction = database.transaction([META_STORE, ENTRY_STORE], "readwrite");
  const metaStore = transaction.objectStore(META_STORE);
  const entryStore = transaction.objectStore(ENTRY_STORE);

  metaStore.put(
    cloneForIndexedDb({
      key: VAULT_CONFIG_KEY,
      ...config,
    })
  );

  entryStore.clear();

  records.forEach((record) => {
    entryStore.put(cloneForIndexedDb(record));
  });

  await transactionToPromise(transaction);
}

export async function deletePasswordRecord(recordId) {
  const database = await openVaultDatabase();
  const transaction = database.transaction(ENTRY_STORE, "readwrite");
  const store = transaction.objectStore(ENTRY_STORE);
  store.delete(recordId);
  await transactionToPromise(transaction);
}
