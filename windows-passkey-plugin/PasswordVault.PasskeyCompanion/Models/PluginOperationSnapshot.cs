namespace PasswordVault.PasskeyCompanion.Models;

public sealed class PluginOperationSnapshot
{
    public string OperationId { get; init; } = string.Empty;

    public string Kind { get; init; } = string.Empty;

    public string State { get; init; } = "idle";

    public string Source { get; init; } = "plugin";

    public long CreatedAtUnixTimeMs { get; init; }

    public long UpdatedAtUnixTimeMs { get; init; }

    public bool RequestPointerPresent { get; init; }

    public bool ResponsePointerPresent { get; init; }

    public bool CancelPointerPresent { get; init; }

    public string Message { get; init; } = string.Empty;

    public string HResultHex { get; init; } = string.Empty;
}
