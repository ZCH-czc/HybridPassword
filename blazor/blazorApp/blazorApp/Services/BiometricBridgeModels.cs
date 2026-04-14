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

    public int TrayAutoLockMinutes { get; set; }

    public bool SupportsExcludeFromRecents { get; set; }

    public bool ExcludeFromRecentsEnabled { get; set; }

    public int BackgroundAutoLockMinutes { get; set; }

    public bool SupportsAutostartSettingsShortcut { get; set; }

    public bool SupportsWebDavSync { get; set; }

    public bool SupportsLanSync { get; set; }

    public bool SupportsPasskeys { get; set; }
}

public sealed class HostOperationResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string VaultKeyBase64 { get; set; } = string.Empty;

    public bool IsBiometricEnabled { get; set; }

    public bool RequiresManualUnlock { get; set; }
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

    public string ContentBase64 { get; set; } = string.Empty;
}

public sealed class StoreVaultKeyRequest
{
    public string VaultKeyBase64 { get; set; } = string.Empty;

    public int ReauthIntervalHours { get; set; }

    public bool MarkManualUnlock { get; set; } = true;
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

public sealed class DurationSettingRequest
{
    public int Minutes { get; set; }
}

public sealed class OpenUrlRequest
{
    public string Url { get; set; } = string.Empty;
}

public sealed class PasskeyBridgeState
{
    public bool IsSupported { get; set; }

    public bool SupportsMetadataManagement { get; set; }

    public bool SupportsPluginManager { get; set; }

    public bool RequiresCompanionApp { get; set; }

    public bool CompanionAppIntegrated { get; set; }

    public uint ApiVersion { get; set; }

    public bool HasPlatformAuthenticator { get; set; }

    public string Platform { get; set; } = "web";

    public string Message { get; set; } = string.Empty;

    public string PluginStatus { get; set; } = string.Empty;

    public bool CanLaunchCompanionApp { get; set; }

    public bool SupportsCompanionAutoLaunch { get; set; }

    public bool CompanionAutoLaunchEnabled { get; set; }

    public long CompanionCheckedAtUnixTimeMs { get; set; }

    public int CompanionBuildNumber { get; set; }

    public int CompanionUbr { get; set; }

    public bool CompanionMeetsPluginBuildRequirement { get; set; }

    public bool CompanionWebAuthnLibraryAvailable { get; set; }

    public bool CompanionPluginExportsAvailable { get; set; }

    public bool CompanionIsPackagedProcess { get; set; }

    public string CompanionStatusSummary { get; set; } = string.Empty;

    public string CompanionDetailMessage { get; set; } = string.Empty;

    public string CompanionWorkflowMode { get; set; } = "skeleton";

    public bool CompanionRegistrationAttempted { get; set; }

    public bool CompanionRegistrationPrepared { get; set; }

    public bool CompanionRegistrationEnvironmentReady { get; set; }

    public bool CompanionRegistrationCompleted { get; set; }

    public long CompanionLastRegistrationAttemptUnixTimeMs { get; set; }

    public string CompanionRegistrationStatus { get; set; } = string.Empty;

    public string CompanionLastRegistrationMessage { get; set; } = string.Empty;

    public string CompanionLastRegistrationHResultHex { get; set; } = string.Empty;

    public int CompanionAuthenticatorStateCode { get; set; }

    public string CompanionAuthenticatorStateLabel { get; set; } = "unknown";

    public bool CompanionHasOperationSigningPublicKey { get; set; }

    public long CompanionOperationSigningPublicKeyStoredAtUnixTimeMs { get; set; }

    public bool CompanionComSkeletonReady { get; set; }

    public bool CompanionComClassIdMatchesManifest { get; set; }

    public bool CompanionComFactoryReady { get; set; }

    public bool CompanionComAuthenticatorReady { get; set; }

    public long CompanionComLastProbeUnixTimeMs { get; set; }

    public string CompanionComLastProbeMessage { get; set; } = string.Empty;

    public string CompanionComAuthenticatorTypeName { get; set; } = string.Empty;

    public bool CompanionComClassFactoryRegistered { get; set; }

    public uint CompanionComClassFactoryRegistrationCookie { get; set; }

    public long CompanionComClassFactoryLastRegistrationUnixTimeMs { get; set; }

