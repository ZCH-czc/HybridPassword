export const DEFAULT_GENERATOR_OPTIONS = {
  length: 16,
  includeUppercase: true,
  includeLowercase: true,
  includeNumbers: true,
  includeSymbols: true,
};

function pickRandomIndex(max) {
  if (globalThis.crypto?.getRandomValues) {
    const numbers = new Uint32Array(1);
    globalThis.crypto.getRandomValues(numbers);
    return numbers[0] % max;
  }

  return Math.floor(Math.random() * max);
}

export function buildCharset(options) {
  let charset = "";

  if (options.includeUppercase) {
    charset += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
  }

  if (options.includeLowercase) {
    charset += "abcdefghijklmnopqrstuvwxyz";
  }

  if (options.includeNumbers) {
    charset += "0123456789";
  }

  if (options.includeSymbols) {
    charset += "!@#$%^&*()-_=+[]{};:,.?/|";
  }

  return charset;
}

export function generateRandomPassword(options) {
  const length = Number(options.length);
  const charset = buildCharset(options);

  if (!length || length < 6 || length > 64) {
    throw new Error("密码长度请设置在 6 到 64 之间。");
  }

  if (!charset) {
    throw new Error("请至少选择一种字符类型。");
  }

  let result = "";

  for (let index = 0; index < length; index += 1) {
    result += charset[pickRandomIndex(charset.length)];
  }

  return result;
}

export function maskPassword(password) {
  return "•".repeat(Math.max(String(password || "").length, 10));
}
