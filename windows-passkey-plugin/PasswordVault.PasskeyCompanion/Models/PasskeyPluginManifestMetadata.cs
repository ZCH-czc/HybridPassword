using System.Text;

namespace PasswordVault.PasskeyCompanion.Models;

internal static class PasskeyPluginManifestMetadata
{
    public const string AuthenticatorName = "Password Vault Passkey Manager";
    public const string PluginRpId = "passkeys.passwordvault.app";
    public const string AuthenticatorClassIdString = "992c709b-7945-4edb-ac3d-b6f8dd17b2ae";
    public const string AuthenticatorAaguidString = "e55e39c2-a705-4578-9feb-e0660c139196";

    public static readonly Guid AuthenticatorClassId = Guid.Parse(AuthenticatorClassIdString);

    public static readonly byte[] AuthenticatorInfo = BuildAuthenticatorInfo();

    public static readonly string LightThemeLogoSvgBase64 = BuildLogoSvgBase64(
        startColor: "#6a8dff",
        endColor: "#2563eb",
        lockFill: "#ffffff",
        badgeFill: "#eef4ff");

    public static readonly string DarkThemeLogoSvgBase64 = BuildLogoSvgBase64(
        startColor: "#4a6cf1",
        endColor: "#183ea9",
        lockFill: "#f8fbff",
        badgeFill: "#d9e4ff");

    private static byte[] BuildAuthenticatorInfo()
    {
        // CBOR for:
        // {1:["FIDO_2_0","FIDO_2_1"],2:["prf","hmac-secret"],3:h'<AAGUID>',4:{"rk":true,"up":true,"uv":true},9:["internal"],10:[{"alg":-7,"type":"public-key"}]}
        const string part1 =
            "A60182684649444F5F325F30684649444F5F325F310282637072666B686D61632D7365637265740350";
        const string part2 =
            "04A362726BF5627570F5627576F5098168696E7465726E616C0A81A263616C672664747970656A7075626C69632D6B6579";

        var aaguidHex = AuthenticatorAaguidString.Replace("-", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        return Convert.FromHexString(part1 + aaguidHex + part2);
    }

    private static string BuildLogoSvgBase64(
        string startColor,
        string endColor,
        string lockFill,
        string badgeFill)
    {
        var svg = $$"""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 96 96">
              <defs>
                <linearGradient id="g" x1="10%" y1="0%" x2="90%" y2="100%">
                  <stop offset="0%" stop-color="{{startColor}}" />
                  <stop offset="100%" stop-color="{{endColor}}" />
                </linearGradient>
              </defs>
              <rect x="8" y="8" width="80" height="80" rx="24" fill="url(#g)" />
              <path d="M48 22c-12 0-22 6-22 6v18c0 20 12.5 29.7 22 34 9.5-4.3 22-14 22-34V28s-10-6-22-6Z" fill="{{badgeFill}}" opacity="0.96" />
              <path d="M48 33c-6.8 0-12.2 5.4-12.2 12.2v3.1h-2.7a2.5 2.5 0 0 0-2.5 2.5v15.9a2.5 2.5 0 0 0 2.5 2.5h29.8a2.5 2.5 0 0 0 2.5-2.5V50.8a2.5 2.5 0 0 0-2.5-2.5h-2.7v-3.1C60.2 38.4 54.8 33 48 33Zm-7.1 12.2c0-3.9 3.2-7.1 7.1-7.1s7.1 3.2 7.1 7.1v3.1H40.9v-3.1Z" fill="{{lockFill}}" />
            </svg>
            """;

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
    }
}
