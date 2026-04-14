const FALLBACK_STATE = {
  isSupported: false,
  isBiometricAvailable: false,
  isBiometricEnabled: false,
  supportsNativeFileDialogs: false,
  biometricLabel: "生物识别",
  platform: "web",
  message: "",
  safeAreaTop: 0,
  safeAreaBottom: 0,
  supportsMinimizeToTray: false,
  minimizeToTrayEnabled: false,
  supportsLaunchAtStartup: false,
  launchAtStartupEnabled: false,
  trayAutoLockMinutes: 0,
  supportsExcludeFromRecents: false,
  excludeFromRecentsEnabled: false,
  backgroundAutoLockMinutes: 0,
  supportsAutostartSettingsShortcut: false,
  supportsWebDavSync: false,
  supportsLanSync: false,
  supportsPasskeys: false,
};

let hybridBridgeLoader = null;
const bridgeEncoder = new TextEncoder();

function bytesToBase64(bytes) {
  let binary = "";
  bytes.forEach((byte) => {
    binary += String.fromCharCode(byte);
  });
  return btoa(binary);
}

function base64ToBytes(value) {
  const base64 = String(value || "");
  if (!base64) {
    return new Uint8Array();
  }

  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);

  for (let index = 0; index < binary.length; index += 1) {
    bytes[index] = binary.charCodeAt(index);
  }

  return bytes;
}

function getHybridBridge() {
  return globalThis.HybridWebView &&
    typeof globalThis.HybridWebView.InvokeDotNet === "function"
    ? globalThis.HybridWebView
    : null;
}

function ensureHybridBridgeLoaded() {
  const bridge = getHybridBridge();
  if (bridge) {
    return Promise.resolve(bridge);
  }

  if (hybridBridgeLoader) {
    return hybridBridgeLoader;
  }

  hybridBridgeLoader = new Promise((resolve) => {
    const script = document.createElement("script");
    script.src = "./_framework/hybridwebview.js";
    script.async = true;
    script.onload = () => resolve(getHybridBridge());
    script.onerror = () => resolve(null);
    document.head.appendChild(script);
  });

  return hybridBridgeLoader;
}

function normalizeHostState(result = {}) {
  return {
    isSupported: Boolean(result.IsSupported),
    isBiometricAvailable: Boolean(result.IsBiometricAvailable),
    isBiometricEnabled: Boolean(result.IsBiometricEnabled),
    supportsNativeFileDialogs: Boolean(result.SupportsNativeFileDialogs),
    biometricLabel: result.BiometricLabel || "生物识别",
    platform: result.Platform || "web",
    message: result.Message || "",
    safeAreaTop: Number(result.SafeAreaTop || 0),
    safeAreaBottom: Number(result.SafeAreaBottom || 0),
    supportsMinimizeToTray: Boolean(result.SupportsMinimizeToTray),
    minimizeToTrayEnabled: Boolean(result.MinimizeToTrayEnabled),
    supportsLaunchAtStartup: Boolean(result.SupportsLaunchAtStartup),
    launchAtStartupEnabled: Boolean(result.LaunchAtStartupEnabled),
    trayAutoLockMinutes: Number(result.TrayAutoLockMinutes || 0),
    supportsExcludeFromRecents: Boolean(result.SupportsExcludeFromRecents),
    excludeFromRecentsEnabled: Boolean(result.ExcludeFromRecentsEnabled),
    backgroundAutoLockMinutes: Number(result.BackgroundAutoLockMinutes || 0),
    supportsAutostartSettingsShortcut: Boolean(result.SupportsAutostartSettingsShortcut),
    supportsWebDavSync: Boolean(result.SupportsWebDavSync),
    supportsLanSync: Boolean(result.SupportsLanSync),
    supportsPasskeys: Boolean(result.SupportsPasskeys),
  };
}

