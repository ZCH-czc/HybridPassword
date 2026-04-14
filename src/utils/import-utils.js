import { strFromU8, unzipSync } from "fflate";
import { parseCsvImportPayload } from "@/utils/csv-utils";
import { normalizeUsername, sanitizeNotes, sanitizeText } from "@/models/password-item";

const ZIP_SIGNATURE = [0x50, 0x4b, 0x03, 0x04];

function isZipBytes(bytes) {
  return ZIP_SIGNATURE.every((signatureByte, index) => bytes[index] === signatureByte);
}

function decodeUtf8(bytes) {
  return new TextDecoder("utf-8").decode(bytes);
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

function dedupeValues(values) {
  return [...new Set((values || []).map((value) => sanitizeText(value)).filter(Boolean))];
}

function normalizeImportedEntry({
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
  const safeCreatedAt = Number.isNaN(createdAt) ? Date.now() : createdAt;
  const safeUpdatedAt = Number.isNaN(updatedAt) ? safeCreatedAt : updatedAt;

  return {
    siteName: safeSiteName,
    username: safeUsername,
    usernameNormalized: normalizeUsername(safeUsername),
    password: safePassword,
    notes: sanitizeNotes(notes),
    createdAt: safeCreatedAt,
    updatedAt: safeUpdatedAt,
    isFavorite: Boolean(isFavorite),
  };
}

function createIgnoredImportItem({
  siteName,
  username,
  password,
  notes,
  reason,
  sourceType = "1password-1pux",
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

function extractFieldValue(value) {
  if (value == null) {
    return "";
  }

  if (typeof value === "string" || typeof value === "number" || typeof value === "boolean") {
    return String(value);
  }

  if (Array.isArray(value)) {
    return value.map(extractFieldValue).filter(Boolean).join(", ");
  }

  if (typeof value === "object") {
    if (typeof value.concealed === "string") {
      return value.concealed;
    }

    if (typeof value.url === "string") {
      return value.url;
    }

    if (typeof value.totp === "string") {
      return value.totp;
    }

    if (typeof value.label === "string") {
      return value.label;
    }

    const flattenedValues = [];
    Object.entries(value).forEach(([key, nestedValue]) => {
      if (nestedValue == null || ["id", "uuid", "reference", "type"].includes(String(key))) {
        return;
      }

      const nestedText = extractFieldValue(nestedValue);
      if (nestedText) {
        flattenedValues.push(nestedText);
      }
    });

    return dedupeValues(flattenedValues).join(", ");
  }

  return "";
}

function buildFieldLabel(field, sectionTitle = "") {
  return [sectionTitle, field?.title || field?.name || field?.id || field?.designation]
    .map((part) => sanitizeText(part))
    .filter(Boolean)
    .join(" / ");
}

function looksLikeUsernameField(field) {
  const hint = `${field?.designation || ""} ${field?.id || ""} ${field?.name || ""} ${field?.title || ""}`.toLowerCase();
  return (
    hint.includes("username") ||
    hint.includes("user name") ||
    hint.includes("email") ||
    hint.includes("login")
  );
}

function looksLikePasswordField(field) {
  const hint = `${field?.designation || ""} ${field?.id || ""} ${field?.name || ""} ${field?.title || ""}`.toLowerCase();
  return (
    hint.includes("password") ||
    hint.includes("passcode") ||
    hint.includes("pin") ||
    hint.includes("secret")
  );
}

function extractPrimaryCredentials(item) {
  let username = "";
  let password = "";

  const loginFields = Array.isArray(item?.details?.loginFields) ? item.details.loginFields : [];
  loginFields.forEach((field) => {
    const rawValue = extractFieldValue(field?.value);
    if (!rawValue) {
      return;
    }

    const fieldType = String(field?.fieldType || field?.type || "").toUpperCase();
    const designation = String(field?.designation || "").toLowerCase();

    if (!username && (designation === "username" || looksLikeUsernameField(field) || ["E", "U", "T"].includes(fieldType))) {
      username = rawValue;
      return;
    }

    if (!password && (designation === "password" || looksLikePasswordField(field) || fieldType === "P")) {
      password = rawValue;
    }
  });

  const sections = Array.isArray(item?.details?.sections) ? item.details.sections : [];
  sections.forEach((section) => {
    const fields = Array.isArray(section?.fields) ? section.fields : [];
    fields.forEach((field) => {
      const rawValue = extractFieldValue(field?.value);
      if (!rawValue) {
        return;
      }

      if (!username && looksLikeUsernameField(field)) {
        username = rawValue;
        return;
      }

      if (!password && looksLikePasswordField(field)) {
        password = rawValue;
      }
    });
  });

  return {
    username: sanitizeText(username),
    password: String(password || ""),
  };
}

function collectUrls(item) {
  const urls = [];
  const overview = item?.overview || {};

  if (overview.url) {
    urls.push(String(overview.url));
  }

  const otherUrls = Array.isArray(overview.urls) ? overview.urls : [];
  otherUrls.forEach((urlItem) => {
    const value = extractFieldValue(urlItem?.url || urlItem?.href || urlItem?.value);
    if (value) {
      urls.push(value);
    }
  });

  return [...new Set(urls.filter(Boolean))];
}

function collectAdditionalLoginFieldNotes(details, { primaryUsername, primaryPassword }) {
  const notes = [];
  const loginFields = Array.isArray(details?.loginFields) ? details.loginFields : [];

  loginFields.forEach((field) => {
    const value = extractFieldValue(field?.value);
    if (!value || value === primaryPassword || value === primaryUsername) {
      return;
    }

    const label = buildFieldLabel(field, "Login field");
    appendNote(notes, label || "Login field", value);
  });

  return notes;
}

function collectStructuredFieldNotes(fields, sectionTitle, { primaryUsername, primaryPassword }) {
  const notes = [];

  (Array.isArray(fields) ? fields : []).forEach((field) => {
    const value = extractFieldValue(field?.value);
    if (!value || value === primaryPassword || value === primaryUsername) {
      return;
    }

    const label = buildFieldLabel(field, sectionTitle);
    appendNote(notes, label || "Field", value);
  });

  return notes;
}

function collectCustomNotes(
  item,
  { accountName, accountEmail, accountDomain, vaultName, vaultDescription, primaryUsername, primaryPassword }
) {
  const notes = [];
  const overview = item?.overview || {};
  const details = item?.details || {};

  if (details.notesPlain) {
    notes.push(String(details.notesPlain));
  }

  collectUrls(item).forEach((urlValue, index) => {
    appendNote(notes, index === 0 ? "URL" : `Additional URL ${index}`, urlValue);
  });

  const tags = Array.isArray(overview?.tags)
    ? overview.tags
    : Array.isArray(item?.tags)
      ? item.tags
      : [];
  if (tags.length) {
    appendNote(notes, "Tags", tags.join(", "));
  }

  if (accountName) {
    appendNote(notes, "1Password account", accountName);
  }

  if (vaultName) {
    appendNote(notes, "1Password vault", vaultName);
  }

  if (vaultDescription) {
    appendNote(notes, "Vault description", vaultDescription);
  }

  if (accountEmail) {
    appendNote(notes, "1Password email", accountEmail);
  }

  if (accountDomain) {
    appendNote(notes, "1Password domain", accountDomain);
  }

  if (overview.subtitle && sanitizeText(overview.subtitle) !== sanitizeText(primaryUsername)) {
    appendNote(notes, "Subtitle", overview.subtitle);
  }

  collectAdditionalLoginFieldNotes(details, {
    primaryUsername,
    primaryPassword,
  }).forEach((note) => notes.push(note));

  const sections = Array.isArray(details.sections) ? details.sections : [];
  sections.forEach((section) => {
    collectStructuredFieldNotes(section?.fields, section?.title, {
      primaryUsername,
      primaryPassword,
    }).forEach((note) => notes.push(note));
  });

  collectStructuredFieldNotes(details?.fields, "", {
    primaryUsername,
    primaryPassword,
  }).forEach((note) => notes.push(note));

  return sanitizeNotes(notes);
}

function map1PasswordItemToEntry(item, context) {
  const { username, password } = extractPrimaryCredentials(item);
  const urls = collectUrls(item);
  const overview = item?.overview || {};
  const siteName =
    sanitizeText(overview.title) ||
    deriveSiteNameFromUrl(urls[0]) ||
    sanitizeText(overview.subtitle);

  const usernameFallback =
    username ||
    (sanitizeText(overview.subtitle).includes("@") ? sanitizeText(overview.subtitle) : "");

  const notes = collectCustomNotes(item, {
    accountName: context.accountName,
    vaultName: context.vaultName,
    primaryUsername: usernameFallback,
    primaryPassword: password,
  });

  if (!sanitizeText(password)) {
    return {
      entry: null,
      ignoredItem: createIgnoredImportItem({
        siteName,
        username: usernameFallback,
        password,
        notes,
        reason: "No password field was found in this 1Password item.",
        sourceType: "1password-1pux",
      }),
    };
  }

  return {
    entry: normalizeImportedEntry({
      siteName,
      username: usernameFallback,
      password,
      notes,
      createdAt: parseTimestamp(item?.createdAt),
      updatedAt: parseTimestamp(item?.updatedAt || item?.state?.updatedAt),
      isFavorite: Number(item?.favIndex || 0) > 0,
    }),
    ignoredItem: null,
  };
}

function collectItemsFromVault(account, vault, entries) {
  const items = Array.isArray(vault?.items) ? vault.items : [];
  items.forEach((item) => {
    entries.push({
      accountName: sanitizeText(
        account?.attrs?.accountName || account?.attrs?.name || account?.name || account?.email
      ),
      accountEmail: sanitizeText(account?.attrs?.email || account?.email),
      accountDomain: sanitizeText(account?.attrs?.domain || account?.domain),
      vaultName: sanitizeText(vault?.attrs?.name || vault?.name),
      vaultDescription: sanitizeText(vault?.attrs?.desc || vault?.desc),
      item,
    });
  });
}

function extract1PasswordItems(payload) {
  const entries = [];
  const accounts = Array.isArray(payload?.accounts) ? payload.accounts : [];

  accounts.forEach((account) => {
    const vaults = Array.isArray(account?.vaults) ? account.vaults : [];
    vaults.forEach((vault) => {
      collectItemsFromVault(account, vault, entries);
    });
  });

  if (!entries.length && Array.isArray(payload?.vaults)) {
    payload.vaults.forEach((vault) => {
      collectItemsFromVault({}, vault, entries);
    });
  }

  if (!entries.length && Array.isArray(payload?.items)) {
    payload.items.forEach((item) => {
      entries.push({
        accountName: "",
        vaultName: "",
        item,
      });
    });
  }

  return entries;
}

export function parse1Password1pux(bytes) {
  let archive;
  try {
    archive = unzipSync(bytes);
  } catch {
    throw new Error("The selected 1Password archive could not be opened.");
  }

  const exportDataBytes = archive["export.data"];
  if (!exportDataBytes) {
    throw new Error("The selected 1Password archive does not contain export.data.");
  }

  let payload;
  try {
    payload = JSON.parse(strFromU8(exportDataBytes));
  } catch {
    throw new Error("The selected 1Password export could not be parsed.");
  }

  const candidates = extract1PasswordItems(payload);
  let ignored = 0;
  const ignoredItems = [];
  const entries = candidates
    .map((candidate) => {
      const mapped = map1PasswordItemToEntry(candidate.item, candidate);
      if (mapped?.ignoredItem) {
        ignored += 1;
        ignoredItems.push(mapped.ignoredItem);
      }
      return mapped?.entry || null;
    })
    .filter(Boolean);

  if (!entries.length) {
    throw new Error("No compatible login items were found in the selected 1Password export.");
  }

  return {
    source: "1password-1pux",
    sourceLabel: "1Password 1PUX",
    entries,
    ignored,
    ignoredItems,
  };
}

export function parseImportedFileData({ fileName = "", bytes }) {
  const safeBytes =
    bytes instanceof Uint8Array ? bytes : new Uint8Array(bytes || new ArrayBuffer(0));
  const lowerFileName = String(fileName || "").toLowerCase();

  if (lowerFileName.endsWith(".1pux") || (safeBytes.length >= 4 && isZipBytes(safeBytes))) {
    return parse1Password1pux(safeBytes);
  }

  return parseCsvImportPayload(decodeUtf8(safeBytes), { fileName });
}
