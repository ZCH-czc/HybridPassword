namespace PasswordVault.PasskeyCompanion.Models;

public sealed class CompanionRuntimeState
{
    public bool HasHostSessionLink { get; init; }

    public int HostProcessId { get; init; }

    public long HostStartTimeUtcTicks { get; init; }

    public bool HostProcessAlive { get; init; }
}
