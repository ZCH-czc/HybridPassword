import { getAppLogsRecord, saveAppLogsRecord } from "@/utils/indexed-db";

const MAX_APP_LOG_ENTRIES = 300;

function normalizeLevel(level) {
  if (level === "error" || level === "warning" || level === "info") {
    return level;
  }

  return "info";
}

function normalizeSource(source) {
  return String(source || "app").trim() || "app";
}

function normalizeMessage(message) {
  return String(message || "").trim();
}

export function normalizeAppLogEntry(entry = {}) {
  return {
    id:
      entry.id ||
      `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`,
    timestampUnixTimeMs: Number(entry.timestampUnixTimeMs || Date.now()),
    level: normalizeLevel(entry.level),
    source: normalizeSource(entry.source),
    message: normalizeMessage(entry.message),
    details: String(entry.details || "").trim(),
    repeatCount: Math.max(1, Number(entry.repeatCount || 1)),
  };
}

export function mergeAppLogEntry(entries, entry) {
  const normalizedEntry = normalizeAppLogEntry(entry);
  const list = Array.isArray(entries) ? [...entries] : [];
  const latestEntry = list[0];

  if (
    latestEntry &&
    latestEntry.level === normalizedEntry.level &&
    latestEntry.source === normalizedEntry.source &&
    latestEntry.message === normalizedEntry.message &&
    latestEntry.details === normalizedEntry.details
  ) {
    list[0] = {
      ...latestEntry,
      timestampUnixTimeMs: normalizedEntry.timestampUnixTimeMs,
      repeatCount: Math.max(1, Number(latestEntry.repeatCount || 1)) + 1,
    };
    return list;
  }

  return [normalizedEntry, ...list].slice(0, MAX_APP_LOG_ENTRIES);
}

export async function loadAppLogs() {
  const record = await getAppLogsRecord();
  const entries = Array.isArray(record?.entries)
    ? record.entries.map(normalizeAppLogEntry)
    : [];

  return {
    entries,
    updatedAt: Number(record?.updatedAt || 0),
  };
}

export async function persistAppLogs(entries, updatedAt = Date.now()) {
  await saveAppLogsRecord({
    entries: Array.isArray(entries) ? entries.slice(0, MAX_APP_LOG_ENTRIES) : [],
    updatedAt: Number(updatedAt || Date.now()),
  });
}

function stringifyErrorReason(reason) {
  if (!reason) {
    return "";
  }

  if (typeof reason === "string") {
    return reason;
  }

  if (reason instanceof Error) {
    return reason.stack || reason.message || String(reason);
  }

  try {
    return JSON.stringify(reason, null, 2);
  } catch {
    return String(reason);
  }
}

export function installGlobalAppLogCapture(onLog) {
  if (typeof onLog !== "function" || typeof window === "undefined") {
    return () => {};
  }

  const handleError = (event) => {
    const details = [event.filename, event.lineno, event.colno]
      .filter((value) => value !== undefined && value !== null && value !== "")
      .join(":");

    onLog({
      level: "error",
      source: "window",
      message: event.message || "Unhandled runtime error",
      details:
        stringifyErrorReason(event.error) ||
        (details ? `at ${details}` : ""),
    });
  };

  const handleRejection = (event) => {
    onLog({
      level: "error",
      source: "promise",
      message: "Unhandled promise rejection",
      details: stringifyErrorReason(event.reason),
    });
  };

  window.addEventListener("error", handleError);
  window.addEventListener("unhandledrejection", handleRejection);

  return () => {
    window.removeEventListener("error", handleError);
    window.removeEventListener("unhandledrejection", handleRejection);
  };
}

export function formatAppLogsAsText({
  appName = "Password Vault",
  appVersion = "",
  platform = "",
  locale = "",
  entries = [],
} = {}) {
  const header = [
    `${appName} - Application Logs`,
    `Version: ${appVersion || "unknown"}`,
    `Platform: ${platform || "unknown"}`,
    `Locale: ${locale || "unknown"}`,
    `Exported At: ${new Date().toISOString()}`,
    "",
  ];

  const body = (Array.isArray(entries) ? entries : []).map((entry, index) => {
    const lines = [
      `#${index + 1}`,
      `Time: ${new Date(Number(entry.timestampUnixTimeMs || 0)).toISOString()}`,
      `Level: ${normalizeLevel(entry.level)}`,
      `Source: ${normalizeSource(entry.source)}`,
      `Repeat: ${Math.max(1, Number(entry.repeatCount || 1))}`,
      `Message: ${normalizeMessage(entry.message) || "-"}`,
    ];

    if (entry.details) {
      lines.push("Details:");
      lines.push(String(entry.details));
    }

    return lines.join("\n");
  });

  return [...header, ...body].join("\n\n");
}
