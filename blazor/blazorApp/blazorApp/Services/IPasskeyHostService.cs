namespace blazorApp.Services;

public interface IPasskeyHostService
{
    Task<PasskeyBridgeState> GetStateAsync();

    Task<IReadOnlyList<PasskeyMetadataState>> ListPasskeysAsync();

    Task<HostOperationResult> RefreshMetadataAsync();

    Task<HostOperationResult> CreatePasskeyAsync(PasskeyCreateRequest request);

    Task<HostOperationResult> UsePasskeyAsync(PasskeyUseRequest request);

    Task<HostOperationResult> DeletePasskeyAsync(PasskeyDeleteRequest request);

    Task<HostOperationResult> ResolveLatestOperationAsync(string resolution, string message = "");

    Task<HostOperationResult> LaunchCompanionAsync();

    Task<HostOperationResult> RestartCompanionAsync();

    Task<HostOperationResult> SetCompanionAutoLaunchAsync(bool enabled);

    Task EnsureCompanionOnHostStartAsync();

    Task CleanupCompanionOnHostExitAsync();
}
