namespace blazorApp.Services;

public sealed class PasswordVaultHostBridge
{
    private readonly IBiometricUnlockService _biometricUnlockService;
    private readonly IHostFileDialogService _hostFileDialogService;
    private readonly IPasskeyHostService _passkeyHostService;
    private readonly IHostPlatformService _hostPlatformService;
    private readonly IHostSyncService _hostSyncService;

    public PasswordVaultHostBridge(
        IBiometricUnlockService biometricUnlockService,
        IHostFileDialogService hostFileDialogService,
        IPasskeyHostService passkeyHostService,
        IHostPlatformService hostPlatformService,
        IHostSyncService hostSyncService)
    {
        _biometricUnlockService = biometricUnlockService;
        _hostFileDialogService = hostFileDialogService;
        _passkeyHostService = passkeyHostService;
        _hostPlatformService = hostPlatformService;
        _hostSyncService = hostSyncService;
    }

    public async Task<HostBridgeState> GetHostBridgeState()
    {
        var state = await _biometricUnlockService.GetBridgeStateAsync();
        state.SupportsNativeFileDialogs = true;
        state.SupportsWebDavSync = true;
        state.SupportsLanSync = true;
        state.SupportsPasskeys = (await _passkeyHostService.GetStateAsync()).IsSupported;
        await _hostPlatformService.EnrichHostBridgeStateAsync(state);
        return state;
    }

    public async Task<HostOperationResult> ClearHostStoredData()
    {
        var messages = new List<string>();

        var biometricResult = await _biometricUnlockService.DisableAsync();
        if (!biometricResult.Success)
        {
            return biometricResult;
        }

        if (!string.IsNullOrWhiteSpace(biometricResult.Message))
        {
            messages.Add(biometricResult.Message);
        }

        var syncResult = await _hostSyncService.ResetSyncStateAsync();
        if (!syncResult.Success)
        {
            return syncResult;
        }

        if (!string.IsNullOrWhiteSpace(syncResult.Message))
        {
            messages.Add(syncResult.Message);
        }

        var platformResult = await _hostPlatformService.ResetPlatformStateAsync();
        if (!platformResult.Success)
        {
            return platformResult;
        }

        if (!string.IsNullOrWhiteSpace(platformResult.Message))
        {
            messages.Add(platformResult.Message);
        }

        return new HostOperationResult
        {
            Success = true,
            Message = messages.Count > 0
                ? string.Join(" ", messages)
                : "Host data cleared.",
            IsBiometricEnabled = false,
        };
    }

    public Task<HostOperationResult> EnableBiometricUnlock(StoreVaultKeyRequest request)
    {
        return _biometricUnlockService.EnableAsync(
            request?.VaultKeyBase64 ?? string.Empty,
            request?.ReauthIntervalHours ?? 0);
    }

    public Task<HostOperationResult> DisableBiometricUnlock()
    {
        return _biometricUnlockService.DisableAsync();
    }

    public Task<HostOperationResult> UnlockWithBiometric()
    {
        return _biometricUnlockService.UnlockAsync();
    }

    public Task<HostOperationResult> UpdateStoredMasterPassword(StoreVaultKeyRequest request)
    {
        return _biometricUnlockService.UpdateStoredVaultKeyAsync(
            request?.VaultKeyBase64 ?? string.Empty,
            request?.ReauthIntervalHours ?? 0,
            request?.MarkManualUnlock ?? true);
    }

    public Task<HostFileOperationResult> SaveTextFile(SaveTextFileRequest request)
    {
        return _hostFileDialogService.SaveTextFileAsync(
            request?.FileName ?? "passwords.txt",
            request?.Content ?? string.Empty,
            request?.MimeType ?? "text/plain;charset=utf-8");
    }

    public Task<HostFileOperationResult> PickImportFile()
    {
        return _hostFileDialogService.PickImportFileAsync();
    }

    public Task<HostOperationResult> SetMinimizeToTray(ToggleSettingRequest request)
    {
        return _hostPlatformService.SetMinimizeToTrayAsync(request?.Enabled ?? false);
    }

    public Task<HostOperationResult> SetLaunchAtStartup(ToggleSettingRequest request)
    {
        return _hostPlatformService.SetLaunchAtStartupAsync(request?.Enabled ?? false);
    }