    public string CompanionComClassFactoryLastMessage { get; set; } = string.Empty;

    public string CompanionComClassFactoryLastHResultHex { get; set; } = string.Empty;

    public int CompanionCallbackTotalCount { get; set; }

    public int CompanionCallbackMakeCredentialCount { get; set; }

    public int CompanionCallbackGetAssertionCount { get; set; }

    public int CompanionCallbackCancelOperationCount { get; set; }

    public int CompanionCallbackGetLockStatusCount { get; set; }

    public long CompanionCallbackLastUnixTimeMs { get; set; }

    public string CompanionCallbackLastKind { get; set; } = string.Empty;

    public string CompanionCallbackLastMessage { get; set; } = string.Empty;

    public string CompanionCallbackLastHResultHex { get; set; } = string.Empty;

    public string CompanionLatestOperationId { get; set; } = string.Empty;

    public string CompanionLatestOperationKind { get; set; } = string.Empty;

    public string CompanionLatestOperationState { get; set; } = "idle";

    public string CompanionLatestOperationSource { get; set; } = string.Empty;

    public long CompanionLatestOperationCreatedAtUnixTimeMs { get; set; }

    public long CompanionLatestOperationUpdatedAtUnixTimeMs { get; set; }

    public bool CompanionLatestOperationRequestPointerPresent { get; set; }

    public bool CompanionLatestOperationResponsePointerPresent { get; set; }

    public bool CompanionLatestOperationCancelPointerPresent { get; set; }

    public string CompanionLatestOperationMessage { get; set; } = string.Empty;

    public string CompanionLatestOperationHResultHex { get; set; } = string.Empty;

    public int CompanionActivationCount { get; set; }

    public long CompanionLastActivationUnixTimeMs { get; set; }

    public string CompanionLastActivationSource { get; set; } = string.Empty;

    public bool CompanionStartedFromPluginActivation { get; set; }

    public int CompanionCreateRequestCount { get; set; }

    public long CompanionLastCreateRequestUnixTimeMs { get; set; }

    public string CompanionLastCreateRequestRpId { get; set; } = string.Empty;

    public string CompanionLastCreateRequestUsername { get; set; } = string.Empty;

    public string CompanionLastCreateRequestMessage { get; set; } = string.Empty;

    public PasskeyLogEntryState[] RecentLogs { get; set; } = [];
}

public sealed class PasskeyLogEntryState
{
    public long TimestampUnixTimeMs { get; set; }

    public string Level { get; set; } = "info";

    public string Source { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public int RepeatCount { get; set; } = 1;
}

public sealed class PasskeyMetadataState
{
    public string NativeProviderRecordId { get; set; } = string.Empty;

    public string CredentialId { get; set; } = string.Empty;

    public string RpId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string UserHandle { get; set; } = string.Empty;

    public string[] TransportHints { get; set; } = [];

    public string AuthenticatorName { get; set; } = string.Empty;

    public string AttestationFormat { get; set; } = string.Empty;

    public bool IsRemovable { get; set; }

    public bool IsBackedUp { get; set; }

    public long CreatedAt { get; set; }

    public long UpdatedAt { get; set; }

    public long? LastUsedAt { get; set; }
}

public sealed class PasskeyCreateRequest
{
    public string RpId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string UserHandle { get; set; } = string.Empty;
}

public sealed class PasskeyUseRequest
{
    public string RpId { get; set; } = string.Empty;

