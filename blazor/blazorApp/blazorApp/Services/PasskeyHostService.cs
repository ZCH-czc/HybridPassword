namespace blazorApp.Services;

public sealed class PasskeyHostService : IPasskeyHostService
{
    private const string WindowsCompanionAutoLaunchPreferenceKey =
        "password_vault.windows.passkey_companion_auto_launch";

    private readonly IPasskeyCompanionClientService _companionClientService;
    private readonly IPasskeyCompanionLauncherService _companionLauncherService;
    private readonly IPasskeyDiagnosticsService _diagnosticsService;
    private bool _sessionStartedCompanion;
    private int _cleanupTriggered;

    public PasskeyHostService(
        IPasskeyCompanionClientService companionClientService,
        IPasskeyCompanionLauncherService companionLauncherService,
        IPasskeyDiagnosticsService diagnosticsService)
    {
        _companionClientService = companionClientService;
        _companionLauncherService = companionLauncherService;
        _diagnosticsService = diagnosticsService;
    }

    public async Task<PasskeyBridgeState> GetStateAsync()
    {
        var isSupported = IsWindowsPasskeyPlatformSupported();
        var apiVersion = WindowsPasskeyNative.GetApiVersion();
        var hasPlatformAuthenticator = WindowsPasskeyNative.HasPlatformAuthenticator();
        var pluginCapability = WindowsPasskeyPluginNative.GetCapabilityState();
        var companionProbe = await _companionClientService.TryGetStatusAsync().ConfigureAwait(false);

        var pluginStatus = pluginCapability.StatusMessage;
        if (companionProbe.IsReachable && companionProbe.Snapshot is not null)
        {
            pluginStatus = $"{pluginCapability.StatusMessage} Companion: {companionProbe.Snapshot.StatusSummary}";
        }
        else if (!string.IsNullOrWhiteSpace(companionProbe.Message))
        {
            pluginStatus = $"{pluginCapability.StatusMessage} Companion: {companionProbe.Message}";
        }

        return new PasskeyBridgeState
        {
            IsSupported = isSupported,
            SupportsMetadataManagement = isSupported,
            SupportsPluginManager = pluginCapability.SupportsPluginManager,
            RequiresCompanionApp = pluginCapability.RequiresCompanionApp,
            CompanionAppIntegrated = companionProbe.IsReachable,
            CanLaunchCompanionApp = _companionLauncherService.CanLaunchCompanionApp(),
            SupportsCompanionAutoLaunch = IsWindowsPlatform(),
            CompanionAutoLaunchEnabled = GetCompanionAutoLaunchEnabled(),
            ApiVersion = apiVersion,
            HasPlatformAuthenticator = hasPlatformAuthenticator,
            Platform = GetPlatformName(),
            Message = isSupported
                ? $"Windows platform passkey metadata is available. WebAuthn API v{apiVersion} detected."
                : "Passkey management is currently planned for supported Windows devices only.",
            PluginStatus = pluginStatus,
            CompanionCheckedAtUnixTimeMs = companionProbe.Snapshot?.CheckedAtUnixTimeMs ?? 0,
            CompanionBuildNumber = companionProbe.Snapshot?.BuildNumber ?? 0,
            CompanionUbr = companionProbe.Snapshot?.Ubr ?? 0,
            CompanionMeetsPluginBuildRequirement = companionProbe.Snapshot?.MeetsPluginBuildRequirement ?? false,
            CompanionWebAuthnLibraryAvailable = companionProbe.Snapshot?.WebAuthnLibraryAvailable ?? false,
            CompanionPluginExportsAvailable = companionProbe.Snapshot?.PluginExportsAvailable ?? false,
            CompanionIsPackagedProcess = companionProbe.Snapshot?.IsPackagedProcess ?? false,
            CompanionStatusSummary = companionProbe.Snapshot?.StatusSummary ?? string.Empty,
            CompanionDetailMessage = companionProbe.Snapshot?.DetailMessage ?? string.Empty,
            CompanionWorkflowMode = companionProbe.Workflow?.WorkflowMode ?? "skeleton",
            CompanionRegistrationAttempted = companionProbe.Workflow?.RegistrationAttempted ?? false,
            CompanionRegistrationPrepared = companionProbe.Workflow?.RegistrationPrepared ?? false,
            CompanionRegistrationEnvironmentReady = companionProbe.Workflow?.RegistrationEnvironmentReady ?? false,
            CompanionRegistrationCompleted = companionProbe.Workflow?.RegistrationCompleted ?? false,
            CompanionLastRegistrationAttemptUnixTimeMs = companionProbe.Workflow?.LastRegistrationAttemptUnixTimeMs ?? 0,
            CompanionRegistrationStatus = companionProbe.Workflow?.RegistrationStatus ?? string.Empty,
            CompanionLastRegistrationMessage = companionProbe.Workflow?.LastRegistrationMessage ?? string.Empty,
            CompanionLastRegistrationHResultHex = companionProbe.Workflow?.LastRegistrationHResultHex ?? string.Empty,
            CompanionAuthenticatorStateCode = companionProbe.Workflow?.AuthenticatorStateCode ?? 0,
            CompanionAuthenticatorStateLabel = companionProbe.Workflow?.AuthenticatorStateLabel ?? "unknown",
            CompanionHasOperationSigningPublicKey = companionProbe.Workflow?.HasOperationSigningPublicKey ?? false,
            CompanionOperationSigningPublicKeyStoredAtUnixTimeMs =
                companionProbe.Workflow?.OperationSigningPublicKeyStoredAtUnixTimeMs ?? 0,
            CompanionComSkeletonReady = companionProbe.Workflow?.ComSkeletonReady ?? false,
            CompanionComClassIdMatchesManifest = companionProbe.Workflow?.ComClassIdMatchesManifest ?? false,
            CompanionComFactoryReady = companionProbe.Workflow?.ComFactoryReady ?? false,
            CompanionComAuthenticatorReady = companionProbe.Workflow?.ComAuthenticatorReady ?? false,
            CompanionComLastProbeUnixTimeMs = companionProbe.Workflow?.ComLastProbeUnixTimeMs ?? 0,
            CompanionComLastProbeMessage = companionProbe.Workflow?.ComLastProbeMessage ?? string.Empty,
            CompanionComAuthenticatorTypeName = companionProbe.Workflow?.ComAuthenticatorTypeName ?? string.Empty,
            CompanionComClassFactoryRegistered = companionProbe.Workflow?.ComClassFactoryRegistered ?? false,
            CompanionComClassFactoryRegistrationCookie = companionProbe.Workflow?.ComClassFactoryRegistrationCookie ?? 0,
            CompanionComClassFactoryLastRegistrationUnixTimeMs =
                companionProbe.Workflow?.ComClassFactoryLastRegistrationUnixTimeMs ?? 0,
            CompanionComClassFactoryLastMessage = companionProbe.Workflow?.ComClassFactoryLastMessage ?? string.Empty,
            CompanionComClassFactoryLastHResultHex = companionProbe.Workflow?.ComClassFactoryLastHResultHex ?? string.Empty,
            CompanionCallbackTotalCount = companionProbe.Workflow?.CallbackTotalCount ?? 0,
            CompanionCallbackMakeCredentialCount = companionProbe.Workflow?.CallbackMakeCredentialCount ?? 0,
            CompanionCallbackGetAssertionCount = companionProbe.Workflow?.CallbackGetAssertionCount ?? 0,
            CompanionCallbackCancelOperationCount = companionProbe.Workflow?.CallbackCancelOperationCount ?? 0,
            CompanionCallbackGetLockStatusCount = companionProbe.Workflow?.CallbackGetLockStatusCount ?? 0,
            CompanionCallbackLastUnixTimeMs = companionProbe.Workflow?.CallbackLastUnixTimeMs ?? 0,
            CompanionCallbackLastKind = companionProbe.Workflow?.CallbackLastKind ?? string.Empty,
            CompanionCallbackLastMessage = companionProbe.Workflow?.CallbackLastMessage ?? string.Empty,
            CompanionCallbackLastHResultHex = companionProbe.Workflow?.CallbackLastHResultHex ?? string.Empty,
            CompanionLatestOperationId = companionProbe.Workflow?.LatestOperationId ?? string.Empty,
            CompanionLatestOperationKind = companionProbe.Workflow?.LatestOperationKind ?? string.Empty,
            CompanionLatestOperationState = companionProbe.Workflow?.LatestOperationState ?? "idle",
            CompanionLatestOperationSource = companionProbe.Workflow?.LatestOperationSource ?? string.Empty,
            CompanionLatestOperationCreatedAtUnixTimeMs =
                companionProbe.Workflow?.LatestOperationCreatedAtUnixTimeMs ?? 0,
            CompanionLatestOperationUpdatedAtUnixTimeMs =
                companionProbe.Workflow?.LatestOperationUpdatedAtUnixTimeMs ?? 0,
            CompanionLatestOperationRequestPointerPresent =
                companionProbe.Workflow?.LatestOperationRequestPointerPresent ?? false,
            CompanionLatestOperationResponsePointerPresent =
                companionProbe.Workflow?.LatestOperationResponsePointerPresent ?? false,
            CompanionLatestOperationCancelPointerPresent =
                companionProbe.Workflow?.LatestOperationCancelPointerPresent ?? false,
            CompanionLatestOperationMessage = companionProbe.Workflow?.LatestOperationMessage ?? string.Empty,
            CompanionLatestOperationHResultHex = companionProbe.Workflow?.LatestOperationHResultHex ?? string.Empty,
            CompanionActivationCount = companionProbe.Workflow?.ActivationCount ?? 0,
            CompanionLastActivationUnixTimeMs = companionProbe.Workflow?.LastActivationUnixTimeMs ?? 0,
            CompanionLastActivationSource = companionProbe.Workflow?.LastActivationSource ?? string.Empty,
            CompanionStartedFromPluginActivation = companionProbe.Workflow?.StartedFromPluginActivation ?? false,
            CompanionCreateRequestCount = companionProbe.Workflow?.CreateRequestCount ?? 0,
            CompanionLastCreateRequestUnixTimeMs = companionProbe.Workflow?.LastCreateRequestUnixTimeMs ?? 0,
            CompanionLastCreateRequestRpId = companionProbe.Workflow?.LastCreateRequestRpId ?? string.Empty,
            CompanionLastCreateRequestUsername = companionProbe.Workflow?.LastCreateRequestUsername ?? string.Empty,
            CompanionLastCreateRequestMessage = companionProbe.Workflow?.LastCreateRequestMessage ?? string.Empty,
            RecentLogs = _diagnosticsService.GetEntries().ToArray(),
        };
    }

