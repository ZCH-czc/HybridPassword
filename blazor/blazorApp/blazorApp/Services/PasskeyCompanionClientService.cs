using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace blazorApp.Services;

public sealed class PasskeyCompanionClientService : IPasskeyCompanionClientService
{
    private const string PipeName = "PasswordVault.PasskeyCompanion.v1";
    private const int ConnectTimeoutMs = 300;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IPasskeyDiagnosticsService _diagnosticsService;

    public PasskeyCompanionClientService(IPasskeyDiagnosticsService diagnosticsService)
    {
        _diagnosticsService = diagnosticsService;
    }

    public async Task<PasskeyCompanionProbeResult> TryGetStatusAsync(CancellationToken cancellationToken = default)
    {
#if WINDOWS
        try
        {
            var response = await SendCommandAsync("get_status", null, cancellationToken).ConfigureAwait(false);
            if (response is null)
            {
                return new PasskeyCompanionProbeResult
                {
                    IsReachable = false,
                    Message = "The passkey companion returned an empty response.",
                };
            }

            if (!response.Success)
            {
                _diagnosticsService.AddWarning("companion-ipc", response.Message ?? "The passkey companion returned an invalid status response.");
            }

            return new PasskeyCompanionProbeResult
            {
                IsReachable = response.Success,
                Message = response.Message ?? "The passkey companion did not return a valid response.",
                Snapshot = response.Snapshot == null
                    ? null
                    : new PasskeyCompanionStatusSnapshot
                    {
                        CheckedAtUnixTimeMs = response.Snapshot.CheckedAtUnixTimeMs,
                        BuildNumber = response.Snapshot.BuildNumber,
                        Ubr = response.Snapshot.Ubr,
                        MeetsPluginBuildRequirement = response.Snapshot.MeetsPluginBuildRequirement,
                        WebAuthnLibraryAvailable = response.Snapshot.WebAuthnLibraryAvailable,
                        PluginExportsAvailable = response.Snapshot.PluginExportsAvailable,
                        IsPackagedProcess = response.Snapshot.IsPackagedProcess,
                        StatusSummary = response.Snapshot.StatusSummary ?? string.Empty,
                        DetailMessage = response.Snapshot.DetailMessage ?? string.Empty,
                    },
                Workflow = response.Workflow == null
                    ? null
                    : new PasskeyCompanionWorkflowSnapshot
                    {
                        WorkflowMode = response.Workflow.WorkflowMode ?? "skeleton",
                        RegistrationAttempted = response.Workflow.RegistrationAttempted,
                        RegistrationPrepared = response.Workflow.RegistrationPrepared,
                        RegistrationEnvironmentReady = response.Workflow.RegistrationEnvironmentReady,
                        RegistrationCompleted = response.Workflow.RegistrationCompleted,
                        LastRegistrationAttemptUnixTimeMs = response.Workflow.LastRegistrationAttemptUnixTimeMs,
                        RegistrationStatus = response.Workflow.RegistrationStatus ?? string.Empty,
                        LastRegistrationMessage = response.Workflow.LastRegistrationMessage ?? string.Empty,
                        LastRegistrationHResultHex = response.Workflow.LastRegistrationHResultHex ?? string.Empty,
                        AuthenticatorStateCode = response.Workflow.AuthenticatorStateCode,
                        AuthenticatorStateLabel = response.Workflow.AuthenticatorStateLabel ?? "unknown",
                        HasOperationSigningPublicKey = response.Workflow.HasOperationSigningPublicKey,
                        OperationSigningPublicKeyStoredAtUnixTimeMs = response.Workflow.OperationSigningPublicKeyStoredAtUnixTimeMs,
                        ComSkeletonReady = response.Workflow.ComSkeletonReady,
                        ComClassIdMatchesManifest = response.Workflow.ComClassIdMatchesManifest,
                        ComFactoryReady = response.Workflow.ComFactoryReady,
                        ComAuthenticatorReady = response.Workflow.ComAuthenticatorReady,
                        ComLastProbeUnixTimeMs = response.Workflow.ComLastProbeUnixTimeMs,
                        ComLastProbeMessage = response.Workflow.ComLastProbeMessage ?? string.Empty,
                        ComAuthenticatorTypeName = response.Workflow.ComAuthenticatorTypeName ?? string.Empty,
                        ComClassFactoryRegistered = response.Workflow.ComClassFactoryRegistered,
                        ComClassFactoryRegistrationCookie = response.Workflow.ComClassFactoryRegistrationCookie,
                        ComClassFactoryLastRegistrationUnixTimeMs = response.Workflow.ComClassFactoryLastRegistrationUnixTimeMs,
                        ComClassFactoryLastMessage = response.Workflow.ComClassFactoryLastMessage ?? string.Empty,
                        ComClassFactoryLastHResultHex = response.Workflow.ComClassFactoryLastHResultHex ?? string.Empty,
                        CallbackTotalCount = response.Workflow.CallbackTotalCount,
                        CallbackMakeCredentialCount = response.Workflow.CallbackMakeCredentialCount,
                        CallbackGetAssertionCount = response.Workflow.CallbackGetAssertionCount,
                        CallbackCancelOperationCount = response.Workflow.CallbackCancelOperationCount,
                        CallbackGetLockStatusCount = response.Workflow.CallbackGetLockStatusCount,
                        CallbackLastUnixTimeMs = response.Workflow.CallbackLastUnixTimeMs,
                        CallbackLastKind = response.Workflow.CallbackLastKind ?? string.Empty,
                        CallbackLastMessage = response.Workflow.CallbackLastMessage ?? string.Empty,
                        CallbackLastHResultHex = response.Workflow.CallbackLastHResultHex ?? string.Empty,
                        LatestOperationId = response.Workflow.LatestOperationId ?? string.Empty,
                        LatestOperationKind = response.Workflow.LatestOperationKind ?? string.Empty,
                        LatestOperationState = response.Workflow.LatestOperationState ?? "idle",
                        LatestOperationSource = response.Workflow.LatestOperationSource ?? string.Empty,
                        LatestOperationCreatedAtUnixTimeMs = response.Workflow.LatestOperationCreatedAtUnixTimeMs,
                        LatestOperationUpdatedAtUnixTimeMs = response.Workflow.LatestOperationUpdatedAtUnixTimeMs,
                        LatestOperationRequestPointerPresent = response.Workflow.LatestOperationRequestPointerPresent,
                        LatestOperationResponsePointerPresent = response.Workflow.LatestOperationResponsePointerPresent,
                        LatestOperationCancelPointerPresent = response.Workflow.LatestOperationCancelPointerPresent,
                        LatestOperationMessage = response.Workflow.LatestOperationMessage ?? string.Empty,
                        LatestOperationHResultHex = response.Workflow.LatestOperationHResultHex ?? string.Empty,
                        ActivationCount = response.Workflow.ActivationCount,
                        LastActivationUnixTimeMs = response.Workflow.LastActivationUnixTimeMs,
                        LastActivationSource = response.Workflow.LastActivationSource ?? string.Empty,
                        StartedFromPluginActivation = response.Workflow.StartedFromPluginActivation,
                        CreateRequestCount = response.Workflow.CreateRequestCount,
                        LastCreateRequestUnixTimeMs = response.Workflow.LastCreateRequestUnixTimeMs,
                        LastCreateRequestRpId = response.Workflow.LastCreateRequestRpId ?? string.Empty,
                        LastCreateRequestUsername = response.Workflow.LastCreateRequestUsername ?? string.Empty,
                        LastCreateRequestMessage = response.Workflow.LastCreateRequestMessage ?? string.Empty,
                    },
            };
        }
        catch (OperationCanceledException)
        {
            _diagnosticsService.AddWarning("companion-ipc", "The Windows passkey companion is not running.");
            return new PasskeyCompanionProbeResult
            {
                IsReachable = false,
                Message = "The Windows passkey companion is not running.",
            };
        }
        catch (Exception exception)
        {
            _diagnosticsService.AddError(
                "companion-ipc",
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to reach the Windows passkey companion."
                    : exception.Message);
            return new PasskeyCompanionProbeResult
            {
                IsReachable = false,
                Message = string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to reach the Windows passkey companion."
                    : exception.Message,
            };
        }
#else
        return new PasskeyCompanionProbeResult
        {
            IsReachable = false,
            Message = "The Windows passkey companion is only available on Windows.",
        };
#endif
    }

