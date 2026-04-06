function normalizeItemText(value, fallback = "Unnamed item") {
  const text = String(value ?? "").trim();
  return text || fallback;
}

function pickLatestRecord(records) {
  return [...records]
    .filter((record) => !record.deletedAt)
    .sort((left, right) => {
      const leftTimestamp = left.createdAt || left.updatedAt || 0;
      const rightTimestamp = right.createdAt || right.updatedAt || 0;
      return rightTimestamp - leftTimestamp;
    })[0] || null;
}

function isLegacyVaultConfig(config) {
  return Boolean(config?.salt && config?.verification);
}

function isV2VaultConfig(config) {
  return Boolean(config?.recoverySalt && config?.recoveryWrappedVaultKey && config?.verification);
}

export function buildSnapshotPreview(records, deletedRecords = []) {
  const latestRecord = pickLatestRecord(records);

  return {
    totalCount: records.length,
    deletedCount: deletedRecords.length,
    latestItem: latestRecord
      ? {
          id: latestRecord.id,
          siteName: normalizeItemText(latestRecord.siteName),
          username: normalizeItemText(latestRecord.username, "Unnamed username"),
          createdAt: latestRecord.createdAt || latestRecord.updatedAt || Date.now(),
          updatedAt: latestRecord.updatedAt || latestRecord.createdAt || Date.now(),
        }
      : null,
  };
}

export function buildEncryptedVaultSnapshot({
  vaultConfig,
  records,
  deletedRecords,
  deviceName = "",
}) {
  if (!vaultConfig) {
    throw new Error("The current vault does not have exportable configuration.");
  }

  const activeRecords = Array.isArray(records) ? records : [];
  const removedRecords = Array.isArray(deletedRecords) ? deletedRecords : [];
  const allRecords = [...activeRecords, ...removedRecords];

  return {
    version: 2,
    exportedAt: Date.now(),
    deviceName: String(deviceName || "").trim(),
    vaultConfig,
    records: allRecords,
    preview: buildSnapshotPreview(activeRecords, removedRecords),
  };
}

export function buildEncryptedVaultSnapshotText(payload) {
  return JSON.stringify(buildEncryptedVaultSnapshot(payload));
}

export function parseEncryptedVaultSnapshot(text) {
  let snapshot;

  try {
    snapshot = JSON.parse(String(text || ""));
  } catch {
    throw new Error("The sync payload is not valid JSON.");
  }

  if (!snapshot || typeof snapshot !== "object") {
    throw new Error("The sync payload format is invalid.");
  }

  if (!snapshot.vaultConfig || (!isLegacyVaultConfig(snapshot.vaultConfig) && !isV2VaultConfig(snapshot.vaultConfig))) {
    throw new Error("The sync payload does not contain a valid vault configuration.");
  }

  if (!Array.isArray(snapshot.records)) {
    throw new Error("The sync payload does not contain a record list.");
  }

  const activeRecords = snapshot.records.filter((record) => !record.deletedAt);
  const deletedRecords = snapshot.records.filter((record) => Boolean(record.deletedAt));

  return {
    version: Number(snapshot.version || 1),
    exportedAt: Number(snapshot.exportedAt || Date.now()),
    deviceName: String(snapshot.deviceName || "").trim(),
    vaultConfig: snapshot.vaultConfig,
    records: snapshot.records,
    activeRecords,
    deletedRecords,
    preview: snapshot.preview || buildSnapshotPreview(activeRecords, deletedRecords),
  };
}
