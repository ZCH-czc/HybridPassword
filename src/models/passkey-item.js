/**
 * Windows passkey metadata model.
 *
 * This file is intentionally metadata-only:
 * - The actual passkey private key material must stay in the Windows-native
 *   provider / secure platform layer.
 * - The Vue / IndexedDB layer should only persist searchable metadata and
 *   local UX state such as favorites or soft-delete markers.
 */

function createFallbackId() {
  return `${Date.now()}-${Math.random().toString(16).slice(2, 10)}`;
}

export function createPasskeyRecordId() {
  return globalThis.crypto?.randomUUID?.() || createFallbackId();
}

export function sanitizePasskeyText(value) {
  return String(value ?? "").trim();
}

export function normalizeRpId(rpId) {
  return sanitizePasskeyText(rpId).toLowerCase();
}

export function sanitizeTransportHints(transportHints) {
  return Array.isArray(transportHints)
    ? transportHints.map((item) => sanitizePasskeyText(item)).filter(Boolean)
    : [];
}

export function createStoredPasskeyRecord({
  id,
  credentialId,
  rpId,
  username,
  displayName,
  userHandle,
  transportHints,
  authenticatorName,
  nativeProviderRecordId,
  origin,
  sourceDeviceId,
  syncState,
  attestationFormat,
  isRemovable,
  isBackedUp,
  isFavorite,
  deletedAt,
  createdAt,
  updatedAt,
  lastUsedAt,
}) {
  const now = Date.now();

  return {
    id: id || createPasskeyRecordId(),
    credentialId: sanitizePasskeyText(credentialId),
    rpId: sanitizePasskeyText(rpId),
    rpIdNormalized: normalizeRpId(rpId),
    username: sanitizePasskeyText(username),
    displayName: sanitizePasskeyText(displayName),
    userHandle: sanitizePasskeyText(userHandle),
    transportHints: sanitizeTransportHints(transportHints),
    authenticatorName: sanitizePasskeyText(authenticatorName),
    nativeProviderRecordId: sanitizePasskeyText(nativeProviderRecordId),
    origin: sanitizePasskeyText(origin),
    sourceDeviceId: sanitizePasskeyText(sourceDeviceId),
    syncState: sanitizePasskeyText(syncState) || "local-only",
    attestationFormat: sanitizePasskeyText(attestationFormat),
    isRemovable: Boolean(isRemovable),
    isBackedUp: Boolean(isBackedUp),
    isFavorite: Boolean(isFavorite),
    deletedAt: deletedAt || null,
    createdAt: createdAt || now,
    updatedAt: updatedAt || now,
    lastUsedAt: lastUsedAt || null,
  };
}

export function createPasskeyListItem(record) {
  return {
    id: record.id,
    credentialId: record.credentialId,
    rpId: record.rpId,
    rpIdNormalized: record.rpIdNormalized || normalizeRpId(record.rpId),
    username: record.username || "",
    displayName: record.displayName || "",
    userHandle: record.userHandle || "",
    transportHints: sanitizeTransportHints(record.transportHints),
    authenticatorName: record.authenticatorName || "",
    nativeProviderRecordId: record.nativeProviderRecordId || "",
    origin: record.origin || "",
    sourceDeviceId: record.sourceDeviceId || "",
    syncState: record.syncState || "local-only",
    attestationFormat: record.attestationFormat || "",
    isRemovable: Boolean(record.isRemovable),
    isBackedUp: Boolean(record.isBackedUp),
    isFavorite: Boolean(record.isFavorite),
    deletedAt: record.deletedAt || null,
    createdAt: Number(record.createdAt || Date.now()),
    updatedAt: Number(record.updatedAt || record.createdAt || Date.now()),
    lastUsedAt: record.lastUsedAt ? Number(record.lastUsedAt) : null,
  };
}