function normalizePasskeyState(result = {}) {
  return {
    isSupported: Boolean(result.IsSupported),
    supportsMetadataManagement: Boolean(result.SupportsMetadataManagement),
    supportsPluginManager: Boolean(result.SupportsPluginManager),
    requiresCompanionApp: Boolean(result.RequiresCompanionApp),
    companionAppIntegrated: Boolean(result.CompanionAppIntegrated),
    canLaunchCompanionApp: Boolean(result.CanLaunchCompanionApp),
    supportsCompanionAutoLaunch: Boolean(result.SupportsCompanionAutoLaunch),
    companionAutoLaunchEnabled: Boolean(result.CompanionAutoLaunchEnabled),
    apiVersion: Number(result.ApiVersion || 0),
    hasPlatformAuthenticator: Boolean(result.HasPlatformAuthenticator),
    platform: result.Platform || "web",
    message: result.Message || "",
    pluginStatus: result.PluginStatus || "",
    companionCheckedAtUnixTimeMs: Number(result.CompanionCheckedAtUnixTimeMs || 0),
    companionBuildNumber: Number(result.CompanionBuildNumber || 0),
    companionUbr: Number(result.CompanionUbr || 0),
    companionMeetsPluginBuildRequirement: Boolean(result.CompanionMeetsPluginBuildRequirement),
    companionWebAuthnLibraryAvailable: Boolean(result.CompanionWebAuthnLibraryAvailable),
    companionPluginExportsAvailable: Boolean(result.CompanionPluginExportsAvailable),
    companionIsPackagedProcess: Boolean(result.CompanionIsPackagedProcess),
    companionStatusSummary: result.CompanionStatusSummary || "",
    companionDetailMessage: result.CompanionDetailMessage || "",
    companionWorkflowMode: result.CompanionWorkflowMode || "skeleton",
    companionRegistrationAttempted: Boolean(result.CompanionRegistrationAttempted),
    companionRegistrationPrepared: Boolean(result.CompanionRegistrationPrepared),
    companionRegistrationEnvironmentReady: Boolean(result.CompanionRegistrationEnvironmentReady),
    companionRegistrationCompleted: Boolean(result.CompanionRegistrationCompleted),
    companionLastRegistrationAttemptUnixTimeMs: Number(result.CompanionLastRegistrationAttemptUnixTimeMs || 0),
    companionRegistrationStatus: result.CompanionRegistrationStatus || "",
    companionLastRegistrationMessage: result.CompanionLastRegistrationMessage || "",
    companionLastRegistrationHResultHex: result.CompanionLastRegistrationHResultHex || "",
    companionAuthenticatorStateCode: Number(result.CompanionAuthenticatorStateCode || 0),
    companionAuthenticatorStateLabel: result.CompanionAuthenticatorStateLabel || "unknown",
    companionHasOperationSigningPublicKey: Boolean(result.CompanionHasOperationSigningPublicKey),
    companionOperationSigningPublicKeyStoredAtUnixTimeMs: Number(result.CompanionOperationSigningPublicKeyStoredAtUnixTimeMs || 0),
    companionComSkeletonReady: Boolean(result.CompanionComSkeletonReady),
    companionComClassIdMatchesManifest: Boolean(result.CompanionComClassIdMatchesManifest),
    companionComFactoryReady: Boolean(result.CompanionComFactoryReady),
    companionComAuthenticatorReady: Boolean(result.CompanionComAuthenticatorReady),
    companionComLastProbeUnixTimeMs: Number(result.CompanionComLastProbeUnixTimeMs || 0),
    companionComLastProbeMessage: result.CompanionComLastProbeMessage || "",
    companionComAuthenticatorTypeName: result.CompanionComAuthenticatorTypeName || "",
    companionComClassFactoryRegistered: Boolean(result.CompanionComClassFactoryRegistered),
    companionComClassFactoryRegistrationCookie: Number(result.CompanionComClassFactoryRegistrationCookie || 0),
    companionComClassFactoryLastRegistrationUnixTimeMs: Number(result.CompanionComClassFactoryLastRegistrationUnixTimeMs || 0),
    companionComClassFactoryLastMessage: result.CompanionComClassFactoryLastMessage || "",
    companionComClassFactoryLastHResultHex: result.CompanionComClassFactoryLastHResultHex || "",
    companionCallbackTotalCount: Number(result.CompanionCallbackTotalCount || 0),
    companionCallbackMakeCredentialCount: Number(result.CompanionCallbackMakeCredentialCount || 0),
    companionCallbackGetAssertionCount: Number(result.CompanionCallbackGetAssertionCount || 0),
    companionCallbackCancelOperationCount: Number(result.CompanionCallbackCancelOperationCount || 0),
    companionCallbackGetLockStatusCount: Number(result.CompanionCallbackGetLockStatusCount || 0),
    companionCallbackLastUnixTimeMs: Number(result.CompanionCallbackLastUnixTimeMs || 0),
    companionCallbackLastKind: result.CompanionCallbackLastKind || "",
    companionCallbackLastMessage: result.CompanionCallbackLastMessage || "",
    companionCallbackLastHResultHex: result.CompanionCallbackLastHResultHex || "",
    companionLatestOperationId: result.CompanionLatestOperationId || "",
    companionLatestOperationKind: result.CompanionLatestOperationKind || "",
    companionLatestOperationState: result.CompanionLatestOperationState || "idle",
    companionLatestOperationSource: result.CompanionLatestOperationSource || "",
    companionLatestOperationCreatedAtUnixTimeMs: Number(result.CompanionLatestOperationCreatedAtUnixTimeMs || 0),
    companionLatestOperationUpdatedAtUnixTimeMs: Number(result.CompanionLatestOperationUpdatedAtUnixTimeMs || 0),
    companionLatestOperationRequestPointerPresent: Boolean(result.CompanionLatestOperationRequestPointerPresent),
    companionLatestOperationResponsePointerPresent: Boolean(result.CompanionLatestOperationResponsePointerPresent),
    companionLatestOperationCancelPointerPresent: Boolean(result.CompanionLatestOperationCancelPointerPresent),
    companionLatestOperationMessage: result.CompanionLatestOperationMessage || "",
    companionLatestOperationHResultHex: result.CompanionLatestOperationHResultHex || "",
    companionActivationCount: Number(result.CompanionActivationCount || 0),
    companionLastActivationUnixTimeMs: Number(result.CompanionLastActivationUnixTimeMs || 0),
    companionLastActivationSource: result.CompanionLastActivationSource || "",
    companionStartedFromPluginActivation: Boolean(result.CompanionStartedFromPluginActivation),
    companionCreateRequestCount: Number(result.CompanionCreateRequestCount || 0),
    companionLastCreateRequestUnixTimeMs: Number(result.CompanionLastCreateRequestUnixTimeMs || 0),
    companionLastCreateRequestRpId: result.CompanionLastCreateRequestRpId || "",
    companionLastCreateRequestUsername: result.CompanionLastCreateRequestUsername || "",
    companionLastCreateRequestMessage: result.CompanionLastCreateRequestMessage || "",
    recentLogs: Array.isArray(result.RecentLogs)
      ? result.RecentLogs.map(normalizePasskeyLogEntry)
      : [],
  };
}

