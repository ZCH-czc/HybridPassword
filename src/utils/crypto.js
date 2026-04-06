const encoder = new TextEncoder();
const decoder = new TextDecoder();

const PBKDF2_ITERATIONS = 250000;
const VERIFY_TEXT = "password-manager-vault-check-v2";
const LEGACY_VERIFY_TEXT = "password-manager-master-key-check-v1";
const SECRET_KEY_PREFIX = "PVSK";
const SECRET_KEY_GROUP_SIZE = 4;
const SECRET_KEY_RANDOM_BYTES = 20;
const SECRET_KEY_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function ensureCryptoSupport() {
  if (!globalThis.crypto?.subtle) {
    throw new Error("当前 WebView 不支持 Web Crypto API，无法启用本地加密密码库。");
  }
}

export function bytesToBase64(bytes) {
  let binary = "";
  const view = bytes instanceof Uint8Array ? bytes : new Uint8Array(bytes);

  for (let index = 0; index < view.length; index += 1) {
    binary += String.fromCharCode(view[index]);
  }

  return btoa(binary);
}

export function base64ToBytes(base64) {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);

  for (let index = 0; index < binary.length; index += 1) {
    bytes[index] = binary.charCodeAt(index);
  }

  return bytes;
}

function concatenateBytes(...chunks) {
  const totalLength = chunks.reduce((sum, chunk) => sum + chunk.length, 0);
  const merged = new Uint8Array(totalLength);
  let offset = 0;

  chunks.forEach((chunk) => {
    merged.set(chunk, offset);
    offset += chunk.length;
  });

  return merged;
}

function splitIntoGroups(value, groupSize = SECRET_KEY_GROUP_SIZE) {
  const groups = [];

  for (let index = 0; index < value.length; index += groupSize) {
    groups.push(value.slice(index, index + groupSize));
  }

  return groups;
}

function encodeBase32(bytes) {
  let bits = 0;
  let value = 0;
  let output = "";

  bytes.forEach((byte) => {
    value = (value << 8) | byte;
    bits += 8;

    while (bits >= 5) {
      output += SECRET_KEY_ALPHABET[(value >>> (bits - 5)) & 31];
      bits -= 5;
    }
  });

  if (bits > 0) {
    output += SECRET_KEY_ALPHABET[(value << (5 - bits)) & 31];
  }

  return output;
}

function normalizeSecretKeyInput(secretKey) {
  const compact = String(secretKey || "")
    .toUpperCase()
    .replace(/[\s-]/g, "");

  if (!compact) {
    return "";
  }

  if (!compact.startsWith(SECRET_KEY_PREFIX)) {
    throw new Error("Secret Key 格式不正确，请检查后重试。");
  }

  const body = compact.slice(SECRET_KEY_PREFIX.length);
  if (!body || !/^[A-Z2-7]+$/.test(body)) {
    throw new Error("Secret Key 格式不正确，请检查后重试。");
  }

  return `${SECRET_KEY_PREFIX}${body}`;
}

function formatSecretKey(compactSecretKey) {
  const normalized = normalizeSecretKeyInput(compactSecretKey);
  const body = normalized.slice(SECRET_KEY_PREFIX.length);
  return [SECRET_KEY_PREFIX, ...splitIntoGroups(body)].join("-");
}

function maskSecretKey(secretKey) {
  const formatted = formatSecretKey(secretKey);
  const segments = formatted.split("-");

  if (segments.length <= 2) {
    return formatted;
  }

  return segments
    .map((segment, index) => {
      if (index <= 1 || index === segments.length - 1) {
        return segment;
      }

      return "*".repeat(segment.length);
    })
    .join("-");
}

function generateSecretKey() {
  const randomBytes = globalThis.crypto.getRandomValues(new Uint8Array(SECRET_KEY_RANDOM_BYTES));
  const body = encodeBase32(randomBytes);
  return formatSecretKey(`${SECRET_KEY_PREFIX}${body}`);
}

async function sha256Bytes(...chunks) {
  const buffer = await globalThis.crypto.subtle.digest("SHA-256", concatenateBytes(...chunks));
  return new Uint8Array(buffer);
}

async function derivePassphraseBytes(passphrase, saltBase64) {
  ensureCryptoSupport();

  const keyMaterial = await globalThis.crypto.subtle.importKey(
    "raw",
    encoder.encode(String(passphrase || "")),
    "PBKDF2",
    false,
    ["deriveBits"]
  );

  const buffer = await globalThis.crypto.subtle.deriveBits(
    {
      name: "PBKDF2",
      salt: base64ToBytes(saltBase64),
      iterations: PBKDF2_ITERATIONS,
      hash: "SHA-256",
    },
    keyMaterial,
    256
  );

  return new Uint8Array(buffer);
}

