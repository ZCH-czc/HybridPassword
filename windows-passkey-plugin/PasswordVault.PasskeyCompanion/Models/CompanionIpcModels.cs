namespace PasswordVault.PasskeyCompanion.Models;

public sealed class CompanionIpcRequest
{
    public string Action { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;
}

public sealed class CompanionIpcResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public CompanionStatusSnapshot? Snapshot { get; set; }

    public CompanionPluginWorkflowSnapshot? Workflow { get; set; }
}
