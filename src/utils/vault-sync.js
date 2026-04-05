function normalizeItemText(value, fallback = "未命名项目") {
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

export function buildSnapshotPreview(records, deletedRecords = []) {
  const latestRecord = pickLatestRecord(records);

  return {
    totalCount: records.length,
    deletedCount: deletedRecords.length,
    latestItem: latestRecord
      ? {
          id: latestRecord.id,
          siteName: normalizeItemText(latestRecord.siteName),
          username: normalizeItemText(latestRecord.username, "未填写用户名"),
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
    throw new Error("当前没有可导出的密码库配置。");
  }

  const activeRecords = Array.isArray(records) ? records : [];
  const removedRecords = Array.isArray(deletedRecords) ? deletedRecords : [];
  const allRecords = [...activeRecords, ...removedRecords];

  return {
    version: 1,
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
    throw new Error("同步数据不是有效的 JSON 格式。");
  }

  if (!snapshot || typeof snapshot !== "object") {
    throw new Error("同步数据格式不正确。");
  }

  if (!snapshot.vaultConfig || !snapshot.vaultConfig.salt || !snapshot.vaultConfig.verification) {
    throw new Error("同步数据缺少密码库配置。");
  }

  if (!Array.isArray(snapshot.records)) {
    throw new Error("同步数据缺少记录列表。");
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