async function importAesKeyFromBytes(rawBytes) {
  ensureCryptoSupport();

  return globalThis.crypto.subtle.importKey(
    "raw",
    rawBytes,
    {
      name: "AES-GCM",
      length: 256,
    },
    false,
    ["encrypt", "decrypt"]
  );
}

async function deriveLocalWrapKey(passphrase, saltBase64) {
  const rawBytes = await derivePassphraseBytes(passphrase, saltBase64);
  return importAesKeyFromBytes(rawBytes);
}

async function deriveRecoveryWrapKey(passphrase, saltBase64, secretKey) {
  const passphraseBytes = await derivePassphraseBytes(passphrase, saltBase64);
  const normalizedSecretKey = normalizeSecretKeyInput(secretKey);
  const combinedBytes = await sha256Bytes(
    encoder.encode("password-vault-recovery-wrap-v2"),
    passphraseBytes,
    encoder.encode(normalizedSecretKey)
  );

  return importAesKeyFromBytes(combinedBytes);
}

export async function importVaultKeyFromBase64(vaultKeyBase64) {
  return importAesKeyFromBytes(base64ToBytes(vaultKeyBase64));
}

export async function encryptBytes(plainBytes, cryptoKey) {
  ensureCryptoSupport();

  const iv = globalThis.crypto.getRandomValues(new Uint8Array(12));
  const encryptedBuffer = await globalThis.crypto.subtle.encrypt(
    {
      name: "AES-GCM",
      iv,
    },
    cryptoKey,
    plainBytes
  );

  return {
    iv: bytesToBase64(iv),
    cipherText: bytesToBase64(encryptedBuffer),
  };
}

export async function decryptBytes(payload, cryptoKey) {
  ensureCryptoSupport();

  const decryptedBuffer = await globalThis.crypto.subtle.decrypt(
    {
      name: "AES-GCM",
      iv: base64ToBytes(payload.iv),
    },
    cryptoKey,
    base64ToBytes(payload.cipherText)
  );

  return new Uint8Array(decryptedBuffer);
}

export async function encryptText(plainText, cryptoKey) {
  const encryptedPayload = await encryptBytes(encoder.encode(String(plainText ?? "")), cryptoKey);
  return encryptedPayload;
}

export async function decryptText(payload, cryptoKey) {
  const decryptedBytes = await decryptBytes(payload, cryptoKey);
  return decoder.decode(decryptedBytes);
}

async function verifyVaultKey(cryptoKey, config) {
  const text = await decryptText(config.verification, cryptoKey);
  if (text !== VERIFY_TEXT) {
    throw new Error("主密码或 Secret Key 不正确，无法解锁密码库。");
  }
}

async function unwrapLocalVaultKey(passphrase, config) {
  const localWrapKey = await deriveLocalWrapKey(passphrase, config.localSalt);
  const vaultKeyBytes = await decryptBytes(config.localWrappedVaultKey, localWrapKey);
  const vaultKeyBase64 = bytesToBase64(vaultKeyBytes);
  const vaultKey = await importVaultKeyFromBase64(vaultKeyBase64);
  await verifyVaultKey(vaultKey, config);

  return {
    vaultKey,
    vaultKeyBase64,
    usedRecovery: false,
  };
}

async function unwrapRecoveryVaultKey(passphrase, secretKey, config) {
  const recoveryWrapKey = await deriveRecoveryWrapKey(passphrase, config.recoverySalt, secretKey);
  const vaultKeyBytes = await decryptBytes(config.recoveryWrappedVaultKey, recoveryWrapKey);
  const vaultKeyBase64 = bytesToBase64(vaultKeyBytes);
  const vaultKey = await importVaultKeyFromBase64(vaultKeyBase64);
  await verifyVaultKey(vaultKey, config);

  return {
    vaultKey,
    vaultKeyBase64,
    usedRecovery: true,
    normalizedSecretKey: normalizeSecretKeyInput(secretKey),
  };
}

async function deriveLegacyAesKey(passphrase, saltBase64) {
  ensureCryptoSupport();

  const keyMaterial = await globalThis.crypto.subtle.importKey(
    "raw",
    encoder.encode(String(passphrase || "")),
    "PBKDF2",
    false,
    ["deriveKey"]
  );

  return globalThis.crypto.subtle.deriveKey(
    {
      name: "PBKDF2",
      salt: base64ToBytes(saltBase64),
      iterations: PBKDF2_ITERATIONS,
      hash: "SHA-256",
    },
    keyMaterial,
    {
      name: "AES-GCM",
      length: 256,
    },
    false,
    ["encrypt", "decrypt"]
  );
}