    public async Task<PasskeyCompanionWorkflowSnapshot?> TryGetWorkflowAsync(CancellationToken cancellationToken = default)
    {
#if WINDOWS
        try
        {
            var response = await SendCommandAsync("get_plugin_workflow", null, cancellationToken).ConfigureAwait(false);
            return response?.Workflow == null
                ? null
                : new PasskeyCompanionWorkflowSnapshot
                {
                    WorkflowMode = response.Workflow.WorkflowMode ?? "skeleton",
                    RegistrationAttempted = response.Workflow.RegistrationAttempted,
                    RegistrationPrepared = response.Workflow.RegistrationPrepared,
                    RegistrationEnvironmentReady = response.Workflow.RegistrationEnvironmentReady,
                    RegistrationCompleted = response.Workflow.RegistrationCompleted,
                    LastRegistrationAttemptUnixTimeMs = response.Workflow.LastRegistrationAttemptUnixTimeMs,
                    RegistrationStatus = response.Workflow.RegistrationStatus ?? string.Empty,
                    LastRegistrationMessage = response.Workflow.LastRegistrationMessage ?? string.Empty,
                    LastRegistrationHResultHex = response.Workflow.LastRegistrationHResultHex ?? string.Empty,
                    AuthenticatorStateCode = response.Workflow.AuthenticatorStateCode,
                    AuthenticatorStateLabel = response.Workflow.AuthenticatorStateLabel ?? "unknown",
                    HasOperationSigningPublicKey = response.Workflow.HasOperationSigningPublicKey,
                    OperationSigningPublicKeyStoredAtUnixTimeMs = response.Workflow.OperationSigningPublicKeyStoredAtUnixTimeMs,
                    ComSkeletonReady = response.Workflow.ComSkeletonReady,
                    ComClassIdMatchesManifest = response.Workflow.ComClassIdMatchesManifest,
                    ComFactoryReady = response.Workflow.ComFactoryReady,
                    ComAuthenticatorReady = response.Workflow.ComAuthenticatorReady,
                    ComLastProbeUnixTimeMs = response.Workflow.ComLastProbeUnixTimeMs,
                    ComLastProbeMessage = response.Workflow.ComLastProbeMessage ?? string.Empty,
                    ComAuthenticatorTypeName = response.Workflow.ComAuthenticatorTypeName ?? string.Empty,
                    ComClassFactoryRegistered = response.Workflow.ComClassFactoryRegistered,
                    ComClassFactoryRegistrationCookie = response.Workflow.ComClassFactoryRegistrationCookie,
                    ComClassFactoryLastRegistrationUnixTimeMs = response.Workflow.ComClassFactoryLastRegistrationUnixTimeMs,
                    ComClassFactoryLastMessage = response.Workflow.ComClassFactoryLastMessage ?? string.Empty,
                    ComClassFactoryLastHResultHex = response.Workflow.ComClassFactoryLastHResultHex ?? string.Empty,
                    CallbackTotalCount = response.Workflow.CallbackTotalCount,
                    CallbackMakeCredentialCount = response.Workflow.CallbackMakeCredentialCount,
                    CallbackGetAssertionCount = response.Workflow.CallbackGetAssertionCount,
                    CallbackCancelOperationCount = response.Workflow.CallbackCancelOperationCount,
                    CallbackGetLockStatusCount = response.Workflow.CallbackGetLockStatusCount,
                    CallbackLastUnixTimeMs = response.Workflow.CallbackLastUnixTimeMs,
                    CallbackLastKind = response.Workflow.CallbackLastKind ?? string.Empty,
                    CallbackLastMessage = response.Workflow.CallbackLastMessage ?? string.Empty,
                    CallbackLastHResultHex = response.Workflow.CallbackLastHResultHex ?? string.Empty,
                    LatestOperationId = response.Workflow.LatestOperationId ?? string.Empty,
                    LatestOperationKind = response.Workflow.LatestOperationKind ?? string.Empty,
                    LatestOperationState = response.Workflow.LatestOperationState ?? "idle",
                    LatestOperationSource = response.Workflow.LatestOperationSource ?? string.Empty,
                    LatestOperationCreatedAtUnixTimeMs = response.Workflow.LatestOperationCreatedAtUnixTimeMs,
                    LatestOperationUpdatedAtUnixTimeMs = response.Workflow.LatestOperationUpdatedAtUnixTimeMs,
                    LatestOperationRequestPointerPresent = response.Workflow.LatestOperationRequestPointerPresent,
                    LatestOperationResponsePointerPresent = response.Workflow.LatestOperationResponsePointerPresent,
                    LatestOperationCancelPointerPresent = response.Workflow.LatestOperationCancelPointerPresent,
                    LatestOperationMessage = response.Workflow.LatestOperationMessage ?? string.Empty,
                    LatestOperationHResultHex = response.Workflow.LatestOperationHResultHex ?? string.Empty,
                    ActivationCount = response.Workflow.ActivationCount,
                    LastActivationUnixTimeMs = response.Workflow.LastActivationUnixTimeMs,
                    LastActivationSource = response.Workflow.LastActivationSource ?? string.Empty,
                    StartedFromPluginActivation = response.Workflow.StartedFromPluginActivation,
                    CreateRequestCount = response.Workflow.CreateRequestCount,
                    LastCreateRequestUnixTimeMs = response.Workflow.LastCreateRequestUnixTimeMs,
                    LastCreateRequestRpId = response.Workflow.LastCreateRequestRpId ?? string.Empty,
                    LastCreateRequestUsername = response.Workflow.LastCreateRequestUsername ?? string.Empty,
                    LastCreateRequestMessage = response.Workflow.LastCreateRequestMessage ?? string.Empty,
                };
        }
        catch
        {
            return null;
        }
#else
        return null;
#endif
    }

