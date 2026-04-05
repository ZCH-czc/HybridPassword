namespace blazorApp.Services;

public sealed class HostBridgeState
{
    public bool IsSupported { get; set; }

    public bool IsBiometricAvailable { get; set; }

    public bool IsBiometricEnabled { get; set; }

    public bool SupportsNativeFileDialogs { get; set; }

    public string BiometricLabel { get; set; } = "生物识别";

    public string Platform { get; set; } = "web";

    public string Message { get; set; } = string.Empty;

    public double SafeAreaTop { get; set; }

    public double SafeAreaBottom { get; set; }

    public bool SupportsMinimizeToTray { get; set; }

    public bool MinimizeToTrayEnabled { get; set; }

    public bool SupportsLaunchAtStartup { get; set; }

    public bool LaunchAtStartupEnabled { get; set; }

    public bool SupportsExcludeFromRecents { get; set; }

    public bool ExcludeFromRecentsEnabled { get; set; }

    public bool SupportsAutostartSettingsShortcut { get; set; }

    public bool SupportsWebDavSync { get; set; }

    public bool SupportsLanSync { get; set; }
}

public sealed class HostOperationResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string MasterPassword { get; set; } = string.Empty;

    public bool IsBiometricEnabled { get; set; }
}

public sealed class HostTextOperationResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}

public sealed class HostFileOperationResult
{
    public bool Success { get; set; }

    public bool Cancelled { get; set; }

    public string Message { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}

public sealed class StoreMasterPasswordRequest
{
    public string MasterPassword { get; set; } = string.Empty;
}

public sealed class SaveTextFileRequest
{
    public string FileName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string MimeType { get; set; } = "text/plain;charset=utf-8";
}

public sealed class ToggleSettingRequest
{
    public bool Enabled { get; set; }
}

public sealed class SyncPreviewItem
{
    public string SiteName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public long CreatedAt { get; set; }

    public long UpdatedAt { get; set; }
}

public sealed class SyncPreview
{
    public int TotalCount { get; set; }

    public int DeletedCount { get; set; }

    public SyncPreviewItem? LatestItem { get; set; }
}

public sealed class SaveWebDavSettingsRequest
{
    public string BaseUrl { get; set; } = string.Empty;

    public string RemotePath { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool UpdatePassword { get; set; }
}

public sealed class WebDavSettingsState
{
    public string BaseUrl { get; set; } = string.Empty;

    public string RemotePath { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public bool HasPassword { get; set; }
}

public sealed class SyncSettingsState
{
    public string DeviceId { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;

    public WebDavSettingsState WebDav { get; set; } = new();
}

public sealed class SnapshotTransferRequest
{
    public string Content { get; set; } = string.Empty;
}

public sealed class PublishLanSnapshotRequest
{
    public string DeviceName { get; set; } = string.Empty;

    public string SnapshotContent { get; set; } = string.Empty;

    public SyncPreview Preview { get; set; } = new();
}

public sealed class UpdateDeviceNameRequest
{
    public string DeviceName { get; set; } = string.Empty;
}

public sealed class LanDeviceSummary
{
    public string DeviceId { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public bool SnapshotAvailable { get; set; }

    public bool IsCurrentDevice { get; set; }

    public long ExportedAt { get; set; }

    public SyncPreview Preview { get; set; } = new();
}

public sealed class DownloadLanSnapshotRequest
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }
}