    public string CredentialId { get; set; } = string.Empty;
}

public sealed class PasskeyDeleteRequest
{
    public string NativeProviderRecordId { get; set; } = string.Empty;
}

public sealed class PasskeyOperationResolutionRequest
{
    public string Resolution { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public sealed class PasskeyCompanionStatusSnapshot
{
    public long CheckedAtUnixTimeMs { get; set; }

    public int BuildNumber { get; set; }

    public int Ubr { get; set; }

    public bool MeetsPluginBuildRequirement { get; set; }

    public bool WebAuthnLibraryAvailable { get; set; }

    public bool PluginExportsAvailable { get; set; }

    public bool IsPackagedProcess { get; set; }

    public string StatusSummary { get; set; } = string.Empty;

    public string DetailMessage { get; set; } = string.Empty;
}

public sealed class PasskeyCompanionWorkflowSnapshot
{
    public string WorkflowMode { get; set; } = "skeleton";

    public bool RegistrationAttempted { get; set; }

    public bool RegistrationPrepared { get; set; }

    public bool RegistrationEnvironmentReady { get; set; }

    public bool RegistrationCompleted { get; set; }

    public long LastRegistrationAttemptUnixTimeMs { get; set; }

    public string RegistrationStatus { get; set; } = string.Empty;

    public string LastRegistrationMessage { get; set; } = string.Empty;

    public string LastRegistrationHResultHex { get; set; } = string.Empty;

    public int AuthenticatorStateCode { get; set; }

    public string AuthenticatorStateLabel { get; set; } = "unknown";

    public bool HasOperationSigningPublicKey { get; set; }

    public long OperationSigningPublicKeyStoredAtUnixTimeMs { get; set; }

    public bool ComSkeletonReady { get; set; }

    public bool ComClassIdMatchesManifest { get; set; }

    public bool ComFactoryReady { get; set; }

    public bool ComAuthenticatorReady { get; set; }

    public long ComLastProbeUnixTimeMs { get; set; }

    public string ComLastProbeMessage { get; set; } = string.Empty;

    public string ComAuthenticatorTypeName { get; set; } = string.Empty;

    public bool ComClassFactoryRegistered { get; set; }

    public uint ComClassFactoryRegistrationCookie { get; set; }

    public long ComClassFactoryLastRegistrationUnixTimeMs { get; set; }

    public string ComClassFactoryLastMessage { get; set; } = string.Empty;

    public string ComClassFactoryLastHResultHex { get; set; } = string.Empty;

    public int CallbackTotalCount { get; set; }

    public int CallbackMakeCredentialCount { get; set; }

    public int CallbackGetAssertionCount { get; set; }

    public int CallbackCancelOperationCount { get; set; }

    public int CallbackGetLockStatusCount { get; set; }

    public long CallbackLastUnixTimeMs { get; set; }

    public string CallbackLastKind { get; set; } = string.Empty;

    public string CallbackLastMessage { get; set; } = string.Empty;

    public string CallbackLastHResultHex { get; set; } = string.Empty;

    public string LatestOperationId { get; set; } = string.Empty;

    public string LatestOperationKind { get; set; } = string.Empty;

    public string LatestOperationState { get; set; } = "idle";

    public string LatestOperationSource { get; set; } = string.Empty;

    public long LatestOperationCreatedAtUnixTimeMs { get; set; }

    public long LatestOperationUpdatedAtUnixTimeMs { get; set; }

    public bool LatestOperationRequestPointerPresent { get; set; }

    public bool LatestOperationResponsePointerPresent { get; set; }

    public bool LatestOperationCancelPointerPresent { get; set; }

    public string LatestOperationMessage { get; set; } = string.Empty;

    public string LatestOperationHResultHex { get; set; } = string.Empty;

    public int ActivationCount { get; set; }

    public long LastActivationUnixTimeMs { get; set; }

    public string LastActivationSource { get; set; } = string.Empty;

    public bool StartedFromPluginActivation { get; set; }

    public int CreateRequestCount { get; set; }

    public long LastCreateRequestUnixTimeMs { get; set; }

    public string LastCreateRequestRpId { get; set; } = string.Empty;

    public string LastCreateRequestUsername { get; set; } = string.Empty;

    public string LastCreateRequestMessage { get; set; } = string.Empty;
}

public sealed class PasskeyCompanionProbeResult
{
    public bool IsReachable { get; set; }

    public string Message { get; set; } = string.Empty;

    public PasskeyCompanionStatusSnapshot? Snapshot { get; set; }

    public PasskeyCompanionWorkflowSnapshot? Workflow { get; set; }
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

    public string TlsFingerprintSha256 { get; set; } = string.Empty;

    public SyncPreview Preview { get; set; } = new();
}

public sealed class DownloadLanSnapshotRequest
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public string TlsFingerprintSha256 { get; set; } = string.Empty;
}

public sealed class UploadLanMergedRecordsRequest
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public string TlsFingerprintSha256 { get; set; } = string.Empty;

    public string RecordsBase64 { get; set; } = string.Empty;
}
