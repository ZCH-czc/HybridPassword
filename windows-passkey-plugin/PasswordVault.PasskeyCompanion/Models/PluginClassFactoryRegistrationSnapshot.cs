namespace PasswordVault.PasskeyCompanion.Models;

public sealed class PluginClassFactoryRegistrationSnapshot
{
    public bool IsRegistered { get; init; }

    public uint RegistrationCookie { get; init; }

    public long LastRegistrationUnixTimeMs { get; init; }

    public string LastMessage { get; init; } = "The local COM class factory has not been registered yet.";

    public int LastHResult { get; init; }
}