export async function createVaultConfig(passphrase, options = {}) {
  ensureCryptoSupport();

  const createdAt = Number(options.createdAt || Date.now());
  const vaultKeyBase64 =
    options.vaultKeyBase64 ||
    bytesToBase64(globalThis.crypto.getRandomValues(new Uint8Array(32)));
  const secretKey = options.secretKey ? formatSecretKey(options.secretKey) : generateSecretKey();
  const localSalt = bytesToBase64(globalThis.crypto.getRandomValues(new Uint8Array(16)));
  const recoverySalt = bytesToBase64(globalThis.crypto.getRandomValues(new Uint8Array(16)));
  const vaultKey = await importVaultKeyFromBase64(vaultKeyBase64);
  const localWrapKey = await deriveLocalWrapKey(passphrase, localSalt);
  const recoveryWrapKey = await deriveRecoveryWrapKey(passphrase, recoverySalt, secretKey);
  const vaultKeyBytes = base64ToBytes(vaultKeyBase64);

  const config = {
    version: 2,
    createdAt,
    localSalt,
    localWrappedVaultKey: await encryptBytes(vaultKeyBytes, localWrapKey),
    recoverySalt,
    recoveryWrappedVaultKey: await encryptBytes(vaultKeyBytes, recoveryWrapKey),
    verification: await encryptText(VERIFY_TEXT, vaultKey),
    secretKeyCipher: await encryptText(secretKey, vaultKey),
    secretKeyHint: maskSecretKey(secretKey),
    migratedFromVersion: Number(options.migratedFromVersion || 0) || null,
  };

  return {
    config,
    vaultKey,
    vaultKeyBase64,
    secretKey,
  };
}

export async function ensureLocalVaultWrap(passphrase, config, vaultKeyBase64) {
  if (config.version < 2) {
    return config;
  }

  if (config.localWrappedVaultKey && config.localSalt) {
    return config;
  }

  const localSalt = bytesToBase64(globalThis.crypto.getRandomValues(new Uint8Array(16)));
  const localWrapKey = await deriveLocalWrapKey(passphrase, localSalt);
  const vaultKeyBytes = base64ToBytes(vaultKeyBase64);

  return {
    ...config,
    localSalt,
    localWrappedVaultKey: await encryptBytes(vaultKeyBytes, localWrapKey),
  };
}

export async function rewrapVaultConfig(passphrase, currentConfig, vaultKeyBase64, secretKey) {
  return createVaultConfig(passphrase, {
    createdAt: currentConfig?.createdAt || Date.now(),
    vaultKeyBase64,
    secretKey,
    migratedFromVersion: currentConfig?.version >= 2 ? null : currentConfig?.version || 1,
  });
}

export async function unlockVaultKey(passphrase, config, options = {}) {
  if (!config?.version || config.version < 2) {
    const legacyKey = await deriveLegacyAesKey(passphrase, config.salt);
    const text = await decryptText(config.verification, legacyKey);

    if (text !== LEGACY_VERIFY_TEXT) {
      throw new Error("主密码不正确，无法解锁密码库。");
    }

    return {
      scheme: "legacy",
      vaultKey: legacyKey,
      vaultKeyBase64: "",
      usedRecovery: false,
      normalizedSecretKey: "",
    };
  }

  const normalizedSecretKey = options.secretKey
    ? normalizeSecretKeyInput(options.secretKey)
    : "";

  if (config.localWrappedVaultKey && config.localSalt) {
    try {
      const localResult = await unwrapLocalVaultKey(passphrase, config);
      return {
        scheme: "local",
        ...localResult,
        normalizedSecretKey: "",
      };
    } catch (error) {
      if (!normalizedSecretKey) {
        throw error;
      }
    }
  }

  if (!normalizedSecretKey) {
    throw new Error("这个密码库还需要 Secret Key 才能完成解锁。");
  }

  return {
    scheme: "recovery",
    ...(await unwrapRecoveryVaultKey(passphrase, normalizedSecretKey, config)),
  };
}

export async function revealSecretKey(config, vaultKey) {
  if (!config?.secretKeyCipher) {
    throw new Error("当前密码库没有可显示的 Secret Key。");
  }

  return decryptText(config.secretKeyCipher, vaultKey);
}

export function exportVaultConfigForSync(config) {
  if (!config || config.version < 2) {
    return config;
  }

  return {
    version: 2,
    createdAt: config.createdAt,
    recoverySalt: config.recoverySalt,
    recoveryWrappedVaultKey: config.recoveryWrappedVaultKey,
    verification: config.verification,
    secretKeyCipher: config.secretKeyCipher,
    secretKeyHint: config.secretKeyHint || "",
    migratedFromVersion: config.migratedFromVersion || null,
  };
}

export function isSecretKeyRequiredForConfig(config) {
  return Boolean(config?.version >= 2 && (!config.localWrappedVaultKey || !config.localSalt));
}

export function formatGeneratedSecretKey(secretKey) {
  return formatSecretKey(secretKey);
}
