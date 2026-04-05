/**
 * 统一的数据模型定义。
 *
 * 说明：
 * 1. `siteName` 作为“网站/应用名称”用于更接近 Google Password Manager 的展示方式，选填。
 * 2. `passwordCipher` 只在 IndexedDB 中保存加密结果，明文密码只在当前内存会话里短暂存在。
 * 3. CSV 导入冲突策略按 `usernameNormalized` 判定，这是本项目的显式约定。
 */

/**
 * @typedef {Object} EncryptedPayload
 * @property {string} iv AES-GCM 随机向量，Base64 编码
 * @property {string} cipherText 密文，Base64 编码
 */

/**
 * @typedef {Object} PasswordRecord
 * @property {string} id
 * @property {string} siteName
 * @property {string} username
 * @property {string} usernameNormalized
 * @property {string[]} notes
 * @property {EncryptedPayload} passwordCipher
 * @property {boolean} isFavorite
 * @property {number|null} deletedAt
 * @property {number} createdAt
 * @property {number} updatedAt
 */

/**
 * @typedef {Object} PasswordDraft
 * @property {string} id
 * @property {string} siteName
 * @property {string} username
 * @property {string} password
 * @property {string[]} notes
 * @property {boolean} isFavorite
 */

function createFallbackId() {
  return `${Date.now()}-${Math.random().toString(16).slice(2, 10)}`;
}

export function createRecordId() {
  return globalThis.crypto?.randomUUID?.() || createFallbackId();
}

export function sanitizeText(value) {
  return String(value ?? "").trim();
}

export function sanitizeNotes(notes) {
  return (notes || []).map((note) => sanitizeText(note)).filter(Boolean);
}

export function normalizeUsername(username) {
  return sanitizeText(username).toLowerCase();
}

export function createEmptyPasswordDraft() {
  return {
    id: "",
    siteName: "",
    username: "",
    password: "",
    notes: [""],
    isFavorite: false,
  };
}

export function clonePasswordDraft(draft) {
  return {
    id: draft?.id || "",
    siteName: draft?.siteName || "",
    username: draft?.username || "",
    password: draft?.password || "",
    notes: Array.isArray(draft?.notes) && draft.notes.length ? [...draft.notes] : [""],
    isFavorite: Boolean(draft?.isFavorite),
  };
}

export function createStoredPasswordRecord({
  id,
  siteName,
  username,
  notes,
  passwordCipher,
  isFavorite,
  deletedAt,
  createdAt,
  updatedAt,
}) {
  const safeUsername = sanitizeText(username);
  const now = Date.now();

  return {
    id: id || createRecordId(),
    siteName: sanitizeText(siteName),
    username: safeUsername,
    usernameNormalized: normalizeUsername(safeUsername),
    notes: sanitizeNotes(notes),
    passwordCipher,
    isFavorite: Boolean(isFavorite),
    deletedAt: deletedAt || null,
    createdAt: createdAt || now,
    updatedAt: updatedAt || now,
  };
}

export function createDraftFromRecord(record, plainPassword) {
  return {
    id: record.id,
    siteName: record.siteName || "",
    username: record.username || "",
    password: plainPassword || "",
    notes: record.notes?.length ? [...record.notes] : [""],
    isFavorite: Boolean(record.isFavorite),
  };
}

export function createExportableEntry(record, plainPassword) {
  return {
    id: record.id,
    siteName: record.siteName || "",
    username: record.username || "",
    password: plainPassword || "",
    notes: record.notes?.length ? [...record.notes] : [],
    isFavorite: Boolean(record.isFavorite),
    createdAt: record.createdAt,
    updatedAt: record.updatedAt,
  };
}