function normalizePasskeyLogEntry(result = {}) {
  return {
    timestampUnixTimeMs: Number(result.TimestampUnixTimeMs || 0),
    level: result.Level || "info",
    source: result.Source || "",
    message: result.Message || "",
    repeatCount: Number(result.RepeatCount || 1),
  };
}

function normalizePasskeyMetadata(result = {}) {
  return {
    nativeProviderRecordId: result.NativeProviderRecordId || "",
    credentialId: result.CredentialId || "",
    rpId: result.RpId || "",
    username: result.Username || "",
    displayName: result.DisplayName || "",
    userHandle: result.UserHandle || "",
    transportHints: Array.isArray(result.TransportHints) ? result.TransportHints : [],
    authenticatorName: result.AuthenticatorName || "",
    attestationFormat: result.AttestationFormat || "",
    isRemovable: Boolean(result.IsRemovable),
    isBackedUp: Boolean(result.IsBackedUp),
    createdAt: Number(result.CreatedAt || 0),
    updatedAt: Number(result.UpdatedAt || 0),
    lastUsedAt: result.LastUsedAt == null ? null : Number(result.LastUsedAt),
  };
}

function normalizeOperationResult(result = {}) {
  return {
    success: Boolean(result.Success),
    message: result.Message || "",
    vaultKeyBase64: result.VaultKeyBase64 || "",
    isBiometricEnabled: Boolean(result.IsBiometricEnabled),
    requiresManualUnlock: Boolean(result.RequiresManualUnlock),
  };
}

