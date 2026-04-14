namespace blazorApp.Services;

public interface IHostSyncService
{
    Task<HostOperationResult> ResetSyncStateAsync();

    Task<SyncSettingsState> GetSyncSettingsAsync();

    Task<HostOperationResult> SaveWebDavSettingsAsync(SaveWebDavSettingsRequest request);

    Task<HostOperationResult> UploadSnapshotToWebDavAsync(SnapshotTransferRequest request);

    Task<HostTextOperationResult> DownloadSnapshotFromWebDavAsync();

    Task<HostOperationResult> PublishLanSnapshotAsync(PublishLanSnapshotRequest request);

    Task<HostOperationResult> SetLanDeviceNameAsync(UpdateDeviceNameRequest request);

    Task<IReadOnlyList<LanDeviceSummary>> ScanLanDevicesAsync();

    Task<HostTextOperationResult> DownloadLanSnapshotAsync(DownloadLanSnapshotRequest request);

    Task<HostOperationResult> UploadLanMergedRecordsAsync(UploadLanMergedRecordsRequest request);
}
