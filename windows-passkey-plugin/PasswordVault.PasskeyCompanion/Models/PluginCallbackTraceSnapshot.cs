namespace PasswordVault.PasskeyCompanion.Models;

public sealed class PluginCallbackTraceSnapshot
{
    public int TotalCount { get; init; }

    public int MakeCredentialCount { get; init; }

    public int GetAssertionCount { get; init; }

    public int CancelOperationCount { get; init; }

    public int GetLockStatusCount { get; init; }

    public long LastCallbackUnixTimeMs { get; init; }

    public string LastCallbackKind { get; init; } = string.Empty;

    public string LastCallbackMessage { get; init; } = "No plugin callback has been captured yet.";

    public string LastCallbackHResultHex { get; init; } = string.Empty;
}
