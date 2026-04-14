import { normalizeUsername, sanitizeNotes, sanitizeText } from "@/models/password-item";

function escapeCsvValue(value) {
  const text = String(value ?? "");

  if (/[",\n]/.test(text)) {
    return `"${text.replace(/"/g, '""')}"`;
  }

  return text;
}

function normalizeHeaderKey(value) {
  return String(value || "")
    .trim()
    .toLowerCase()
    .replace(/[\s_-]+/g, "");
}

function pickHeaderIndex(headers, candidates) {
  return candidates
    .map((candidate) => headers.indexOf(candidate))
    .find((index) => index >= 0);
}

function splitNotesText(value) {
  const text = String(value || "").trim();
  if (!text) {
    return [];
  }

  if (text.includes(" | ")) {
    return sanitizeNotes(text.split("|"));
  }

  return sanitizeNotes(text.split(/\r?\n+/));
}

function parseTimestamp(value) {
  if (value == null || value === "") {
    return NaN;
  }

  if (typeof value === "number" && Number.isFinite(value)) {
    return value > 10_000_000_000 ? value : value * 1000;
  }

  const numericValue = Number(value);
  if (Number.isFinite(numericValue)) {
    return numericValue > 10_000_000_000 ? numericValue : numericValue * 1000;
  }

  const parsed = Date.parse(String(value));
  return Number.isNaN(parsed) ? NaN : parsed;
}

function parseBooleanLike(value) {
  const normalized = String(value || "")
    .trim()
    .toLowerCase();

  return ["1", "true", "yes", "y", "favorite", "favourite"].includes(normalized);
}

function deriveSiteNameFromUrl(urlValue) {
  const text = String(urlValue || "").trim();
  if (!text) {
    return "";
  }

  try {
    const url = new URL(text.includes("://") ? text : `https://${text}`);
    return url.hostname.replace(/^www\./i, "");
  } catch {
    return text;
  }
}

function appendNote(notes, label, value) {
  const safeValue = sanitizeText(value);
  if (!safeValue) {
    return;
  }

  const safeLabel = sanitizeText(label);
  notes.push(safeLabel ? `${safeLabel}: ${safeValue}` : safeValue);
}

function createIgnoredImportItem({
  siteName,
  username,
  password,
  notes,
  reason,
  sourceType = "csv",
}) {
  return {
    id: `${sourceType}-${Date.now()}-${Math.random().toString(16).slice(2, 10)}`,
    siteName: sanitizeText(siteName),
    username: sanitizeText(username),
    password: String(password || ""),
    notes: sanitizeNotes(notes),
    reason: sanitizeText(reason) || "Unsupported item",
    sourceType,
  };
}

function buildImportedEntry({
  siteName,
  username,
  password,
  notes,
  createdAt,
  updatedAt,
  isFavorite,
}) {
  const safePassword = String(password || "");
  const safeUsername = sanitizeText(username);
  const safeSiteName = sanitizeText(siteName);
  const normalizedNotes = sanitizeNotes(notes);
  const safeCreatedAt = Number.isNaN(createdAt) ? Date.now() : createdAt;
  const safeUpdatedAt = Number.isNaN(updatedAt) ? safeCreatedAt : updatedAt;

  return {
    siteName: safeSiteName,
    username: safeUsername,
    usernameNormalized: normalizeUsername(safeUsername),
    password: safePassword,
    notes: normalizedNotes,
    createdAt: safeCreatedAt,
    updatedAt: safeUpdatedAt,
    isFavorite: Boolean(isFavorite),
  };
}

export function parseCsvRows(text) {
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
        : "- No notes";

      return [
        `# ${index + 1} ${entry.siteName || "Unnamed item"}`,
        `Username: ${entry.username}`,
        `Password: ${entry.password}`,
        "Notes:",
        notesText,
        `Created: ${new Date(entry.createdAt).toLocaleString()}`,
        `Updated: ${new Date(entry.updatedAt).toLocaleString()}`,
      ].join("\n");
    })
    .join("\n\n");
}

export async function readTextFile(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result || ""));
    reader.onerror = () => reject(reader.error || new Error("Unable to read the selected file."));
    reader.readAsText(file, "utf-8");
  });
}

