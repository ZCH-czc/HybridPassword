namespace blazorApp.Services;

public sealed class PasswordVaultHostBridge
{
    private readonly IBiometricUnlockService _biometricUnlockService;
    private readonly IHostFileDialogService _hostFileDialogService;
    private readonly IHostPlatformService _hostPlatformService;
    private readonly IHostSyncService _hostSyncService;

    public PasswordVaultHostBridge(
        IBiometricUnlockService biometricUnlockService,
        IHostFileDialogService hostFileDialogService,
        IHostPlatformService hostPlatformService,
        IHostSyncService hostSyncService)
    {
        _biometricUnlockService = biometricUnlockService;
        _hostFileDialogService = hostFileDialogService;
        _hostPlatformService = hostPlatformService;
        _hostSyncService = hostSyncService;
    }

    public async Task<HostBridgeState> GetHostBridgeState()
    {
        var state = await _biometricUnlockService.GetBridgeStateAsync();
        state.SupportsNativeFileDialogs = true;
        state.SupportsWebDavSync = true;
        state.SupportsLanSync = true;
        await _hostPlatformService.EnrichHostBridgeStateAsync(state);
        return state;
    }

    public Task<HostOperationResult> EnableBiometricUnlock(StoreMasterPasswordRequest request)
    {
        return _biometricUnlockService.EnableAsync(request?.MasterPassword ?? string.Empty);
    }

    public Task<HostOperationResult> DisableBiometricUnlock()
    {
        return _biometricUnlockService.DisableAsync();
    }

    public Task<HostOperationResult> UnlockWithBiometric()
    {
        return _biometricUnlockService.UnlockAsync();
    }

    public Task<HostOperationResult> UpdateStoredMasterPassword(StoreMasterPasswordRequest request)
    {
        return _biometricUnlockService.UpdateStoredMasterPasswordAsync(request?.MasterPassword ?? string.Empty);
    }

    public Task<HostFileOperationResult> SaveTextFile(SaveTextFileRequest request)
    {
        return _hostFileDialogService.SaveTextFileAsync(
            request?.FileName ?? "passwords.txt",
            request?.Content ?? string.Empty,
            request?.MimeType ?? "text/plain;charset=utf-8");
    }

    public Task<HostFileOperationResult> PickCsvFile()
    {
        return _hostFileDialogService.PickCsvFileAsync();
    }

    public Task<HostOperationResult> SetMinimizeToTray(ToggleSettingRequest request)
    {
        return _hostPlatformService.SetMinimizeToTrayAsync(request?.Enabled ?? false);
    }

    public Task<HostOperationResult> SetLaunchAtStartup(ToggleSettingRequest request)
    {
        return _hostPlatformService.SetLaunchAtStartupAsync(request?.Enabled ?? false);
    }

    public Task<HostOperationResult> SetExcludeFromRecents(ToggleSettingRequest request)
    {
        return _hostPlatformService.SetExcludeFromRecentsAsync(request?.Enabled ?? false);
    }

    public Task<HostOperationResult> OpenAutostartSettings()
    {
        return _hostPlatformService.OpenAutostartSettingsAsync();
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
}