    public Task<IReadOnlyList<PasskeyMetadataState>> ListPasskeysAsync()
    {
        return Task.FromResult(WindowsPasskeyNative.ListPasskeys());
    }

    public async Task<HostOperationResult> RefreshMetadataAsync()
    {
        try
        {
            var items = await ListPasskeysAsync();
            _diagnosticsService.AddInfo(
                "passkey-host",
                $"Refreshed {items.Count} Windows passkey metadata entr{(items.Count == 1 ? "y" : "ies")}.");
            return new HostOperationResult
            {
                Success = true,
                Message = $"Refreshed {items.Count} Windows passkey metadata entr{(items.Count == 1 ? "y" : "ies")}.",
            };
        }
        catch (Exception exception)
        {
            _diagnosticsService.AddError(
                "passkey-host",
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to refresh Windows passkey metadata."
                    : exception.Message);
            return BuildFailureResult(exception, "Unable to refresh Windows passkey metadata.");
        }
    }

    public Task<HostOperationResult> CreatePasskeyAsync(PasskeyCreateRequest request)
    {
        return CreatePasskeySkeletonAsync(request);
    }

    public Task<HostOperationResult> UsePasskeyAsync(PasskeyUseRequest request)
    {
        return Task.FromResult(BuildPhaseZeroResult(
            "Passkey use has not been connected to the Windows-native provider yet."));
    }

