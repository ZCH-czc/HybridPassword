namespace PasswordVault.PasskeyCompanion.Models;

public sealed class PluginActivationSnapshot
{
    public int ActivationCount { get; init; }

    public long LastActivationUnixTimeMs { get; init; }

    public string LastActivationSource { get; init; } = string.Empty;

    public bool StartedFromPluginActivation { get; init; }
}
