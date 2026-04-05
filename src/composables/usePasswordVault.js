import { reactive, ref, shallowRef } from "vue";
import {
  clonePasswordDraft,
  createDraftFromRecord,
  createExportableEntry,
  createStoredPasswordRecord,
  normalizeUsername,
} from "@/models/password-item";
import { createVaultConfig, decryptText, encryptText, unlockVaultKey } from "@/utils/crypto";
import { collapseImportedEntries, parseImportedEntries } from "@/utils/csv-utils";
import {
  buildEncryptedVaultSnapshot,
  buildSnapshotPreview,
  parseEncryptedVaultSnapshot,
} from "@/utils/vault-sync";
import {
  deletePasswordRecord,
  getVaultConfigRecord,
  listPasswordRecords,
  putPasswordRecord,
  putPasswordRecords,
  replaceVaultSnapshotData,
  saveVaultConfigRecord,
} from "@/utils/indexed-db";

function sortActiveRecords(records) {
  return [...records].sort((left, right) => right.updatedAt - left.updatedAt);
}

function sortDeletedRecords(records) {
  return [...records].sort((left, right) => (right.deletedAt || 0) - (left.deletedAt || 0));
}

function splitRecords(records) {
  const activeRecords = [];
  const removedRecords = [];

  records.forEach((record) => {
    if (record.deletedAt) {
      removedRecords.push(record);
    } else {
      activeRecords.push(record);
    }
  });

  return {
    activeRecords: sortActiveRecords(activeRecords),
    removedRecords: sortDeletedRecords(removedRecords),
  };
}