    public Task<HostOperationResult> DeletePasskeyAsync(PasskeyDeleteRequest request)
    {
        try
        {
            WindowsPasskeyNative.DeletePasskey(request.NativeProviderRecordId);
            _diagnosticsService.AddInfo("passkey-host", "The Windows passkey was deleted.");
            return Task.FromResult(new HostOperationResult
            {
                Success = true,
                Message = "The Windows passkey was deleted.",
            });
        }
        catch (Exception exception)
        {
            _diagnosticsService.AddError(
                "passkey-host",
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to delete the Windows passkey."
                    : exception.Message);
            return Task.FromResult(BuildFailureResult(exception, "Unable to delete the Windows passkey."));
        }
    }

    public async Task<HostOperationResult> ResolveLatestOperationAsync(string resolution, string message = "")
    {
        if (!IsWindowsPlatform())
        {
            return BuildPhaseZeroResult("Passkey operation handling is only planned for the Windows host.");
        }

        var companionProbe = await _companionClientService.TryGetStatusAsync().ConfigureAwait(false);
        if (!companionProbe.IsReachable)
        {
            var launchResult = await LaunchCompanionAsync().ConfigureAwait(false);
            if (!launchResult.Success)
            {
                return launchResult;
            }
        }

        var result = await _companionClientService.TryResolveLatestOperationAsync(
            resolution,
            message).ConfigureAwait(false);
        if (result.Success)
        {
            _diagnosticsService.AddInfo("passkey-host", result.Message);
        }
        else
        {
            _diagnosticsService.AddWarning("passkey-host", result.Message);
        }

        return result;
    }

