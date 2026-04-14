using Microsoft.Win32;
using PasswordVault.PasskeyCompanion.Models;
using System.Runtime.InteropServices;
using System.Text;

namespace PasswordVault.PasskeyCompanion.Services;

public sealed class CompanionStatusService
{
    private const string WebAuthnLibrary = "webauthn.dll";
    private const int MinimumSupportedBuild = 26100;
    private const int MinimumSupportedUbr = 6725;
    private const int AppModelErrorNoPackage = 15700;
    private const int ErrorInsufficientBuffer = 122;

    private static readonly string[] RequiredPluginExports =
    [
        "WebAuthNPluginAddAuthenticator",
        "WebAuthNPluginRemoveAuthenticator",
        "WebAuthNPluginUpdateAuthenticatorDetails",
        "WebAuthNPluginAuthenticatorAddCredentials",
        "WebAuthNPluginAuthenticatorRemoveCredentials",
        "WebAuthNPluginPerformUserVerification",
    ];

    private readonly Func<CompanionRuntimeState>? _runtimeStateProvider;

    public CompanionStatusService(Func<CompanionRuntimeState>? runtimeStateProvider = null)
    {
        _runtimeStateProvider = runtimeStateProvider;
    }

    public CompanionStatusSnapshot Probe()
    {
        var checkedAt = DateTimeOffset.Now;
        var (build, ubr) = ReadWindowsBuild();
        var meetsBuildRequirement = build > MinimumSupportedBuild ||
                                    (build == MinimumSupportedBuild && ubr >= MinimumSupportedUbr);

        bool webAuthnAvailable;
        List<string> missingExports;
        ProbeWebAuthnLibrary(out webAuthnAvailable, out missingExports);

        var packageIdentity = ReadPackageIdentity();
        var isPackaged = packageIdentity.IsPackaged;
        var pluginExportsAvailable = webAuthnAvailable && missingExports.Count == 0;
        var runtimeState = _runtimeStateProvider?.Invoke() ?? new CompanionRuntimeState();

        return new CompanionStatusSnapshot
        {
            CheckedAt = checkedAt,
            BuildNumber = build,
            Ubr = ubr,
            MeetsPluginBuildRequirement = meetsBuildRequirement,
            WebAuthnLibraryAvailable = webAuthnAvailable,
            PluginExportsAvailable = pluginExportsAvailable,
            IsPackagedProcess = isPackaged,
            HasHostSessionLink = runtimeState.HasHostSessionLink,
            HostProcessId = runtimeState.HostProcessId,
            HostStartTimeUtcTicks = runtimeState.HostStartTimeUtcTicks,
            HostProcessAlive = runtimeState.HostProcessAlive,
            PackageFamilyName = packageIdentity.FamilyName,
            PackageFullName = packageIdentity.FullName,
            MissingExports = missingExports,
            NextSteps = BuildNextSteps(
                meetsBuildRequirement,
                webAuthnAvailable,
                pluginExportsAvailable,
                isPackaged,
                runtimeState),
            StatusSummary = BuildStatusSummary(
                meetsBuildRequirement,
                webAuthnAvailable,
                pluginExportsAvailable,
                isPackaged,
                runtimeState),
            DetailMessage = BuildDetailMessage(
                build,
                ubr,
                meetsBuildRequirement,
                webAuthnAvailable,
                pluginExportsAvailable,
                isPackaged,
                runtimeState),
        };
    }