export function parseCsvImportPayload(text, { fileName = "" } = {}) {
  const rows = parseCsvRows(String(text || ""));

  if (rows.length < 2) {
    throw new Error("The selected CSV file is empty or invalid.");
  }

  const rawHeaders = rows[0].map((header) => String(header).trim());
  const headers = rawHeaders.map(normalizeHeaderKey);
  const titleIndex = pickHeaderIndex(headers, ["sitename", "title", "name", "itemtitle"]);
  const urlIndex = pickHeaderIndex(headers, ["url", "website", "websiteurl", "loginurl", "signinurl"]);
  const usernameIndex = pickHeaderIndex(headers, ["username", "email", "login", "emailaddress"]);
  const passwordIndex = pickHeaderIndex(headers, ["password", "passcode", "passwd"]);
  const notesIndex = pickHeaderIndex(headers, ["notes", "note", "comment", "comments", "notesplain"]);
  const createdAtIndex = pickHeaderIndex(headers, [
    "createdat",
    "created",
    "createddate",
    "createdon",
  ]);
  const updatedAtIndex = pickHeaderIndex(headers, [
    "updatedat",
    "updated",
    "modified",
    "modifiedat",
    "lastedited",
    "lastmodified",
  ]);
  const favoriteIndex = pickHeaderIndex(headers, ["isfavorite", "favorite", "favourite", "starred"]);

  if (passwordIndex == null || passwordIndex < 0) {
    throw new Error("The selected CSV file does not contain a password column.");
  }

  const knownIndexes = new Set(
    [
      titleIndex,
      urlIndex,
      usernameIndex,
      passwordIndex,
      notesIndex,
      createdAtIndex,
      updatedAtIndex,
      favoriteIndex,
    ].filter((index) => index >= 0)
  );
  const isOnePasswordCsv =
    headers.includes("vault") ||
    headers.includes("tags") ||
    headers.includes("otpauth") ||
    headers.includes("archived") ||
    fileName.toLowerCase().includes("1password");

  let ignored = 0;
  const ignoredItems = [];
  const entries = rows
    .slice(1)
    .map((row) => {
      const password = String(row[passwordIndex] || "");
      const username = usernameIndex >= 0 ? row[usernameIndex] : "";
      const primaryUrl = urlIndex >= 0 ? row[urlIndex] : "";
      const title = titleIndex >= 0 ? row[titleIndex] : "";
      const siteName = sanitizeText(title) || deriveSiteNameFromUrl(primaryUrl);

      const notes = [];
      splitNotesText(notesIndex >= 0 ? row[notesIndex] : "").forEach((note) => notes.push(note));

      if (primaryUrl) {
        appendNote(notes, "URL", primaryUrl);
      }

      rawHeaders.forEach((headerLabel, index) => {
        if (knownIndexes.has(index)) {
          return;
        }

        const value = sanitizeText(row[index]);
        if (!value) {
          return;
        }

        appendNote(notes, headerLabel, value);
      });

      if (!sanitizeText(password)) {
        ignored += 1;
        ignoredItems.push(
          createIgnoredImportItem({
            siteName,
            username,
            password,
            notes,
            reason: "No password value was found in this row.",
            sourceType: isOnePasswordCsv ? "1password-csv" : "csv",
          })
        );
        return null;
      }

      const favorite = favoriteIndex >= 0 ? parseBooleanLike(row[favoriteIndex]) : false;
      const createdAt = createdAtIndex >= 0 ? parseTimestamp(row[createdAtIndex]) : NaN;
      const updatedAt = updatedAtIndex >= 0 ? parseTimestamp(row[updatedAtIndex]) : NaN;

      return buildImportedEntry({
        siteName,
        username,
        password,
        notes,
        createdAt,
        updatedAt,
        isFavorite: favorite,
      });
    })
    .filter(Boolean);

  if (!entries.length) {
    throw new Error("No compatible password entries were found in the selected CSV file.");
  }

  return {
    source: isOnePasswordCsv ? "1password-csv" : "csv",
    sourceLabel: isOnePasswordCsv ? "1Password CSV" : "CSV",
    entries,
    ignored,
    ignoredItems,
  };
}

export function createImportEntryMergeKey(entry) {
  const siteKey = sanitizeText(entry?.siteName).toLowerCase();
  const usernameKey = normalizeUsername(entry?.username);

  if (siteKey || usernameKey) {
    return `${siteKey}::${usernameKey}`;
  }

  return `password::${String(entry?.password || "").slice(0, 64)}`;
}

export function collapseImportedEntries(entries, strategy, keyBuilder = createImportEntryMergeKey) {
  const entryMap = new Map();
  const order = [];
  let skippedDuplicates = 0;

  entries.forEach((entry) => {
    const key = keyBuilder(entry);
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