function normalizeTextResult(result = {}) {
  return {
    success: Boolean(result.Success),
    message: result.Message || "",
    content: result.Content || "",
  };
}

function normalizeFileResult(result = {}) {
  return {
    success: Boolean(result.Success),
    cancelled: Boolean(result.Cancelled),
    message: result.Message || "",
    fileName: result.FileName || "",
    filePath: result.FilePath || "",
    content: result.Content || "",
    contentBase64: result.ContentBase64 || "",
  };
}

function normalizeSyncSettings(result = {}) {
  return {
    deviceId: result.DeviceId || "",
    deviceName: result.DeviceName || "",
    webDav: {
      baseUrl: result.WebDav?.BaseUrl || "",
      remotePath: result.WebDav?.RemotePath || "",
      username: result.WebDav?.Username || "",
      hasPassword: Boolean(result.WebDav?.HasPassword),
    },
  };
}

function normalizeLanDevice(result = {}) {
  return {
    deviceId: result.DeviceId || "",
    deviceName: result.DeviceName || "",
    host: result.Host || "",
    port: Number(result.Port || 0),
    tlsFingerprintSha256: result.TlsFingerprintSha256 || "",
    snapshotAvailable: Boolean(result.SnapshotAvailable),
    isCurrentDevice: Boolean(result.IsCurrentDevice),
    exportedAt: Number(result.ExportedAt || 0),
    preview: {
      totalCount: Number(result.Preview?.TotalCount || 0),
      deletedCount: Number(result.Preview?.DeletedCount || 0),
      latestItem: result.Preview?.LatestItem
        ? {
            siteName: result.Preview.LatestItem.SiteName || "",
            username: result.Preview.LatestItem.Username || "",
            createdAt: Number(result.Preview.LatestItem.CreatedAt || 0),
            updatedAt: Number(result.Preview.LatestItem.UpdatedAt || 0),
          }
        : null,
    },
  };
}

async function invokeHost(methodName, args = []) {
  const bridge = await ensureHybridBridgeLoaded();
  if (!bridge) {
    throw new Error("当前运行环境未接入原生宿主桥接。");
  }

  return bridge.InvokeDotNet(methodName, args);
}

async function invokeToggleMethod(methodName, enabled) {
  try {
    const result = await invokeHost(methodName, [{ Enabled: enabled }]);
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "调用宿主设置失败。",
      masterPassword: "",
      isBiometricEnabled: false,
    };
  }
}

async function invokeDurationMethod(methodName, minutes) {
  try {
    const result = await invokeHost(methodName, [{ Minutes: Number(minutes || 0) }]);
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "调用宿主时长设置失败。",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function getHostBridgeState() {
  const bridge = await ensureHybridBridgeLoaded();
  if (!bridge) {
    return {
      ...FALLBACK_STATE,
      message: "当前为浏览器调试模式，未接入原生宿主能力。",
    };
  }

  try {
    const result = await bridge.InvokeDotNet("GetHostBridgeState");
    return normalizeHostState(result);
  } catch (error) {
    return {
      ...FALLBACK_STATE,
      isSupported: true,
      message: error?.message || "读取原生宿主状态失败。",
    };
  }
}

export async function getSyncSettingsWithHost() {
  try {
    const result = await invokeHost("GetSyncSettings");
    return normalizeSyncSettings(result);
  } catch {
    return {
      deviceId: "",
      deviceName: "",
      webDav: {
        baseUrl: "",
        remotePath: "",
        username: "",
        hasPassword: false,
      },
    };
  }
}

export async function saveWebDavSettingsWithHost(payload) {
  try {
    const result = await invokeHost("SaveWebDavSettings", [
      {
        BaseUrl: payload.baseUrl,
        RemotePath: payload.remotePath,
        Username: payload.username,
        Password: payload.password,
        UpdatePassword: Boolean(payload.updatePassword),
      },
    ]);

    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "保存 WebDAV 配置失败。",
      masterPassword: "",
      isBiometricEnabled: false,
    };
  }
}

export async function uploadSnapshotToWebDavWithHost(content) {
  try {
    const result = await invokeHost("UploadSnapshotToWebDav", [{ Content: content }]);
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "上传 WebDAV 失败。",
      masterPassword: "",
      isBiometricEnabled: false,
    };
  }
}