    public async Task<HostOperationResult> TryPreparePluginRegistrationAsync(CancellationToken cancellationToken = default)
    {
#if WINDOWS
        try
        {
            var response = await SendCommandAsync("prepare_plugin_registration", null, cancellationToken).ConfigureAwait(false);
            var message = response?.Message ?? "The Windows passkey companion did not return a valid registration response.";
            if (response?.Success == true)
            {
                _diagnosticsService.AddInfo("companion-plugin", message);
            }
            else
            {
                _diagnosticsService.AddWarning("companion-plugin", message);
            }

            return new HostOperationResult
            {
                Success = response?.Success == true,
                Message = message,
            };
        }
        catch (OperationCanceledException)
        {
            _diagnosticsService.AddWarning("companion-plugin", "The Windows passkey companion is not running.");
            return new HostOperationResult
            {
                Success = false,
                Message = "The Windows passkey companion is not running.",
            };
        }
        catch (Exception exception)
        {
            var message = string.IsNullOrWhiteSpace(exception.Message)
                ? "Unable to prepare plugin registration on the Windows passkey companion."
                : exception.Message;
            _diagnosticsService.AddError("companion-plugin", message);
            return new HostOperationResult
            {
                Success = false,
                Message = message,
            };
        }
#else
        return new HostOperationResult
        {
            Success = false,
            Message = "The Windows passkey companion is only available on Windows.",
        };
#endif
    }