    private static (int build, int ubr) ReadWindowsBuild()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key == null)
            {
                return (0, 0);
            }

            var buildText = key.GetValue("CurrentBuildNumber")?.ToString() ?? "0";
            var ubrObject = key.GetValue("UBR");
            _ = int.TryParse(buildText, out var build);
            var ubr = ubrObject is int ubrValue ? ubrValue : 0;
            return (build, ubr);
        }
        catch
        {
            return (0, 0);
        }
    }

    private static void ProbeWebAuthnLibrary(out bool available, out List<string> missingExports)
    {
        available = false;
        missingExports = [];

        if (!NativeLibrary.TryLoad(WebAuthnLibrary, out var handle))
        {
            return;
        }

        available = true;
        try
        {
            foreach (var exportName in RequiredPluginExports)
            {
                if (!NativeLibrary.TryGetExport(handle, exportName, out _))
                {
                    missingExports.Add(exportName);
                }
            }
        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }

    private static PackageIdentityState ReadPackageIdentity()
    {
        var fullName = ReadPackageString(GetCurrentPackageFullName);
        var familyName = ReadPackageString(GetCurrentPackageFamilyName);

        return new PackageIdentityState(
            !string.IsNullOrWhiteSpace(fullName),
            familyName,
            fullName);
    }

    private static string ReadPackageString(PackageStringReader reader)
    {
        var length = 0;
        var result = reader(ref length, null);
        if (result == AppModelErrorNoPackage || result != ErrorInsufficientBuffer || length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length);
        result = reader(ref length, builder);
        return result == 0 ? builder.ToString() : string.Empty;
    }

    private static IReadOnlyList<string> BuildNextSteps(
        bool meetsBuildRequirement,
        bool webAuthnAvailable,
        bool pluginExportsAvailable,
        bool isPackaged,
        CompanionRuntimeState runtimeState)
    {
        var steps = new List<string>();

        if (!meetsBuildRequirement)
        {
            steps.Add($"Upgrade Windows 11 to build {MinimumSupportedBuild}.{MinimumSupportedUbr}+.");
        }

        if (!webAuthnAvailable)
        {
            steps.Add("Verify that webauthn.dll is available on this Windows installation.");
        }

        if (webAuthnAvailable && !pluginExportsAvailable)
        {
            steps.Add("Use a Windows build that exposes the plugin passkey manager exports.");
        }

        if (!isPackaged)
        {
            steps.Add("Package this companion app with MSIX and a COM server manifest entry before real plugin registration can succeed.");
        }

        if (!runtimeState.HasHostSessionLink)
        {
            steps.Add("Launch this companion from the MAUI host so it can attach to the current app session.");
        }

        steps.Add("Finish the authenticator registration path and bridge the result back to the MAUI vault host.");
        return steps;
    }

    private static string BuildStatusSummary(
        bool meetsBuildRequirement,
        bool webAuthnAvailable,
        bool pluginExportsAvailable,
        bool isPackaged,
        CompanionRuntimeState runtimeState)
    {
        var readinessSummary =
            meetsBuildRequirement && webAuthnAvailable && pluginExportsAvailable && isPackaged
                ? "Companion app prerequisites are in place."
                : "Companion app is not ready for plugin registration yet.";

        if (runtimeState.HasHostSessionLink && runtimeState.HostProcessAlive)
        {
            return $"{readinessSummary} Linked to the current MAUI host session.";
        }

        if (runtimeState.HasHostSessionLink)
        {
            return $"{readinessSummary} The linked host session is no longer alive.";
        }

        return $"{readinessSummary} Running without a host-session link.";
    }

    private static string BuildDetailMessage(
        int build,
        int ubr,
        bool meetsBuildRequirement,
        bool webAuthnAvailable,
        bool pluginExportsAvailable,
        bool isPackaged,
        CompanionRuntimeState runtimeState)
    {
        var parts = new List<string>
        {
            $"Current Windows build: {build}.{ubr}."
        };

        parts.Add(meetsBuildRequirement
            ? "OS build requirement is satisfied."
            : $"Plugin manager support needs Windows 11 build {MinimumSupportedBuild}.{MinimumSupportedUbr}+.");

        parts.Add(webAuthnAvailable
            ? "webauthn.dll is available."
            : "webauthn.dll could not be loaded.");

        parts.Add(pluginExportsAvailable
            ? "Required plugin exports were found."
            : "Some required plugin exports are still missing.");

        parts.Add(isPackaged
            ? "The process is packaged."
            : "The process is not packaged yet, so Windows plugin registration is still blocked.");

        if (runtimeState.HasHostSessionLink && runtimeState.HostProcessAlive)
        {
            parts.Add(
                $"This background companion is attached to host process {runtimeState.HostProcessId} and will shut down if that host session ends.");
        }
        else if (runtimeState.HasHostSessionLink)
        {
            parts.Add(
                $"The previously linked host process {runtimeState.HostProcessId} is no longer alive. The companion will exit shortly.");
        }
        else
        {
            parts.Add("This companion is running without a linked host session.");
        }

        return string.Join(" ", parts);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFamilyName(ref int packageFamilyNameLength, StringBuilder? packageFamilyName);

    private delegate int PackageStringReader(ref int length, StringBuilder? value);

    private readonly record struct PackageIdentityState(
        bool IsPackaged,
        string FamilyName,
        string FullName);
}
