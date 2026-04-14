const DB_NAME = "password-manager-vault";
const DB_VERSION = 3;
const META_STORE = "vaultMeta";
const ENTRY_STORE = "passwordEntries";
const PASSKEY_STORE = "passkeyRecords";
const VAULT_CONFIG_KEY = "vault-config";
const APP_SETTINGS_KEY = "app-settings";
const PENDING_IMPORT_REVIEW_KEY = "pending-import-review";
const APP_LOGS_KEY = "app-logs";

let dbPromise = null;

function cloneForIndexedDb(value) {
  if (value == null) {
    return value;
  }

  return JSON.parse(JSON.stringify(value));
}

function ensureIndexedDb() {
  if (!globalThis.indexedDB) {
    throw new Error("IndexedDB is not available in the current runtime.");
  }
}

function requestToPromise(request) {
  return new Promise((resolve, reject) => {
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error || new Error("IndexedDB request failed."));
  });
}

function transactionToPromise(transaction) {
  return new Promise((resolve, reject) => {
    transaction.oncomplete = () => resolve();
    transaction.onerror = () =>
      reject(transaction.error || new Error("IndexedDB transaction failed."));
    transaction.onabort = () =>
      reject(transaction.error || new Error("IndexedDB transaction was aborted."));
  });
}

function ensurePasswordEntryStore(database, upgradeTransaction) {
  let entryStore;
  if (!database.objectStoreNames.contains(ENTRY_STORE)) {
    entryStore = database.createObjectStore(ENTRY_STORE, { keyPath: "id" });
  } else {
    entryStore = upgradeTransaction.objectStore(ENTRY_STORE);
  }

  if (!entryStore.indexNames.contains("usernameNormalized")) {
    entryStore.createIndex("usernameNormalized", "usernameNormalized", { unique: false });
  }

  if (!entryStore.indexNames.contains("updatedAt")) {
    entryStore.createIndex("updatedAt", "updatedAt", { unique: false });
  }

  if (!entryStore.indexNames.contains("deletedAt")) {
    entryStore.createIndex("deletedAt", "deletedAt", { unique: false });
  }
}

function ensurePasskeyStore(database, upgradeTransaction) {
  let passkeyStore;
  if (!database.objectStoreNames.contains(PASSKEY_STORE)) {
    passkeyStore = database.createObjectStore(PASSKEY_STORE, { keyPath: "id" });
  } else {
    passkeyStore = upgradeTransaction.objectStore(PASSKEY_STORE);
  }

  if (!passkeyStore.indexNames.contains("rpIdNormalized")) {
    passkeyStore.createIndex("rpIdNormalized", "rpIdNormalized", { unique: false });
  }

  if (!passkeyStore.indexNames.contains("username")) {
    passkeyStore.createIndex("username", "username", { unique: false });
  }

  if (!passkeyStore.indexNames.contains("deletedAt")) {
    passkeyStore.createIndex("deletedAt", "deletedAt", { unique: false });
  }

  if (!passkeyStore.indexNames.contains("updatedAt")) {
    passkeyStore.createIndex("updatedAt", "updatedAt", { unique: false });
  }

  if (!passkeyStore.indexNames.contains("nativeProviderRecordId")) {
    passkeyStore.createIndex("nativeProviderRecordId", "nativeProviderRecordId", {
      unique: false,
    });
  }
}

export async function openVaultDatabase() {
  ensureIndexedDb();

  if (!dbPromise) {
    dbPromise = new Promise((resolve, reject) => {
      const request = globalThis.indexedDB.open(DB_NAME, DB_VERSION);

      request.onupgradeneeded = () => {
        const database = request.result;
        const upgradeTransaction = request.transaction;

        if (!database.objectStoreNames.contains(META_STORE)) {
          database.createObjectStore(META_STORE, { keyPath: "key" });
        }

        ensurePasswordEntryStore(database, upgradeTransaction);
        ensurePasskeyStore(database, upgradeTransaction);
      };

      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error || new Error("Failed to open IndexedDB."));
    });
  }

  return dbPromise;
}