    public async Task<HostOperationResult> TryCreatePasskeySkeletonAsync(
        PasskeyCreateRequest request,
        CancellationToken cancellationToken = default)
    {
#if WINDOWS
        try
        {
            var response = await SendCommandAsync(
                "create_passkey_skeleton",
                new
                {
                    RpId = request?.RpId ?? string.Empty,
                    Username = request?.Username ?? string.Empty,
                    DisplayName = request?.DisplayName ?? string.Empty,
                    UserHandle = request?.UserHandle ?? string.Empty,
                },
                cancellationToken).ConfigureAwait(false);

            var message = response?.Message ?? "The Windows passkey companion did not return a valid create-passkey response.";
            if (response?.Success == true)
            {
                _diagnosticsService.AddInfo("companion-plugin", message);
            }
            else
            {
                _diagnosticsService.AddWarning("companion-plugin", message);
            }

            return new HostOperationResult
            {
                Success = response?.Success == true,
                Message = message,
            };
        }
        catch (OperationCanceledException)
        {
            _diagnosticsService.AddWarning("companion-plugin", "The Windows passkey companion is not running.");
            return new HostOperationResult
            {
                Success = false,
                Message = "The Windows passkey companion is not running.",
            };
        }
        catch (Exception exception)
        {
            var message = string.IsNullOrWhiteSpace(exception.Message)
                ? "Unable to send the create-passkey skeleton request to the Windows passkey companion."
                : exception.Message;
            _diagnosticsService.AddError("companion-plugin", message);
            return new HostOperationResult
            {
                Success = false,
                Message = message,
            };
        }
#else
        return new HostOperationResult
        {
            Success = false,
            Message = "The Windows passkey companion is only available on Windows.",
        };
#endif
    }

