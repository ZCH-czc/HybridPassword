namespace blazorApp.Services;

public interface IPasskeyCompanionClientService
{
    Task<PasskeyCompanionProbeResult> TryGetStatusAsync(CancellationToken cancellationToken = default);

    Task<PasskeyCompanionWorkflowSnapshot?> TryGetWorkflowAsync(CancellationToken cancellationToken = default);

    Task<HostOperationResult> TryPreparePluginRegistrationAsync(CancellationToken cancellationToken = default);

    Task<HostOperationResult> TryCreatePasskeySkeletonAsync(
        PasskeyCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<HostOperationResult> TryResolveLatestOperationAsync(
        string resolution,
        string message = "",
        CancellationToken cancellationToken = default);

    Task<HostOperationResult> TryActivateAsync(CancellationToken cancellationToken = default);

    Task<HostOperationResult> TryShutdownAsync(CancellationToken cancellationToken = default);
}
