import { reactive, ref, shallowRef } from "vue";
import {
  clonePasswordDraft,
  createDraftFromRecord,
  createExportableEntry,
  createStoredPasswordRecord,
  normalizeUsername,
} from "@/models/password-item";
import {
  createVaultConfig,
  decryptText,
  encryptText,
  ensureLocalVaultWrap,
  exportVaultConfigForSync,
  importVaultKeyFromBase64,
  isSecretKeyRequiredForConfig,
  revealSecretKey,
  rewrapVaultConfig,
  unlockVaultKey,
} from "@/utils/crypto";
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
  const vaultKey = shallowRef(null);
  const vaultKeyBase64 = ref("");
  const state = reactive({
    bootstrapping: false,
    unlocking: false,
    unlocked: false,
    requiresSetup: false,
    requiresSecretKeyForUnlock: false,
  });

  function assertUnlocked() {
    if (!vaultKey.value || !vaultKeyBase64.value) {
      throw new Error("请先解锁密码库。");
    }
  }

  function applyVaultSession(config, cryptoKey, keyBase64) {
    vaultConfig.value = config;
    vaultKey.value = cryptoKey;
    vaultKeyBase64.value = keyBase64;
    state.unlocked = true;
    state.requiresSetup = false;
    state.requiresSecretKeyForUnlock = false;
  }

  function clearVaultSession() {
    vaultKey.value = null;
    vaultKeyBase64.value = "";
    state.unlocked = false;
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
      state.requiresSecretKeyForUnlock =
        !state.requiresSetup && isSecretKeyRequiredForConfig(vaultConfig.value);
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

  async function migrateLegacyVault(passphrase, legacyCryptoKey) {
    const allRecords = await listPasswordRecords();
    const migratedVault = await createVaultConfig(passphrase, {
      migratedFromVersion: vaultConfig.value?.version || 1,
    });
    const migratedRecords = [];

    for (const record of allRecords) {
      const plainPassword = await decryptText(record.passwordCipher, legacyCryptoKey);
      const passwordCipher = await encryptText(plainPassword, migratedVault.vaultKey);

      migratedRecords.push({
        ...record,
        passwordCipher,
        updatedAt: record.updatedAt || Date.now(),
      });
    }

    await replaceVaultSnapshotData(migratedVault.config, migratedRecords);
    applyVaultSession(migratedVault.config, migratedVault.vaultKey, migratedVault.vaultKeyBase64);

    return {
      generatedSecretKey: migratedVault.secretKey,
      usedRecovery: false,
      migratedFromLegacy: true,
    };
  }

  async function submitMasterPassword(passphrase, secretKey = "") {
    state.unlocking = true;

    try {
      if (state.requiresSetup) {
        const nextVault = await createVaultConfig(passphrase);
        await saveVaultConfigRecord(nextVault.config);
        applyVaultSession(nextVault.config, nextVault.vaultKey, nextVault.vaultKeyBase64);
        await loadRecords();

        return {
          generatedSecretKey: nextVault.secretKey,
          usedRecovery: false,
          migratedFromLegacy: false,
        };
      }

      const unlocked = await unlockVaultKey(passphrase, vaultConfig.value, { secretKey });

      if (unlocked.scheme === "legacy") {
        const migrationResult = await migrateLegacyVault(passphrase, unlocked.vaultKey);
        await loadRecords();
        return migrationResult;
      }

      let nextConfig = vaultConfig.value;
      if (unlocked.usedRecovery) {
        nextConfig = await ensureLocalVaultWrap(passphrase, vaultConfig.value, unlocked.vaultKeyBase64);
        if (nextConfig !== vaultConfig.value) {
          await saveVaultConfigRecord(nextConfig);
        }
      }

      applyVaultSession(nextConfig, unlocked.vaultKey, unlocked.vaultKeyBase64);
      await loadRecords();

      return {
        generatedSecretKey: "",
        usedRecovery: unlocked.usedRecovery,
        migratedFromLegacy: false,
      };
    } finally {
      state.unlocking = false;
    }
  }

  async function unlockWithVaultKeyBase64(rawVaultKeyBase64) {
    state.unlocking = true;

    try {
      if (!vaultConfig.value || state.requiresSetup) {
        throw new Error("The vault has not been set up yet.");
      }

      if (!rawVaultKeyBase64) {
        throw new Error("The host did not provide a valid vault key.");
      }

      if (vaultConfig.value.version < 2) {
        throw new Error("Biometric unlock requires the vault to be migrated first.");
      }

      const importedVaultKey = await importVaultKeyFromBase64(rawVaultKeyBase64);
      const verificationText = await decryptText(vaultConfig.value.verification, importedVaultKey);

      if (verificationText !== "password-manager-vault-check-v2") {
        throw new Error("The stored device key is no longer valid for this vault.");
      }

      applyVaultSession(vaultConfig.value, importedVaultKey, rawVaultKeyBase64);
      await loadRecords();

      return true;
    } finally {
      state.unlocking = false;
    }
  }

  function lockVault() {
    clearVaultSession();
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

    const plainPassword = await decryptText(record.passwordCipher, vaultKey.value);
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

    const plainPassword = await decryptText(record.passwordCipher, vaultKey.value);
    return createDraftFromRecord(record, plainPassword);
  }

  async function saveDraft(draft) {
    assertUnlocked();

    const existing = draft.id ? findRecordById(draft.id) : null;
    const passwordCipher = await encryptText(draft.password, vaultKey.value);
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

    const secretKey = await revealSecretKey(vaultConfig.value, vaultKey.value);
    const nextVault = await rewrapVaultConfig(
      nextPassphrase,
      vaultConfig.value,
      vaultKeyBase64.value,
      secretKey
    );

    await saveVaultConfigRecord(nextVault.config);
    applyVaultSession(nextVault.config, vaultKey.value, vaultKeyBase64.value);
    clearRevealedPasswords();
  }

  async function getExportEntries() {
    assertUnlocked();

    const exportEntries = [];

    for (const record of records.value) {
      const plainPassword = await decryptText(record.passwordCipher, vaultKey.value);
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
      vaultConfig: exportVaultConfigForSync(vaultConfig.value),
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
    clearVaultSession();
    state.requiresSetup = false;
    state.requiresSecretKeyForUnlock = isSecretKeyRequiredForConfig(snapshot.vaultConfig);
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

      const passwordCipher = await encryptText(entry.password, vaultKey.value);
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

  async function getSecretKey() {
    assertUnlocked();
    return revealSecretKey(vaultConfig.value, vaultKey.value);
  }

  function getVaultKeyBase64() {
    assertUnlocked();
    return vaultKeyBase64.value;
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
    getSecretKey,
    getSyncPreview,
    getVaultKeyBase64,
    hidePassword,
    importEntriesFromCsvText,
    loadRecords,
    lockVault,
    unlockWithVaultKeyBase64,
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
