namespace blazorApp.Services;

public interface IBiometricUnlockService
{
    Task<HostBridgeState> GetBridgeStateAsync();

    Task<HostOperationResult> EnableAsync(string masterPassword);

    Task<HostOperationResult> DisableAsync();

    Task<HostOperationResult> UnlockAsync();

    Task<HostOperationResult> UpdateStoredMasterPasswordAsync(string masterPassword);
}
