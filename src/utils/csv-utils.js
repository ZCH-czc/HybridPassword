import { normalizeUsername, sanitizeNotes, sanitizeText } from "@/models/password-item";

function escapeCsvValue(value) {
  const text = String(value ?? "");

  if (/[",\n]/.test(text)) {
    return `"${text.replace(/"/g, '""')}"`;
  }

  return text;
}

function parseCsvRows(text) {
  const rows = [];
  let row = [];
  let current = "";
  let inQuotes = false;

  for (let index = 0; index < text.length; index += 1) {
    const char = text[index];
    const nextChar = text[index + 1];

    if (inQuotes) {
      if (char === '"' && nextChar === '"') {
        current += '"';
        index += 1;
      } else if (char === '"') {
        inQuotes = false;
      } else {
        current += char;
      }
      continue;
    }

    if (char === '"') {
      inQuotes = true;
      continue;
    }

    if (char === ",") {
      row.push(current);
      current = "";
      continue;
    }

    if (char === "\n") {
      row.push(current);
      rows.push(row);
      row = [];
      current = "";
      continue;
    }

    if (char !== "\r") {
      current += char;
    }
  }

  if (current.length || row.length) {
    row.push(current);
    rows.push(row);
  }

  return rows.filter((cells) => cells.some((cell) => String(cell).trim() !== ""));
}

export function buildCsvContent(entries) {
  const headers = ["siteName", "username", "password", "notes", "createdAt", "updatedAt"];
  const lines = entries.map((entry) =>
    [
      entry.siteName || "",
      entry.username,
      entry.password,
      entry.notes.join(" | "),
      new Date(entry.createdAt).toISOString(),
      new Date(entry.updatedAt).toISOString(),
    ]
      .map(escapeCsvValue)
      .join(",")
  );

  return [headers.join(","), ...lines].join("\n");
}

export function buildTxtContent(entries) {
  return entries
    .map((entry, index) => {
      const notesText = entry.notes.length
        ? entry.notes.map((note) => `- ${note}`).join("\n")
        : "- 无";

      return [
        `# ${index + 1} ${entry.siteName || "未命名项目"}`,
        `用户名: ${entry.username}`,
        `密码: ${entry.password}`,
        "备注:",
        notesText,
        `创建时间: ${new Date(entry.createdAt).toLocaleString()}`,
        `更新时间: ${new Date(entry.updatedAt).toLocaleString()}`,
      ].join("\n");
    })
    .join("\n\n");
}

export async function readTextFile(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result || ""));
    reader.onerror = () => reject(reader.error || new Error("读取文件失败。"));
    reader.readAsText(file, "utf-8");
  });
}

export function parseImportedEntries(text) {
  const rows = parseCsvRows(String(text || ""));

  if (rows.length < 2) {
    throw new Error("CSV 内容为空或格式不正确。");
  }

  const headers = rows[0].map((header) => String(header).trim().toLowerCase());
  const siteNameIndex = [headers.indexOf("sitename"), headers.indexOf("title"), headers.indexOf("website")].find(
    (index) => index >= 0
  );
  const usernameIndex = headers.indexOf("username");
  const passwordIndex = headers.indexOf("password");
  const notesIndex = headers.indexOf("notes");
  const createdAtIndex = headers.indexOf("createdat");
  const updatedAtIndex = headers.indexOf("updatedat");

  if (usernameIndex < 0 || passwordIndex < 0) {
    throw new Error("CSV 缺少必要列：username、password。");
  }

  return rows
    .slice(1)
    .map((row) => {
      const createdAt = createdAtIndex >= 0 ? Date.parse(row[createdAtIndex] || "") : NaN;
      const updatedAt = updatedAtIndex >= 0 ? Date.parse(row[updatedAtIndex] || "") : NaN;

      return {
        siteName: siteNameIndex >= 0 ? sanitizeText(row[siteNameIndex]) : "",
        username: sanitizeText(row[usernameIndex]),
        usernameNormalized: normalizeUsername(row[usernameIndex]),
        password: String(row[passwordIndex] || ""),
        notes:
          notesIndex >= 0 ? sanitizeNotes(String(row[notesIndex] || "").split("|")) : [],
        createdAt: Number.isNaN(createdAt) ? Date.now() : createdAt,
        updatedAt: Number.isNaN(updatedAt) ? Date.now() : updatedAt,
      };
    })
    .filter((entry) => entry.username && entry.password);
}

export function collapseImportedEntries(entries, strategy) {
  const entryMap = new Map();
  const order = [];
  let skippedDuplicates = 0;

  entries.forEach((entry) => {
    const key = entry.usernameNormalized;
    const exists = entryMap.has(key);

    if (!exists) {
      order.push(key);
      entryMap.set(key, entry);
      return;
    }

    if (strategy === "skip") {
      skippedDuplicates += 1;
      return;
    }

    entryMap.set(key, entry);
  });

  return {
    entries: order.map((key) => entryMap.get(key)),
    skippedDuplicates,
  };
}
