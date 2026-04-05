const encoder = new TextEncoder();
const decoder = new TextDecoder();
const PBKDF2_ITERATIONS = 250000;
const VERIFY_TEXT = "password-manager-master-key-check-v1";

function ensureCryptoSupport() {
  if (!globalThis.crypto?.subtle) {
    throw new Error("当前 WebView 不支持 Web Crypto API，无法启用本地加密密码库。");
  }
}

function bytesToBase64(bytes) {
  let binary = "";
  const view = bytes instanceof Uint8Array ? bytes : new Uint8Array(bytes);

  for (let index = 0; index < view.length; index += 1) {
    binary += String.fromCharCode(view[index]);
  }

  return btoa(binary);
}

function base64ToBytes(base64) {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);

  for (let index = 0; index < binary.length; index += 1) {
    bytes[index] = binary.charCodeAt(index);
  }

  return bytes;
}

async function deriveAesKey(passphrase, saltBase64) {
  ensureCryptoSupport();

  const keyMaterial = await globalThis.crypto.subtle.importKey(
    "raw",
    encoder.encode(passphrase),
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

export async function encryptText(plainText, cryptoKey) {
  ensureCryptoSupport();

  const iv = globalThis.crypto.getRandomValues(new Uint8Array(12));
  const encryptedBuffer = await globalThis.crypto.subtle.encrypt(
    {
      name: "AES-GCM",
      iv,
    },
    cryptoKey,
    encoder.encode(String(plainText ?? ""))
  );

  return {
    iv: bytesToBase64(iv),
    cipherText: bytesToBase64(encryptedBuffer),
  };
}

export async function decryptText(payload, cryptoKey) {
  ensureCryptoSupport();

  const decryptedBuffer = await globalThis.crypto.subtle.decrypt(
    {
      name: "AES-GCM",
      iv: base64ToBytes(payload.iv),
    },
    cryptoKey,
    base64ToBytes(payload.cipherText)
  );

  return decoder.decode(decryptedBuffer);
}

export async function createVaultConfig(passphrase) {
  const salt = bytesToBase64(globalThis.crypto.getRandomValues(new Uint8Array(16)));
  const cryptoKey = await deriveAesKey(passphrase, salt);

  return {
    cryptoKey,
    config: {
      version: 1,
      salt,
      verification: await encryptText(VERIFY_TEXT, cryptoKey),
      createdAt: Date.now(),
    },
  };
}

export async function unlockVaultKey(passphrase, config) {
  const cryptoKey = await deriveAesKey(passphrase, config.salt);
  const text = await decryptText(config.verification, cryptoKey);

  if (text !== VERIFY_TEXT) {
    throw new Error("主密码不正确，无法解锁密码库。");
  }

  return cryptoKey;
}
