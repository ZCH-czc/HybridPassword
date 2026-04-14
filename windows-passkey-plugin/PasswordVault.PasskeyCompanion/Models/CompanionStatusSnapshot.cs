namespace PasswordVault.PasskeyCompanion.Models;

public sealed class CompanionStatusSnapshot
{
    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.UtcNow;

    public long CheckedAtUnixTimeMs => CheckedAt.ToUnixTimeMilliseconds();

    public int BuildNumber { get; init; }

    public int Ubr { get; init; }

    public bool MeetsPluginBuildRequirement { get; init; }

    public bool WebAuthnLibraryAvailable { get; init; }

    public bool PluginExportsAvailable { get; init; }

    public bool IsPackagedProcess { get; init; }

    public bool HasHostSessionLink { get; init; }

    public int HostProcessId { get; init; }

    public long HostStartTimeUtcTicks { get; init; }

    public bool HostProcessAlive { get; init; }

    public string PackageFamilyName { get; init; } = string.Empty;

    public string PackageFullName { get; init; } = string.Empty;

    public IReadOnlyList<string> MissingExports { get; init; } = [];

    public IReadOnlyList<string> NextSteps { get; init; } = [];

    public string StatusSummary { get; init; } = string.Empty;

    public string DetailMessage { get; init; } = string.Empty;

    public bool IsCompanionReady =>
        MeetsPluginBuildRequirement &&
        WebAuthnLibraryAvailable &&
        PluginExportsAvailable &&
        IsPackagedProcess;
}