    public async Task<HostOperationResult> LaunchCompanionAsync()
    {
        _diagnosticsService.AddInfo("passkey-host", "Attempting to activate or launch the Windows passkey companion.");
        var activationResult = await _companionClientService.TryActivateAsync().ConfigureAwait(false);
        if (activationResult.Success)
        {
            _diagnosticsService.AddInfo("passkey-host", activationResult.Message);
            await PrimePluginRegistrationAsync().ConfigureAwait(false);
            return activationResult;
        }

        var launchResult = await _companionLauncherService.LaunchAsync().ConfigureAwait(false);
        if (!launchResult.Success)
        {
            _diagnosticsService.AddWarning("passkey-host", launchResult.Message);
        }
        else
        {
            _sessionStartedCompanion = true;
            await PrimePluginRegistrationAsync().ConfigureAwait(false);
        }

        return launchResult;
    }

    public async Task<HostOperationResult> RestartCompanionAsync()
    {
        _diagnosticsService.AddInfo("passkey-host", "Attempting to restart the Windows passkey companion.");
        var restartResult = await _companionLauncherService.RestartAsync().ConfigureAwait(false);
        if (!restartResult.Success)
        {
            _diagnosticsService.AddWarning("passkey-host", restartResult.Message);
        }
        else
        {
            _sessionStartedCompanion = true;
        }

        return restartResult;
    }

    public Task<HostOperationResult> SetCompanionAutoLaunchAsync(bool enabled)
    {
        if (!IsWindowsPlatform())
        {
            return Task.FromResult(new HostOperationResult
            {
                Success = false,
                Message = "Automatic Windows companion launch is only available on Windows.",
            });
        }

        Preferences.Default.Set(WindowsCompanionAutoLaunchPreferenceKey, enabled);
        _diagnosticsService.AddInfo(
            "passkey-host",
            enabled
                ? "Automatic Windows passkey companion launch was turned on."
                : "Automatic Windows passkey companion launch was turned off.");
        return Task.FromResult(new HostOperationResult
        {
            Success = true,
            Message = enabled
                ? "The Windows passkey companion will be launched automatically when the app starts."
                : "Automatic Windows passkey companion launch was turned off.",
        });
    }

    public async Task EnsureCompanionOnHostStartAsync()
    {
        if (!IsWindowsPlatform() || !GetCompanionAutoLaunchEnabled())
        {
            return;
        }

        try
        {
            _diagnosticsService.AddInfo(
                "passkey-host",
                "Automatic startup is enabled. The app will try to launch the Windows passkey companion now.");
            var result = await LaunchCompanionAsync().ConfigureAwait(false);
            if (!result.Success)
            {
                _diagnosticsService.AddWarning("passkey-host", result.Message);
            }
        }
        catch
        {
        }
    }

