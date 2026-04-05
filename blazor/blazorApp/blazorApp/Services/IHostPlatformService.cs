namespace blazorApp.Services;

public interface IHostPlatformService
{
    void AttachWindow(Microsoft.Maui.Controls.Window window);

    Task EnrichHostBridgeStateAsync(HostBridgeState state);

    Task<HostOperationResult> SetMinimizeToTrayAsync(bool enabled);

    Task<HostOperationResult> SetLaunchAtStartupAsync(bool enabled);

    Task<HostOperationResult> SetExcludeFromRecentsAsync(bool enabled);

    Task<HostOperationResult> OpenAutostartSettingsAsync();
}