    public async Task<HostOperationResult> TryResolveLatestOperationAsync(
        string resolution,
        string message = "",
        CancellationToken cancellationToken = default)
    {
#if WINDOWS
        try
        {
            var response = await SendCommandAsync(
                "resolve_latest_operation",
                new
                {
                    Resolution = resolution ?? string.Empty,
                    Message = message ?? string.Empty,
                },
                cancellationToken).ConfigureAwait(false);

            var resolvedMessage = response?.Message ?? "The Windows passkey companion did not return a valid operation-resolution response.";
            if (response?.Success == true)
            {
                _diagnosticsService.AddInfo("companion-plugin", resolvedMessage);
            }
            else
            {
                _diagnosticsService.AddWarning("companion-plugin", resolvedMessage);
            }

            return new HostOperationResult
            {
                Success = response?.Success == true,
                Message = resolvedMessage,
            };
        }
        catch (OperationCanceledException)
        {
            _diagnosticsService.AddWarning("companion-plugin", "The Windows passkey companion is not running.");
            return new HostOperationResult
            {
                Success = false,
                Message = "The Windows passkey companion is not running.",
            };
        }
        catch (Exception exception)
        {
            var resolvedMessage = string.IsNullOrWhiteSpace(exception.Message)
                ? "Unable to update the latest plugin operation on the Windows passkey companion."
                : exception.Message;
            _diagnosticsService.AddError("companion-plugin", resolvedMessage);
            return new HostOperationResult
            {
                Success = false,
                Message = resolvedMessage,
            };
        }
#else
        return new HostOperationResult
        {
            Success = false,
            Message = "The Windows passkey companion is only available on Windows.",
        };
#endif
    }

    public async Task<HostOperationResult> TryActivateAsync(CancellationToken cancellationToken = default)
    {
#if WINDOWS
        try
        {
            var response = await SendCommandAsync("activate", null, cancellationToken).ConfigureAwait(false);
            if (response?.Success == true)
            {
                _diagnosticsService.AddInfo("companion-ipc", response.Message ?? "The Windows passkey companion was activated.");
            }
            else
            {
                _diagnosticsService.AddWarning(
                    "companion-ipc",
                    response?.Message ?? "The Windows passkey companion did not return a valid activation response.");
            }
            return new HostOperationResult
            {
                Success = response?.Success == true,
                Message = response?.Message ?? "The Windows passkey companion did not return a valid activation response.",
            };
        }
        catch (OperationCanceledException)
        {
            _diagnosticsService.AddWarning("companion-ipc", "The Windows passkey companion is not running.");
            return new HostOperationResult
            {
                Success = false,
                Message = "The Windows passkey companion is not running.",
            };
        }
        catch (Exception exception)
        {
            _diagnosticsService.AddError(
                "companion-ipc",
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to activate the Windows passkey companion."
                    : exception.Message);
            return new HostOperationResult
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to activate the Windows passkey companion."
                    : exception.Message,
            };
        }
#else
        return new HostOperationResult
        {
            Success = false,
            Message = "The Windows passkey companion is only available on Windows.",
        };
#endif
    }

    public async Task<HostOperationResult> TryShutdownAsync(CancellationToken cancellationToken = default)
    {
#if WINDOWS
        try
        {
            var response = await SendCommandAsync("shutdown", null, cancellationToken).ConfigureAwait(false);
            if (response?.Success == true)
            {
                _diagnosticsService.AddInfo(
                    "companion-ipc",
                    response.Message ?? "The Windows passkey companion shutdown was requested.");
            }
            else
            {
                _diagnosticsService.AddWarning(
                    "companion-ipc",
                    response?.Message ?? "The Windows passkey companion did not return a valid shutdown response.");
            }

            return new HostOperationResult
            {
                Success = response?.Success == true,
                Message = response?.Message ?? "The Windows passkey companion did not return a valid shutdown response.",
            };
        }
        catch (OperationCanceledException)
        {
            _diagnosticsService.AddWarning("companion-ipc", "The Windows passkey companion is not running.");
            return new HostOperationResult
            {
                Success = false,
                Message = "The Windows passkey companion is not running.",
            };
        }
        catch (Exception exception)
        {
            _diagnosticsService.AddError(
                "companion-ipc",
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to shut down the Windows passkey companion."
                    : exception.Message);
            return new HostOperationResult
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to shut down the Windows passkey companion."
                    : exception.Message,
            };
        }
#else
        return new HostOperationResult
        {
            Success = false,
            Message = "The Windows passkey companion is only available on Windows.",
        };
#endif
    }

