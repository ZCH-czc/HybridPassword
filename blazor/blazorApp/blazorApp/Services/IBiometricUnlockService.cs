namespace blazorApp.Services;

public interface IBiometricUnlockService
{
    Task<HostBridgeState> GetBridgeStateAsync();

    Task<HostOperationResult> EnableAsync(string vaultKeyBase64, int reauthIntervalHours);

    Task<HostOperationResult> DisableAsync();

    Task<HostOperationResult> UnlockAsync();

    Task<HostOperationResult> UpdateStoredVaultKeyAsync(
        string vaultKeyBase64,
        int reauthIntervalHours,
        bool markManualUnlock);
}
