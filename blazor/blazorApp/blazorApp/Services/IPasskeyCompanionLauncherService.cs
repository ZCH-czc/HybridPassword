namespace blazorApp.Services;

public interface IPasskeyCompanionLauncherService
{
    bool CanLaunchCompanionApp();

    Task<HostOperationResult> LaunchAsync(CancellationToken cancellationToken = default);

    Task<HostOperationResult> RestartAsync(CancellationToken cancellationToken = default);

    Task<HostOperationResult> StopAsync(CancellationToken cancellationToken = default);
}