#if WINDOWS
    private async Task<CompanionPipeResponse?> SendCommandAsync(
        string action,
        object? payload,
        CancellationToken cancellationToken)
    {
        using var client = new NamedPipeClientStream(
            ".",
            PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ConnectTimeoutMs);

        await client.ConnectAsync(timeoutCts.Token).ConfigureAwait(false);

        using var reader = new StreamReader(client, Encoding.UTF8, false, 1024, leaveOpen: true);
        using var writer = new StreamWriter(client, new UTF8Encoding(false), 1024, leaveOpen: true)
        {
            AutoFlush = true,
        };

        var request = JsonSerializer.Serialize(
            new
            {
                action,
                payloadJson = payload == null
                    ? string.Empty
                    : JsonSerializer.Serialize(payload, _jsonOptions),
            },
            _jsonOptions);
        await writer.WriteLineAsync(request).ConfigureAwait(false);

        var responseLine = await reader.ReadLineAsync(timeoutCts.Token).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(responseLine))
        {
            return null;
        }

        return JsonSerializer.Deserialize<CompanionPipeResponse>(responseLine, _jsonOptions);
    }
#endif

    private sealed class CompanionPipeResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public CompanionPipeSnapshot? Snapshot { get; set; }

        public CompanionPipeWorkflow? Workflow { get; set; }
    }

    private sealed class CompanionPipeSnapshot
    {
        public long CheckedAtUnixTimeMs { get; set; }

        public int BuildNumber { get; set; }

        public int Ubr { get; set; }

        public bool MeetsPluginBuildRequirement { get; set; }

        public bool WebAuthnLibraryAvailable { get; set; }

        public bool PluginExportsAvailable { get; set; }

        public bool IsPackagedProcess { get; set; }

        public string? StatusSummary { get; set; }

        public string? DetailMessage { get; set; }
    }

    private sealed class CompanionPipeWorkflow
    {
        public string? WorkflowMode { get; set; }

        public bool RegistrationAttempted { get; set; }

        public bool RegistrationPrepared { get; set; }

        public bool RegistrationEnvironmentReady { get; set; }

        public bool RegistrationCompleted { get; set; }

        public long LastRegistrationAttemptUnixTimeMs { get; set; }

        public string? RegistrationStatus { get; set; }

        public string? LastRegistrationMessage { get; set; }

        public string? LastRegistrationHResultHex { get; set; }

        public int AuthenticatorStateCode { get; set; }

        public string? AuthenticatorStateLabel { get; set; }

        public bool HasOperationSigningPublicKey { get; set; }

        public long OperationSigningPublicKeyStoredAtUnixTimeMs { get; set; }

        public bool ComSkeletonReady { get; set; }

        public bool ComClassIdMatchesManifest { get; set; }

        public bool ComFactoryReady { get; set; }

        public bool ComAuthenticatorReady { get; set; }

        public long ComLastProbeUnixTimeMs { get; set; }

        public string? ComLastProbeMessage { get; set; }

        public string? ComAuthenticatorTypeName { get; set; }

        public bool ComClassFactoryRegistered { get; set; }

        public uint ComClassFactoryRegistrationCookie { get; set; }

        public long ComClassFactoryLastRegistrationUnixTimeMs { get; set; }

        public string? ComClassFactoryLastMessage { get; set; }

        public string? ComClassFactoryLastHResultHex { get; set; }

        public int CallbackTotalCount { get; set; }

        public int CallbackMakeCredentialCount { get; set; }

        public int CallbackGetAssertionCount { get; set; }

        public int CallbackCancelOperationCount { get; set; }

        public int CallbackGetLockStatusCount { get; set; }

        public long CallbackLastUnixTimeMs { get; set; }

        public string? CallbackLastKind { get; set; }

        public string? CallbackLastMessage { get; set; }

        public string? CallbackLastHResultHex { get; set; }

        public string? LatestOperationId { get; set; }

        public string? LatestOperationKind { get; set; }

        public string? LatestOperationState { get; set; }

        public string? LatestOperationSource { get; set; }

        public long LatestOperationCreatedAtUnixTimeMs { get; set; }

        public long LatestOperationUpdatedAtUnixTimeMs { get; set; }

        public bool LatestOperationRequestPointerPresent { get; set; }

        public bool LatestOperationResponsePointerPresent { get; set; }

        public bool LatestOperationCancelPointerPresent { get; set; }

        public string? LatestOperationMessage { get; set; }

        public string? LatestOperationHResultHex { get; set; }

        public int ActivationCount { get; set; }

        public long LastActivationUnixTimeMs { get; set; }

        public string? LastActivationSource { get; set; }

        public bool StartedFromPluginActivation { get; set; }

        public int CreateRequestCount { get; set; }

        public long LastCreateRequestUnixTimeMs { get; set; }

        public string? LastCreateRequestRpId { get; set; }

        public string? LastCreateRequestUsername { get; set; }

        public string? LastCreateRequestMessage { get; set; }
    }
}
