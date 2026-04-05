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
  supportsExcludeFromRecents: false,
  excludeFromRecentsEnabled: false,
  supportsAutostartSettingsShortcut: false,
  supportsWebDavSync: false,
  supportsLanSync: false,
};

let hybridBridgeLoader = null;

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
    supportsExcludeFromRecents: Boolean(result.SupportsExcludeFromRecents),
    excludeFromRecentsEnabled: Boolean(result.ExcludeFromRecentsEnabled),
    supportsAutostartSettingsShortcut: Boolean(result.SupportsAutostartSettingsShortcut),
    supportsWebDavSync: Boolean(result.SupportsWebDavSync),
    supportsLanSync: Boolean(result.SupportsLanSync),
  };
}

function normalizeOperationResult(result = {}) {
  return {
    success: Boolean(result.Success),
    message: result.Message || "",
    masterPassword: result.MasterPassword || "",
    isBiometricEnabled: Boolean(result.IsBiometricEnabled),
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

export async function enableBiometricUnlock(masterPassword) {
  try {
    const result = await invokeHost("EnableBiometricUnlock", [
      {
        MasterPassword: masterPassword,
      },
    ]);

    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "启用生物识别失败。",
      masterPassword: "",
      isBiometricEnabled: false,
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
      masterPassword: "",
      isBiometricEnabled: false,
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
      masterPassword: "",
      isBiometricEnabled: false,
    };
  }
}

export async function updateStoredMasterPassword(masterPassword) {
  try {
    const result = await invokeHost("UpdateStoredMasterPassword", [
      {
        MasterPassword: masterPassword,
      },
    ]);

    return normalizeOperationResult(result);
  } catch (error) {
    return {
      success: false,
      message: error?.message || "同步主密码到原生宿主失败。",
      masterPassword: "",
      isBiometricEnabled: false,
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

export async function pickCsvFileWithHost() {
  try {
    const result = await invokeHost("PickCsvFile");
    return normalizeFileResult(result);
  } catch (error) {
    return {
      success: false,
      cancelled: false,
      message: error?.message || "调用宿主选择文件失败。",
      fileName: "",
      filePath: "",
      content: "",
    };
  }
}

export function setMinimizeToTray(enabled) {
  return invokeToggleMethod("SetMinimizeToTray", enabled);
}

export function setLaunchAtStartup(enabled) {
  return invokeToggleMethod("SetLaunchAtStartup", enabled);
}

export function setExcludeFromRecents(enabled) {
  return invokeToggleMethod("SetExcludeFromRecents", enabled);
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
