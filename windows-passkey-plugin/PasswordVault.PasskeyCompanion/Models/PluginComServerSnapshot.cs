namespace PasswordVault.PasskeyCompanion.Models;

public sealed class PluginComServerSnapshot
{
    public bool ComSkeletonReady { get; init; }

    public bool ClassIdMatchesManifest { get; init; }

    public bool FactoryReady { get; init; }

    public bool AuthenticatorReady { get; init; }

    public long LastProbeUnixTimeMs { get; init; }

    public string LastProbeMessage { get; init; } = string.Empty;

    public string AuthenticatorTypeName { get; init; } = string.Empty;
}
