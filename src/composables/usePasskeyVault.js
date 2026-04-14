import { ref } from "vue";
import {
  createStoredPasskeyRecord,
  normalizeRpId,
} from "@/models/passkey-item";
import {
  deletePasskeyRecord,
  listPasskeyRecords,
  putPasskeyRecord,
  replacePasskeyRecords,
} from "@/utils/indexed-db";

function sortActiveRecords(records) {
  return [...records].sort((left, right) => {
    const leftTime = Number(left.lastUsedAt || left.updatedAt || left.createdAt || 0);
    const rightTime = Number(right.lastUsedAt || right.updatedAt || right.createdAt || 0);
    return rightTime - leftTime;
  });
}

function sortDeletedRecords(records) {
  return [...records].sort((left, right) => Number(right.deletedAt || 0) - Number(left.deletedAt || 0));
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

function createMatchKeys(record) {
  const nativeKey = String(record?.nativeProviderRecordId || "").trim();
  const credentialKey = String(record?.credentialId || "").trim();
  const rpKey = normalizeRpId(record?.rpId);
  const usernameKey = String(record?.username || "").trim().toLowerCase();

  return {
    nativeKey,
    credentialKey,
    fallbackKey: `${rpKey}::${usernameKey}`,
  };
}

export function usePasskeyVault() {
  const records = ref([]);
  const deletedRecords = ref([]);

  function clearRecords() {
    records.value = [];
    deletedRecords.value = [];
  }

  async function loadRecords() {
    const allRecords = await listPasskeyRecords();
    const { activeRecords, removedRecords } = splitRecords(allRecords);
    records.value = activeRecords;
    deletedRecords.value = removedRecords;

    return {
      activeRecords,
      removedRecords,
    };
  }

  async function syncFromHostMetadata(hostItems = []) {
    const existingRecords = [...records.value, ...deletedRecords.value];
    const existingByNative = new Map();
    const existingByCredential = new Map();
    const existingByFallback = new Map();

    existingRecords.forEach((record) => {
      const matchKeys = createMatchKeys(record);
      if (matchKeys.nativeKey) {
        existingByNative.set(matchKeys.nativeKey, record);
      }
      if (matchKeys.credentialKey) {
        existingByCredential.set(matchKeys.credentialKey, record);
      }
      existingByFallback.set(matchKeys.fallbackKey, record);
    });

    const matchedRecordIds = new Set();
    const nextRecords = hostItems.map((item) => {
      const matchKeys = createMatchKeys(item);
      const existing =
        (matchKeys.nativeKey && existingByNative.get(matchKeys.nativeKey)) ||
        (matchKeys.credentialKey && existingByCredential.get(matchKeys.credentialKey)) ||
        existingByFallback.get(matchKeys.fallbackKey) ||
        null;

      if (existing?.id) {
        matchedRecordIds.add(existing.id);
      }

      return createStoredPasskeyRecord({
        id: existing?.id,
        credentialId: item.credentialId,
        rpId: item.rpId,
        username: item.username,
        displayName: item.displayName,
        userHandle: item.userHandle,
        transportHints: item.transportHints,
        authenticatorName: item.authenticatorName,
        nativeProviderRecordId: item.nativeProviderRecordId,
        origin: item.origin || item.rpId,
        sourceDeviceId: item.sourceDeviceId || "windows-host",
        syncState: "windows-managed",
        attestationFormat: item.attestationFormat,
        isRemovable: item.isRemovable,
        isBackedUp: item.isBackedUp,
        isFavorite: existing?.isFavorite || false,
        deletedAt: existing?.deletedAt || null,
        createdAt: existing?.createdAt || item.createdAt || Date.now(),
        updatedAt: item.updatedAt || existing?.updatedAt || Date.now(),
        lastUsedAt: item.lastUsedAt || existing?.lastUsedAt || null,
      });
    });

    existingRecords.forEach((record) => {
      if (!record.deletedAt || matchedRecordIds.has(record.id)) {
        return;
      }

      nextRecords.push(record);
    });

    await replacePasskeyRecords(nextRecords);
    await loadRecords();

    return {
      total: nextRecords.length,
      active: records.value.length,
      deleted: deletedRecords.value.length,
    };
  }

  async function removeRecord(recordId) {
    const existing = records.value.find((record) => record.id === recordId);
    if (!existing) {
      throw new Error("The selected passkey metadata record could not be found.");
    }

    await putPasskeyRecord({
      ...existing,
      deletedAt: Date.now(),
      updatedAt: Date.now(),
    });

    await loadRecords();
  }

  async function restoreRecord(recordId) {
    const existing = deletedRecords.value.find((record) => record.id === recordId);
    if (!existing) {
      throw new Error("The selected deleted passkey metadata record could not be found.");
    }

    await putPasskeyRecord({
      ...existing,
      deletedAt: null,
      updatedAt: Date.now(),
    });

    await loadRecords();
  }

  async function permanentlyDeleteRecord(recordId) {
    await deletePasskeyRecord(recordId);
    await loadRecords();
  }

  return {
    records,
    deletedRecords,
    clearRecords,
    loadRecords,
    syncFromHostMetadata,
    removeRecord,
    restoreRecord,
    permanentlyDeleteRecord,
  };
}