export function usePasswordVault() {
  const records = ref([]);
  const deletedRecords = ref([]);
  const revealedPasswords = reactive({});
  const vaultConfig = ref(null);
  const cryptoKey = shallowRef(null);
  const state = reactive({
    bootstrapping: false,
    unlocking: false,
    unlocked: false,
    requiresSetup: false,
  });

  function assertUnlocked() {
    if (!cryptoKey.value) {
      throw new Error("请先输入主密码解锁密码库。");
    }
  }

  function clearRevealedPasswords() {
    Object.keys(revealedPasswords).forEach((recordId) => {
      delete revealedPasswords[recordId];
    });
  }

  function clearRevealedPasswordsByIds(recordIds) {
    recordIds.forEach((recordId) => {
      delete revealedPasswords[recordId];
    });
  }

  function findRecordById(recordId) {
    return [...records.value, ...deletedRecords.value].find((item) => item.id === recordId) || null;
  }

  async function bootstrapVault() {
    state.bootstrapping = true;

    try {
      vaultConfig.value = await getVaultConfigRecord();
      state.requiresSetup = !vaultConfig.value;
    } finally {
      state.bootstrapping = false;
    }
  }

  async function loadRecords() {
    const allRecords = await listPasswordRecords();
    const { activeRecords, removedRecords } = splitRecords(allRecords);

    records.value = activeRecords;
    deletedRecords.value = removedRecords;

    return {
      activeRecords,
      removedRecords,
    };
  }

  async function submitMasterPassword(passphrase) {
    state.unlocking = true;

    try {
      if (state.requiresSetup) {
        const result = await createVaultConfig(passphrase);
        cryptoKey.value = result.cryptoKey;
        vaultConfig.value = result.config;
        await saveVaultConfigRecord(result.config);
        state.requiresSetup = false;
      } else {
        cryptoKey.value = await unlockVaultKey(passphrase, vaultConfig.value);
      }

      state.unlocked = true;
      await loadRecords();
    } finally {
      state.unlocking = false;
    }
  }

  function lockVault() {
    cryptoKey.value = null;
    state.unlocked = false;
    records.value = [];
    deletedRecords.value = [];
    clearRevealedPasswords();
  }

  async function decryptPasswordById(recordId) {
    assertUnlocked();

    if (revealedPasswords[recordId]) {
      return revealedPasswords[recordId];
    }

    const record = findRecordById(recordId);
    if (!record) {
      throw new Error("未找到指定的密码记录。");
    }

    const plainPassword = await decryptText(record.passwordCipher, cryptoKey.value);
    revealedPasswords[recordId] = plainPassword;
    return plainPassword;
  }

  function hidePassword(recordId) {
    delete revealedPasswords[recordId];
  }

  async function buildEditableDraft(recordId) {
    assertUnlocked();

    const record = records.value.find((item) => item.id === recordId);
    if (!record) {
      throw new Error("未找到要编辑的密码记录。");
    }

    const plainPassword = await decryptText(record.passwordCipher, cryptoKey.value);
    return createDraftFromRecord(record, plainPassword);
  }

  async function saveDraft(draft) {
    assertUnlocked();

    const existing = draft.id ? findRecordById(draft.id) : null;
    const passwordCipher = await encryptText(draft.password, cryptoKey.value);
    const storedRecord = createStoredPasswordRecord({
      id: existing?.id || draft.id,
      siteName: draft.siteName,
      username: draft.username,
      notes: draft.notes,
      passwordCipher,
      isFavorite: draft.isFavorite ?? existing?.isFavorite ?? false,
      deletedAt: null,
      createdAt: existing?.createdAt || Date.now(),
      updatedAt: Date.now(),
    });

    await putPasswordRecord(storedRecord);
    await loadRecords();
    delete revealedPasswords[storedRecord.id];
    return storedRecord;
  }

  async function removeRecord(recordId) {
    const existing = records.value.find((record) => record.id === recordId);
    if (!existing) {
      throw new Error("未找到要删除的密码记录。");
    }

    await putPasswordRecord({
      ...existing,
      deletedAt: Date.now(),
      updatedAt: Date.now(),
    });

    await loadRecords();
    delete revealedPasswords[recordId];
  }

  async function removeRecords(recordIds) {
    const targetIds = new Set(recordIds);
    const targets = records.value.filter((record) => targetIds.has(record.id));

    if (!targets.length) {
      throw new Error("未找到要移入最近删除的密码记录。");
    }

    const timestamp = Date.now();
    await putPasswordRecords(
      targets.map((record) => ({
        ...record,
        deletedAt: timestamp,
        updatedAt: timestamp,
      }))
    );

    await loadRecords();
    clearRevealedPasswordsByIds(targets.map((record) => record.id));
  }

  async function restoreRecord(recordId) {
    const existing = deletedRecords.value.find((record) => record.id === recordId);
    if (!existing) {
      throw new Error("未找到要恢复的记录。");
    }

    await putPasswordRecord({
      ...existing,
      deletedAt: null,
      updatedAt: Date.now(),
    });

    await loadRecords();
  }

  async function permanentlyDeleteRecord(recordId) {
    await deletePasswordRecord(recordId);
    await loadRecords();
    delete revealedPasswords[recordId];
  }

  async function toggleFavorite(recordId) {
    const existing = records.value.find((record) => record.id === recordId);
    if (!existing) {
      throw new Error("未找到要收藏的密码记录。");
    }

    await putPasswordRecord({
      ...existing,
      isFavorite: !existing.isFavorite,
      updatedAt: Date.now(),
    });

    await loadRecords();
  }

  async function setFavoriteRecords(recordIds, isFavorite) {
    const targetIds = new Set(recordIds);
    const targets = records.value.filter((record) => targetIds.has(record.id));

    if (!targets.length) {
      throw new Error("未找到要更新收藏状态的密码记录。");
    }

    const timestamp = Date.now();
    await putPasswordRecords(
      targets.map((record) => ({
        ...record,
        isFavorite,
        updatedAt: timestamp,
      }))
    );

    await loadRecords();
  }

  async function changeMasterPassword(currentPassphrase, nextPassphrase) {
    assertUnlocked();

    await unlockVaultKey(currentPassphrase, vaultConfig.value);

    const nextVault = await createVaultConfig(nextPassphrase);
    const allRecords = [...records.value, ...deletedRecords.value];
    const reEncryptedRecords = [];

    for (const record of allRecords) {
      const plainPassword = await decryptText(record.passwordCipher, cryptoKey.value);
      const passwordCipher = await encryptText(plainPassword, nextVault.cryptoKey);

      reEncryptedRecords.push({
        ...record,
        passwordCipher,
        updatedAt: Date.now(),
      });
    }

    if (reEncryptedRecords.length) {
      await putPasswordRecords(reEncryptedRecords);
    }

    await saveVaultConfigRecord(nextVault.config);
    vaultConfig.value = nextVault.config;
    cryptoKey.value = nextVault.cryptoKey;
    clearRevealedPasswords();
    await loadRecords();
  }

  async function getExportEntries() {
    assertUnlocked();

    const exportEntries = [];

    for (const record of records.value) {
      const plainPassword = await decryptText(record.passwordCipher, cryptoKey.value);
      exportEntries.push(createExportableEntry(record, plainPassword));
    }

    return exportEntries;
  }

  function getSyncPreview() {
    return buildSnapshotPreview(records.value, deletedRecords.value);
  }

  function getEncryptedSnapshot(deviceName = "") {
    assertUnlocked();

    return buildEncryptedVaultSnapshot({
      vaultConfig: vaultConfig.value,
      records: records.value,
      deletedRecords: deletedRecords.value,
      deviceName,
    });
  }

  async function replaceWithEncryptedSnapshot(snapshotInput) {
    const snapshot =
      typeof snapshotInput === "string"
        ? parseEncryptedVaultSnapshot(snapshotInput)
        : snapshotInput;

    await replaceVaultSnapshotData(snapshot.vaultConfig, snapshot.records);
    vaultConfig.value = snapshot.vaultConfig;
    cryptoKey.value = null;
    state.unlocked = false;
    state.requiresSetup = false;
    clearRevealedPasswords();
    await loadRecords();

    return snapshot;
  }

  async function importEntriesFromCsvText(text, strategy) {
    assertUnlocked();

    const parsedEntries = parseImportedEntries(text);
    if (!parsedEntries.length) {
      throw new Error("CSV 中没有可导入的有效数据。");
    }

    const collapsed = collapseImportedEntries(parsedEntries, strategy);
    const existingMap = new Map(
      [...records.value, ...deletedRecords.value].map((record) => [record.usernameNormalized, record])
    );

    let created = 0;
    let updated = 0;
    let skipped = collapsed.skippedDuplicates;
    const recordsToPersist = [];

    for (const entry of collapsed.entries) {
      const existing = existingMap.get(normalizeUsername(entry.username));

      if (existing && strategy === "skip") {
        skipped += 1;
        continue;
      }

      const passwordCipher = await encryptText(entry.password, cryptoKey.value);
      const mergedRecord = createStoredPasswordRecord({
        id: existing?.id,
        siteName: entry.siteName,
        username: entry.username,
        notes: entry.notes,
        passwordCipher,
        isFavorite: existing?.isFavorite || false,
        deletedAt: null,
        createdAt: existing?.createdAt || entry.createdAt,
        updatedAt: Date.now(),
      });

      recordsToPersist.push(mergedRecord);

      if (existing) {
        updated += 1;
      } else {
        created += 1;
      }
    }

    if (recordsToPersist.length) {
      await putPasswordRecords(recordsToPersist);
    }

    await loadRecords();

    return {
      total: parsedEntries.length,
      created,
      updated,
      skipped,
    };
  }

  return {
    records,
    deletedRecords,
    revealedPasswords,
    state,
    vaultConfig,
    bootstrapVault,
    buildEditableDraft,
    changeMasterPassword,
    clonePasswordDraft,
    decryptPasswordById,
    getEncryptedSnapshot,
    getExportEntries,
    getSyncPreview,
    hidePassword,
    importEntriesFromCsvText,
    loadRecords,
    lockVault,
    permanentlyDeleteRecord,
    removeRecord,
    removeRecords,
    replaceWithEncryptedSnapshot,
    restoreRecord,
    saveDraft,
    setFavoriteRecords,
    submitMasterPassword,
    toggleFavorite,
  };
}