    private async Task<HostOperationResult> CreatePasskeySkeletonAsync(PasskeyCreateRequest request)
    {
        if (!IsWindowsPlatform())
        {
            return BuildPhaseZeroResult("Passkey creation is only planned for the Windows host.");
        }

        if (string.IsNullOrWhiteSpace(request?.RpId) || string.IsNullOrWhiteSpace(request?.Username))
        {
            return new HostOperationResult
            {
                Success = false,
                Message = "rpId and username are required before a passkey-create request can be prepared.",
            };
        }

        var companionProbe = await _companionClientService.TryGetStatusAsync().ConfigureAwait(false);
        if (!companionProbe.IsReachable)
        {
            var launchResult = await LaunchCompanionAsync().ConfigureAwait(false);
            if (!launchResult.Success)
            {
                return launchResult;
            }
        }

        var result = await _companionClientService.TryCreatePasskeySkeletonAsync(
            request ?? new PasskeyCreateRequest()).ConfigureAwait(false);
        if (result.Success)
        {
            _diagnosticsService.AddInfo("passkey-host", result.Message);
        }
        else
        {
            _diagnosticsService.AddWarning("passkey-host", result.Message);
        }

        return result;
    }

    private async Task PrimePluginRegistrationAsync()
    {
        var result = await _companionClientService.TryPreparePluginRegistrationAsync().ConfigureAwait(false);
        if (result.Success)
        {
            _diagnosticsService.AddInfo("passkey-host", result.Message);
            return;
        }

        _diagnosticsService.AddWarning("passkey-host", result.Message);
    }

    public async Task CleanupCompanionOnHostExitAsync()
    {
        if (!IsWindowsPlatform())
        {
            return;
        }

        if (Interlocked.Exchange(ref _cleanupTriggered, 1) != 0)
        {
            return;
        }

        if (!_sessionStartedCompanion)
        {
            _diagnosticsService.AddInfo(
                "passkey-host",
                "App exit cleanup skipped because the current session did not launch the Windows passkey companion.");
            return;
        }

        try
        {
            _diagnosticsService.AddInfo(
                "passkey-host",
                "The main app is exiting. It will now stop the Windows passkey companion it started during this session.");
            var result = await _companionLauncherService.StopAsync().ConfigureAwait(false);
            if (!result.Success)
            {
                _diagnosticsService.AddWarning("passkey-host", result.Message);
                return;
            }

            _diagnosticsService.AddInfo("passkey-host", result.Message);
        }
        catch (Exception exception)
        {
            _diagnosticsService.AddError(
                "passkey-host",
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to clean up the Windows passkey companion during app exit."
                    : exception.Message);
        }
    }

    private static HostOperationResult BuildPhaseZeroResult(string message)
    {
        return new HostOperationResult
        {
            Success = false,
            Message = message,
        };
    }

    private static bool IsWindowsPasskeyPlatformSupported()
    {
        return WindowsPasskeyNative.IsAvailable();
    }

    private static HostOperationResult BuildFailureResult(Exception exception, string fallbackMessage)
    {
        return new HostOperationResult
        {
            Success = false,
            Message = string.IsNullOrWhiteSpace(exception.Message)
                ? fallbackMessage
                : exception.Message,
        };
    }

    private static string GetPlatformName()
    {
#if WINDOWS
        return "windows";
#elif ANDROID
        return "android";
#elif IOS
        return "ios";
#elif MACCATALYST
        return "maccatalyst";
#else
        return "web";
#endif
    }

    private static bool GetCompanionAutoLaunchEnabled()
    {
#if WINDOWS
        return Preferences.Default.Get(WindowsCompanionAutoLaunchPreferenceKey, true);
#else
        return false;
#endif
    }

    private static bool IsWindowsPlatform()
    {
#if WINDOWS
        return true;
#else
        return false;
#endif
    }
}
