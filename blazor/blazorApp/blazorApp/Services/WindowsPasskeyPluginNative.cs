using System.Runtime.InteropServices;
#if WINDOWS
using Microsoft.Win32;
#endif

namespace blazorApp.Services;

internal sealed class WindowsPasskeyPluginCapabilityState
{
    public bool IsSupportedOsBuild { get; init; }

    public bool PluginApisAvailable { get; init; }

    public bool SupportsPluginManager => IsSupportedOsBuild && PluginApisAvailable;

    public bool RequiresCompanionApp => true;

    public bool CompanionAppIntegrated => false;

    public int BuildNumber { get; init; }

    public int Ubr { get; init; }

    public string StatusMessage { get; init; } = string.Empty;
}

internal static class WindowsPasskeyPluginNative
{
    private const string WebAuthnLibrary = "webauthn.dll";
    private const int MinimumSupportedBuild = 26100;
    private const int MinimumSupportedUbr = 6725;

    private static readonly string[] RequiredExports =
    [
        "WebAuthNPluginAddAuthenticator",
        "WebAuthNPluginRemoveAuthenticator",
        "WebAuthNPluginUpdateAuthenticatorDetails",
        "WebAuthNPluginAuthenticatorAddCredentials",
        "WebAuthNPluginAuthenticatorRemoveCredentials",
        "WebAuthNPluginPerformUserVerification",
    ];

    private static readonly Lazy<bool> PluginApiAvailability = new(DetectPluginApiAvailability);

    internal static WindowsPasskeyPluginCapabilityState GetCapabilityState()
    {
#if WINDOWS
        var (build, ubr) = ReadWindowsBuild();
        var isSupportedOsBuild = build > MinimumSupportedBuild ||
                                 (build == MinimumSupportedBuild && ubr >= MinimumSupportedUbr);
        var pluginApisAvailable = PluginApiAvailability.Value;

        return new WindowsPasskeyPluginCapabilityState
        {
            IsSupportedOsBuild = isSupportedOsBuild,
            PluginApisAvailable = pluginApisAvailable,
            BuildNumber = build,
            Ubr = ubr,
            StatusMessage = BuildStatusMessage(isSupportedOsBuild, pluginApisAvailable, build, ubr),
        };
#else
        return new WindowsPasskeyPluginCapabilityState
        {
            IsSupportedOsBuild = false,
            PluginApisAvailable = false,
            BuildNumber = 0,
            Ubr = 0,
            StatusMessage = "Windows third-party passkey manager support is only available on Windows.",
        };
#endif
    }

    private static bool DetectPluginApiAvailability()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        if (!NativeLibrary.TryLoad(WebAuthnLibrary, out var handle))
        {
            return false;
        }

        try
        {
            return RequiredExports.All(exportName =>
                NativeLibrary.TryGetExport(handle, exportName, out _));
        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }

    private static (int build, int ubr) ReadWindowsBuild()
    {
#if WINDOWS
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key == null)
            {
                return (0, 0);
            }

            var buildText = key.GetValue("CurrentBuildNumber")?.ToString() ?? "0";
            var ubrValue = key.GetValue("UBR");
            _ = int.TryParse(buildText, out var build);
            var ubr = ubrValue is int ubrInt ? ubrInt : 0;
            return (build, ubr);
        }
        catch
        {
            return (0, 0);
        }
#else
        return (0, 0);
#endif
    }

    private static string BuildStatusMessage(
        bool isSupportedOsBuild,
        bool pluginApisAvailable,
        int build,
        int ubr)
    {
        if (!isSupportedOsBuild)
        {
            return $"Windows plugin passkey manager requires Windows 11 build 26100.{MinimumSupportedUbr}+ or newer. Current build: {build}.{ubr}.";
        }

        if (!pluginApisAvailable)
        {
            return "This Windows device does not expose the required WebAuthn plugin exports yet.";
        }

        return "Windows plugin passkey manager APIs are available. The next step is a separate packaged companion app.";
    }
}
