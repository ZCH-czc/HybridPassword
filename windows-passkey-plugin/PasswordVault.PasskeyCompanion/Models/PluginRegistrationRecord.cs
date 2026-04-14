namespace PasswordVault.PasskeyCompanion.Models;

public sealed class PluginRegistrationRecord
{
    public long LastRegistrationAttemptUnixTimeMs { get; set; }

    public string LastRegistrationMessage { get; set; } = "No registration attempt has been recorded yet.";

    public int LastRegistrationHResult { get; set; }

    public bool RegistrationCompleted { get; set; }

    public int AuthenticatorStateCode { get; set; }

    public string AuthenticatorStateLabel { get; set; } = "unknown";

    public string OperationSigningPublicKeyBase64 { get; set; } = string.Empty;

    public long OperationSigningPublicKeyStoredAtUnixTimeMs { get; set; }
}
