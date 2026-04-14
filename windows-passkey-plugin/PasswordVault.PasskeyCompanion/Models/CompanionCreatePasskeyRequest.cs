namespace PasswordVault.PasskeyCompanion.Models;

public sealed class CompanionCreatePasskeyRequest
{
    public string RpId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string UserHandle { get; set; } = string.Empty;
}
