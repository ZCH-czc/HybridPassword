namespace blazorApp.Services;

public interface IHostPlatformService
{
    void AttachWindow(Microsoft.Maui.Controls.Window window);

    Task EnrichHostBridgeStateAsync(HostBridgeState state);

    Task<HostOperationResult> SetMinimizeToTrayAsync(bool enabled);

    Task<HostOperationResult> SetLaunchAtStartupAsync(bool enabled);

    Task<HostOperationResult> SetTrayAutoLockMinutesAsync(int minutes);

    Task<HostOperationResult> SetExcludeFromRecentsAsync(bool enabled);

    Task<HostOperationResult> SetBackgroundAutoLockMinutesAsync(int minutes);

    Task<HostOperationResult> OpenAutostartSettingsAsync();
}