export async function resetVaultDatabase() {
  ensureIndexedDb();

  if (dbPromise) {
    try {
      const database = await dbPromise;
      database.close();
    } catch {
    } finally {
      dbPromise = null;
    }
  }

  await new Promise((resolve, reject) => {
    const request = globalThis.indexedDB.deleteDatabase(DB_NAME);

    request.onsuccess = () => resolve();
    request.onerror = () =>
      reject(request.error || new Error("Unable to remove the current vault database."));
    request.onblocked = () =>
      reject(new Error("The vault database is still open in another window or webview."));
  });
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

export async function getPendingImportReviewRecord() {
  const result = await getMetaRecord(PENDING_IMPORT_REVIEW_KEY);
  return (
    result || {
      items: [],
      updatedAt: 0,
    }
  );
}

export async function savePendingImportReviewRecord(record) {
  await saveMetaRecord(PENDING_IMPORT_REVIEW_KEY, {
    items: Array.isArray(record?.items) ? record.items : [],
    updatedAt: Number(record?.updatedAt || 0),
  });
}

export async function getAppLogsRecord() {
  const result = await getMetaRecord(APP_LOGS_KEY);
  return (
    result || {
      entries: [],
      updatedAt: 0,
    }
  );
}

export async function saveAppLogsRecord(record) {
  await saveMetaRecord(APP_LOGS_KEY, {
    entries: Array.isArray(record?.entries) ? record.entries : [],
    updatedAt: Number(record?.updatedAt || 0),
  });
}

export async function listPasswordRecords() {
  const collectedRecords = [];

  await streamPasswordRecords({
    batchSize: 200,
    onActiveBatch(batch) {
      collectedRecords.push(...batch);
    },
    onDeletedBatch(batch) {
      collectedRecords.push(...batch);
    },
  });

  return collectedRecords;
}

async function streamPasswordRecordsFromIndex({
  database,
  indexName,
  direction = "prev",
  batchSize = 100,
  filter,
  onBatch,
}) {
  const transaction = database.transaction(ENTRY_STORE, "readonly");
  const store = transaction.objectStore(ENTRY_STORE);

  await new Promise((resolve, reject) => {
    const source =
      indexName && store.indexNames.contains(indexName) ? store.index(indexName) : store;
    const request = source.openCursor(null, direction);
    let batch = [];

    function flushBatch() {
      if (!batch.length || typeof onBatch !== "function") {
        batch = [];
        return;
      }

      const nextBatch = batch;
      batch = [];
      onBatch(nextBatch);
    }

    request.onerror = () => reject(request.error || new Error("IndexedDB cursor failed."));
    request.onsuccess = () => {
      const cursor = request.result;
      if (!cursor) {
        try {
          flushBatch();
          resolve();
        } catch (error) {
          reject(error);
        }
        return;
      }

      try {
        if (!filter || filter(cursor.value)) {
          batch.push(cursor.value);
        }

        if (batch.length >= batchSize) {
          flushBatch();
        }

        cursor.continue();
      } catch (error) {
        reject(error);
      }
    };
  });

  await transactionToPromise(transaction);
}

export async function streamPasswordRecords({
  batchSize = 100,
  onActiveBatch,
  onDeletedBatch,
} = {}) {
  const database = await openVaultDatabase();

  await streamPasswordRecordsFromIndex({
    database,
    indexName: "updatedAt",
    direction: "prev",
    batchSize,
    filter: (record) => !record.deletedAt,
    onBatch: onActiveBatch,
  });

  await streamPasswordRecordsFromIndex({
    database,
    indexName: "deletedAt",
    direction: "prev",
    batchSize,
    filter: (record) => Boolean(record.deletedAt),
    onBatch: onDeletedBatch,
  });
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

export async function replacePasswordRecords(records) {
  const database = await openVaultDatabase();
  const transaction = database.transaction(ENTRY_STORE, "readwrite");
  const store = transaction.objectStore(ENTRY_STORE);

  store.clear();

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

export async function getPasskeyRecord(recordId) {
  const database = await openVaultDatabase();
  const transaction = database.transaction(PASSKEY_STORE, "readonly");
  const store = transaction.objectStore(PASSKEY_STORE);
  const result = await requestToPromise(store.get(recordId));
  await transactionToPromise(transaction);
  return result || null;
}

export async function listPasskeyRecords() {
  const database = await openVaultDatabase();
  const transaction = database.transaction(PASSKEY_STORE, "readonly");
  const store = transaction.objectStore(PASSKEY_STORE);
  const result = await requestToPromise(store.getAll());
  await transactionToPromise(transaction);
  return Array.isArray(result) ? result : [];
}

export async function putPasskeyRecord(record) {
  const database = await openVaultDatabase();
  const transaction = database.transaction(PASSKEY_STORE, "readwrite");
  const store = transaction.objectStore(PASSKEY_STORE);
  store.put(cloneForIndexedDb(record));
  await transactionToPromise(transaction);
}

export async function putPasskeyRecords(records) {
  const database = await openVaultDatabase();
  const transaction = database.transaction(PASSKEY_STORE, "readwrite");
  const store = transaction.objectStore(PASSKEY_STORE);

  records.forEach((record) => {
    store.put(cloneForIndexedDb(record));
  });

  await transactionToPromise(transaction);
}

export async function replacePasskeyRecords(records) {
  const database = await openVaultDatabase();
  const transaction = database.transaction(PASSKEY_STORE, "readwrite");
  const store = transaction.objectStore(PASSKEY_STORE);

  store.clear();

  records.forEach((record) => {
    store.put(cloneForIndexedDb(record));
  });

  await transactionToPromise(transaction);
}

export async function deletePasskeyRecord(recordId) {
  const database = await openVaultDatabase();
  const transaction = database.transaction(PASSKEY_STORE, "readwrite");
  const store = transaction.objectStore(PASSKEY_STORE);
  store.delete(recordId);
  await transactionToPromise(transaction);
}