export async function downloadSnapshotFromWebDavWithHost() {
  try {
    const result = await invokeHost("DownloadSnapshotFromWebDav");
    return normalizeTextResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "从 WebDAV 拉取失败。",
      content: "",
    };
  }
}

export async function publishLanSnapshotWithHost(payload) {
  try {
    const result = await invokeHost("PublishLanSnapshot", [
      {
        DeviceName: payload.deviceName,
        SnapshotContent: payload.snapshotContent,
        Preview: {
          TotalCount: payload.preview?.totalCount || 0,
          DeletedCount: payload.preview?.deletedCount || 0,
          LatestItem: payload.preview?.latestItem
            ? {
                SiteName: payload.preview.latestItem.siteName,
                Username: payload.preview.latestItem.username,
                CreatedAt: payload.preview.latestItem.createdAt,
                UpdatedAt: payload.preview.latestItem.updatedAt,
              }
            : null,
        },
      },
    ]);

    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "发布局域网同步数据失败。",
      masterPassword: "",
      isBiometricEnabled: false,
    };
  }
}

export async function setLanDeviceNameWithHost(deviceName) {
  try {
    const result = await invokeHost("SetLanDeviceName", [{ DeviceName: deviceName }]);
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "更新设备名称失败。",
      masterPassword: "",
      isBiometricEnabled: false,
    };
  }
}

export async function scanLanDevicesWithHost() {
  try {
    const result = await invokeHost("ScanLanDevices");
    return Array.isArray(result) ? result.map(normalizeLanDevice) : [];
  } catch {
    return [];
  }
}

export async function downloadLanSnapshotWithHost(device) {
  try {
    const result = await invokeHost("DownloadLanSnapshot", [
      {
        Host: device.host,
        Port: device.port,
        TlsFingerprintSha256: device.tlsFingerprintSha256 || "",
      },
    ]);
    return normalizeTextResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "从局域网设备拉取失败。",
      content: "",
    };
  }
}