    public Task<HostOperationResult> SetTrayAutoLockMinutes(DurationSettingRequest request)
    {
        return _hostPlatformService.SetTrayAutoLockMinutesAsync(request?.Minutes ?? 0);
    }

    public Task<HostOperationResult> SetExcludeFromRecents(ToggleSettingRequest request)
    {
        return _hostPlatformService.SetExcludeFromRecentsAsync(request?.Enabled ?? false);
    }

    public Task<HostOperationResult> SetBackgroundAutoLockMinutes(DurationSettingRequest request)
    {
        return _hostPlatformService.SetBackgroundAutoLockMinutesAsync(request?.Minutes ?? 0);
    }

    public Task<HostOperationResult> OpenAutostartSettings()
    {
        return _hostPlatformService.OpenAutostartSettingsAsync();
    }

    public Task<HostOperationResult> OpenExternalUrl(OpenUrlRequest request)
    {
        return _hostPlatformService.OpenExternalUrlAsync(request?.Url ?? string.Empty);
    }

    public Task<PasskeyBridgeState> GetPasskeyState()
    {
        return _passkeyHostService.GetStateAsync();
    }

    public Task<IReadOnlyList<PasskeyMetadataState>> ListPasskeys()
    {
        return _passkeyHostService.ListPasskeysAsync();
    }

    public Task<HostOperationResult> RefreshPasskeyMetadata()
    {
        return _passkeyHostService.RefreshMetadataAsync();
    }

    public Task<HostOperationResult> CreatePasskey(PasskeyCreateRequest request)
    {
        return _passkeyHostService.CreatePasskeyAsync(request ?? new PasskeyCreateRequest());
    }

    public Task<HostOperationResult> UsePasskey(PasskeyUseRequest request)
    {
        return _passkeyHostService.UsePasskeyAsync(request ?? new PasskeyUseRequest());
    }

    public Task<HostOperationResult> DeletePasskey(PasskeyDeleteRequest request)
    {
        return _passkeyHostService.DeletePasskeyAsync(request ?? new PasskeyDeleteRequest());
    }

    public Task<HostOperationResult> ResolvePasskeyOperation(PasskeyOperationResolutionRequest request)
    {
        return _passkeyHostService.ResolveLatestOperationAsync(
            request?.Resolution ?? string.Empty,
            request?.Message ?? string.Empty);
    }

    public Task<HostOperationResult> LaunchPasskeyCompanion()
    {
        return _passkeyHostService.LaunchCompanionAsync();
    }

    public Task<HostOperationResult> RestartPasskeyCompanion()
    {
        return _passkeyHostService.RestartCompanionAsync();
    }

    public Task<HostOperationResult> SetPasskeyCompanionAutoLaunch(ToggleSettingRequest request)
    {
        return _passkeyHostService.SetCompanionAutoLaunchAsync(request?.Enabled ?? false);
    }

    public Task<SyncSettingsState> GetSyncSettings()
    {
        return _hostSyncService.GetSyncSettingsAsync();
    }

    public Task<HostOperationResult> SaveWebDavSettings(SaveWebDavSettingsRequest request)
    {
        return _hostSyncService.SaveWebDavSettingsAsync(request);
    }

    public Task<HostOperationResult> UploadSnapshotToWebDav(SnapshotTransferRequest request)
    {
        return _hostSyncService.UploadSnapshotToWebDavAsync(request);
    }

    public Task<HostTextOperationResult> DownloadSnapshotFromWebDav()
    {
        return _hostSyncService.DownloadSnapshotFromWebDavAsync();
    }

    public Task<HostOperationResult> PublishLanSnapshot(PublishLanSnapshotRequest request)
    {
        return _hostSyncService.PublishLanSnapshotAsync(request);
    }

    public Task<HostOperationResult> SetLanDeviceName(UpdateDeviceNameRequest request)
    {
        return _hostSyncService.SetLanDeviceNameAsync(request);
    }

    public Task<IReadOnlyList<LanDeviceSummary>> ScanLanDevices()
    {
        return _hostSyncService.ScanLanDevicesAsync();
    }

    public Task<HostTextOperationResult> DownloadLanSnapshot(DownloadLanSnapshotRequest request)
    {
        return _hostSyncService.DownloadLanSnapshotAsync(request);
    }

    public Task<HostOperationResult> UploadLanMergedRecords(UploadLanMergedRecordsRequest request)
    {
        return _hostSyncService.UploadLanMergedRecordsAsync(request);
    }
}