export async function uploadLanMergedRecordsWithHost(device, records) {
  try {
    const payloadJson = JSON.stringify(Array.isArray(records) ? records : []);
    const result = await invokeHost("UploadLanMergedRecords", [
      {
        Host: device.host,
        Port: device.port,
        TlsFingerprintSha256: device.tlsFingerprintSha256 || "",
        RecordsBase64: bytesToBase64(bridgeEncoder.encode(payloadJson)),
      },
    ]);
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "Push LAN merged records failed.",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function clearHostStoredData() {
  const bridge = await ensureHybridBridgeLoaded();
  if (!bridge) {
    return {
      success: true,
      message: "",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }

  try {
    const result = await invokeHost("ClearHostStoredData");
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "Unable to clear host data.",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function enableBiometricUnlock(vaultKeyBase64, reauthIntervalHours) {
  try {
    const result = await invokeHost("EnableBiometricUnlock", [
      {
        VaultKeyBase64: vaultKeyBase64,
        ReauthIntervalHours: Number(reauthIntervalHours || 0),
      },
    ]);

    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "启用生物识别失败。",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function disableBiometricUnlock() {
  try {
    const result = await invokeHost("DisableBiometricUnlock");
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "关闭生物识别失败。",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function unlockWithBiometric() {
  try {
    const result = await invokeHost("UnlockWithBiometric");
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "生物识别解锁失败。",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function updateStoredMasterPassword(vaultKeyBase64, reauthIntervalHours, markManualUnlock = true) {
  try {
    const result = await invokeHost("UpdateStoredMasterPassword", [
      {
        VaultKeyBase64: vaultKeyBase64,
        ReauthIntervalHours: Number(reauthIntervalHours || 0),
        MarkManualUnlock: Boolean(markManualUnlock),
      },
    ]);

    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "同步主密码到原生宿主失败。",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function saveTextFileWithHost({ fileName, content, mimeType }) {
  try {
    const result = await invokeHost("SaveTextFile", [
      {
        FileName: fileName,
        Content: content,
        MimeType: mimeType,
      },
    ]);

    return normalizeFileResult(result);
  } catch (error) {
    return {
      success: false,
      cancelled: false,
      message: error?.message || "调用宿主保存文件失败。",
      fileName: "",
      filePath: "",
      content: "",
    };
  }
}

export async function pickImportFileWithHost() {
  try {
    const result = await invokeHost("PickImportFile");
    return normalizeFileResult(result);
  } catch (error) {
    return {
      success: false,
      cancelled: false,
      message: error?.message || "调用宿主选择文件失败。",
      fileName: "",
      filePath: "",
      content: "",
      contentBase64: "",
    };
  }
}

export function decodeHostFileBytes(fileResult) {
  return base64ToBytes(fileResult?.contentBase64);
}

export function setMinimizeToTray(enabled) {
  return invokeToggleMethod("SetMinimizeToTray", enabled);
}

export function setLaunchAtStartup(enabled) {
  return invokeToggleMethod("SetLaunchAtStartup", enabled);
}

export function setTrayAutoLockMinutes(minutes) {
  return invokeDurationMethod("SetTrayAutoLockMinutes", minutes);
}

export function setExcludeFromRecents(enabled) {
  return invokeToggleMethod("SetExcludeFromRecents", enabled);
}

export function setBackgroundAutoLockMinutes(minutes) {
  return invokeDurationMethod("SetBackgroundAutoLockMinutes", minutes);
}

export async function openAutostartSettings() {
  try {
    const result = await invokeHost("OpenAutostartSettings");
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "打开系统设置失败。",
      masterPassword: "",
      isBiometricEnabled: false,
    };
  }
}

export async function openExternalUrl(url) {
  const bridge = await ensureHybridBridgeLoaded();
  if (!bridge) {
    const popup = window.open(url, "_blank", "noopener,noreferrer");
    if (!popup) {
      window.location.href = url;
    }
    return {
      success: true,
      message: "",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }

  try {
    const result = await invokeHost("OpenExternalUrl", [{ Url: url }]);
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "Unable to open the external link.",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function getPasskeyState() {
  const bridge = await ensureHybridBridgeLoaded();
  if (!bridge) {
      return {
        isSupported: false,
        supportsMetadataManagement: false,
        supportsPluginManager: false,
        requiresCompanionApp: true,
        companionAppIntegrated: false,
        canLaunchCompanionApp: false,
        supportsCompanionAutoLaunch: false,
        companionAutoLaunchEnabled: false,
        apiVersion: 0,
        hasPlatformAuthenticator: false,
        platform: "web",
        message: "Passkey management is only planned for the Windows host.",
        pluginStatus: "Windows plugin passkey manager requires a separate packaged companion app.",
        companionCheckedAtUnixTimeMs: 0,
        companionBuildNumber: 0,
        companionUbr: 0,
        companionMeetsPluginBuildRequirement: false,
        companionWebAuthnLibraryAvailable: false,
        companionPluginExportsAvailable: false,
        companionIsPackagedProcess: false,
        companionStatusSummary: "",
        companionDetailMessage: "",
        companionWorkflowMode: "skeleton",
        companionRegistrationAttempted: false,
        companionRegistrationPrepared: false,
        companionRegistrationEnvironmentReady: false,
        companionRegistrationCompleted: false,
        companionLastRegistrationAttemptUnixTimeMs: 0,
        companionRegistrationStatus: "",
        companionLastRegistrationMessage: "",
        companionLastRegistrationHResultHex: "",
        companionAuthenticatorStateCode: 0,
        companionAuthenticatorStateLabel: "unknown",
        companionHasOperationSigningPublicKey: false,
        companionOperationSigningPublicKeyStoredAtUnixTimeMs: 0,
        companionComSkeletonReady: false,
        companionComClassIdMatchesManifest: false,
        companionComFactoryReady: false,
        companionComAuthenticatorReady: false,
        companionComLastProbeUnixTimeMs: 0,
        companionComLastProbeMessage: "",
        companionComAuthenticatorTypeName: "",
        companionComClassFactoryRegistered: false,
        companionComClassFactoryRegistrationCookie: 0,
        companionComClassFactoryLastRegistrationUnixTimeMs: 0,
        companionComClassFactoryLastMessage: "",
        companionComClassFactoryLastHResultHex: "",
        companionCallbackTotalCount: 0,
        companionCallbackMakeCredentialCount: 0,
        companionCallbackGetAssertionCount: 0,
        companionCallbackCancelOperationCount: 0,
        companionCallbackGetLockStatusCount: 0,
        companionCallbackLastUnixTimeMs: 0,
        companionCallbackLastKind: "",
        companionCallbackLastMessage: "",
        companionCallbackLastHResultHex: "",
        companionLatestOperationId: "",
        companionLatestOperationKind: "",
        companionLatestOperationState: "idle",
        companionLatestOperationSource: "",
        companionLatestOperationCreatedAtUnixTimeMs: 0,
        companionLatestOperationUpdatedAtUnixTimeMs: 0,
        companionLatestOperationRequestPointerPresent: false,
        companionLatestOperationResponsePointerPresent: false,
        companionLatestOperationCancelPointerPresent: false,
        companionLatestOperationMessage: "",
        companionLatestOperationHResultHex: "",
        companionActivationCount: 0,
        companionLastActivationUnixTimeMs: 0,
        companionLastActivationSource: "",
        companionStartedFromPluginActivation: false,
        companionCreateRequestCount: 0,
        companionLastCreateRequestUnixTimeMs: 0,
        companionLastCreateRequestRpId: "",
        companionLastCreateRequestUsername: "",
        companionLastCreateRequestMessage: "",
        recentLogs: [],
      };
  }

  try {
    const result = await invokeHost("GetPasskeyState");
    return normalizePasskeyState(result);
  } catch (error) {
        return {
          isSupported: false,
          supportsMetadataManagement: false,
          supportsPluginManager: false,
          requiresCompanionApp: true,
          companionAppIntegrated: false,
          canLaunchCompanionApp: false,
          supportsCompanionAutoLaunch: false,
          companionAutoLaunchEnabled: false,
          apiVersion: 0,
          hasPlatformAuthenticator: false,
          platform: "web",
          message: error?.message || "Unable to query passkey capability state.",
          pluginStatus: error?.message || "Unable to query Windows plugin passkey capability state.",
          companionCheckedAtUnixTimeMs: 0,
          companionBuildNumber: 0,
          companionUbr: 0,
          companionMeetsPluginBuildRequirement: false,
          companionWebAuthnLibraryAvailable: false,
          companionPluginExportsAvailable: false,
          companionIsPackagedProcess: false,
          companionStatusSummary: "",
          companionDetailMessage: "",
          companionWorkflowMode: "skeleton",
          companionRegistrationAttempted: false,
          companionRegistrationPrepared: false,
          companionRegistrationEnvironmentReady: false,
          companionRegistrationCompleted: false,
          companionLastRegistrationAttemptUnixTimeMs: 0,
          companionRegistrationStatus: "",
          companionLastRegistrationMessage: "",
          companionLastRegistrationHResultHex: "",
          companionAuthenticatorStateCode: 0,
          companionAuthenticatorStateLabel: "unknown",
          companionHasOperationSigningPublicKey: false,
          companionOperationSigningPublicKeyStoredAtUnixTimeMs: 0,
          companionComSkeletonReady: false,
          companionComClassIdMatchesManifest: false,
          companionComFactoryReady: false,
          companionComAuthenticatorReady: false,
          companionComLastProbeUnixTimeMs: 0,
          companionComLastProbeMessage: "",
          companionComAuthenticatorTypeName: "",
          companionComClassFactoryRegistered: false,
          companionComClassFactoryRegistrationCookie: 0,
          companionComClassFactoryLastRegistrationUnixTimeMs: 0,
          companionComClassFactoryLastMessage: "",
          companionComClassFactoryLastHResultHex: "",
          companionCallbackTotalCount: 0,
          companionCallbackMakeCredentialCount: 0,
          companionCallbackGetAssertionCount: 0,
          companionCallbackCancelOperationCount: 0,
          companionCallbackGetLockStatusCount: 0,
          companionCallbackLastUnixTimeMs: 0,
          companionCallbackLastKind: "",
          companionCallbackLastMessage: "",
          companionCallbackLastHResultHex: "",
          companionLatestOperationId: "",
          companionLatestOperationKind: "",
          companionLatestOperationState: "idle",
          companionLatestOperationSource: "",
          companionLatestOperationCreatedAtUnixTimeMs: 0,
          companionLatestOperationUpdatedAtUnixTimeMs: 0,
          companionLatestOperationRequestPointerPresent: false,
          companionLatestOperationResponsePointerPresent: false,
          companionLatestOperationCancelPointerPresent: false,
          companionLatestOperationMessage: "",
          companionLatestOperationHResultHex: "",
          companionActivationCount: 0,
          companionLastActivationUnixTimeMs: 0,
          companionLastActivationSource: "",
          companionStartedFromPluginActivation: false,
          companionCreateRequestCount: 0,
          companionLastCreateRequestUnixTimeMs: 0,
          companionLastCreateRequestRpId: "",
          companionLastCreateRequestUsername: "",
          companionLastCreateRequestMessage: "",
          recentLogs: [],
        };
      }
    }

export async function launchPasskeyCompanionWithHost() {
  try {
    const result = await invokeHost("LaunchPasskeyCompanion");
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "Unable to launch the Windows passkey companion.",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function restartPasskeyCompanionWithHost() {
  try {
    const result = await invokeHost("RestartPasskeyCompanion");
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "Unable to restart the Windows passkey companion.",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function setPasskeyCompanionAutoLaunchWithHost(enabled) {
  try {
    const result = await invokeHost("SetPasskeyCompanionAutoLaunch", [{ Enabled: Boolean(enabled) }]);
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "Unable to update the Windows passkey companion auto-launch setting.",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function listPasskeysWithHost() {
  try {
    const result = await invokeHost("ListPasskeys");
    return Array.isArray(result) ? result.map(normalizePasskeyMetadata) : [];
  } catch {
    return [];
  }
}

export async function refreshPasskeyMetadataWithHost() {
  try {
    const result = await invokeHost("RefreshPasskeyMetadata");
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "Unable to refresh passkey metadata.",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function createPasskeyWithHost(payload) {
  try {
    const result = await invokeHost("CreatePasskey", [
      {
        RpId: payload?.rpId || "",
        Username: payload?.username || "",
        DisplayName: payload?.displayName || "",
        UserHandle: payload?.userHandle || "",
      },
    ]);
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "Unable to create a passkey.",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function usePasskeyWithHost(payload) {
  try {
    const result = await invokeHost("UsePasskey", [
      {
        RpId: payload?.rpId || "",
        CredentialId: payload?.credentialId || "",
      },
    ]);
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "Unable to use the selected passkey.",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function deletePasskeyWithHost(payload) {
  try {
    const result = await invokeHost("DeletePasskey", [
      {
        NativeProviderRecordId: payload?.nativeProviderRecordId || "",
      },
    ]);
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "Unable to delete the selected passkey.",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}

export async function resolvePasskeyOperationWithHost(payload) {
  try {
    const result = await invokeHost("ResolvePasskeyOperation", [
      {
        Resolution: payload?.resolution || "",
        Message: payload?.message || "",
      },
    ]);
    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "Unable to update the current passkey operation.",
      vaultKeyBase64: "",
      isBiometricEnabled: false,
      requiresManualUnlock: false,
    };
  }
}
