using PasswordVault.PasskeyCompanion.Models;
using System.IO;
using System.Text.Json;

namespace PasswordVault.PasskeyCompanion.Services;

public sealed class PluginRegistrationService : IDisposable
{
    private const string RegistrationRecordFileName = "plugin-registration.json";

    private readonly CompanionStatusService _statusService;
    private readonly PluginActivationService _pluginActivationService;
    private readonly PluginCallbackTraceService _callbackTraceService;
    private readonly PluginOperationStateService _operationStateService;
    private readonly PluginComServerSkeletonService _comServerSkeletonService;
    private readonly PluginClassFactoryRegistrationService _classFactoryRegistrationService;
    private readonly object _syncRoot = new();
    private readonly string _registrationRecordPath;

    private bool _registrationAttempted;
    private bool _registrationPrepared;
    private DateTimeOffset? _lastRegistrationAttemptAt;
    private string _registrationStatus = "Plugin registration has not been attempted yet.";
    private string _lastRegistrationMessage = "No registration attempt has been recorded yet.";
    private int _lastRegistrationHResult;
    private PluginRegistrationRecord _registrationRecord;

    private int _createRequestCount;
    private DateTimeOffset? _lastCreateRequestAt;
    private string _lastCreateRequestRpId = string.Empty;
    private string _lastCreateRequestUsername = string.Empty;
    private string _lastCreateRequestMessage = "No create-passkey request has been captured yet.";

    public PluginRegistrationService(
        CompanionStatusService statusService,
        PluginActivationService pluginActivationService)
    {
        _statusService = statusService;
        _pluginActivationService = pluginActivationService;
        _callbackTraceService = new PluginCallbackTraceService();
        _operationStateService = new PluginOperationStateService();
        _comServerSkeletonService = new PluginComServerSkeletonService(
            pluginActivationService,
            _callbackTraceService,
            _operationStateService);
        _classFactoryRegistrationService = new PluginClassFactoryRegistrationService(
            pluginActivationService,
            _callbackTraceService,
            _operationStateService);
        _registrationRecordPath = BuildRegistrationRecordPath();
        _registrationRecord = LoadRegistrationRecord();

        if (_registrationRecord.LastRegistrationAttemptUnixTimeMs > 0)
        {
            _registrationAttempted = true;
            _lastRegistrationAttemptAt =
                DateTimeOffset.FromUnixTimeMilliseconds(_registrationRecord.LastRegistrationAttemptUnixTimeMs);
            _lastRegistrationMessage = _registrationRecord.LastRegistrationMessage;
            _lastRegistrationHResult = _registrationRecord.LastRegistrationHResult;
            _registrationPrepared = _registrationRecord.RegistrationCompleted;
            _registrationStatus = BuildRegistrationStatusFromRecord(_registrationRecord);
        }
    }

    public CompanionPluginWorkflowSnapshot GetWorkflowSnapshot()
    {
        var status = _statusService.Probe();
        var registrationRecord = RefreshRegistrationRecordSnapshot(status);
        var activation = _pluginActivationService.GetSnapshot();
        var comSnapshot = _comServerSkeletonService.Probe();
        var classFactorySnapshot = _classFactoryRegistrationService.GetSnapshot();
        var callbackSnapshot = _callbackTraceService.GetSnapshot();
        var operationSnapshot = _operationStateService.GetSnapshot();

        lock (_syncRoot)
        {
            return new CompanionPluginWorkflowSnapshot
            {
                WorkflowMode = "native-registration",
                RegistrationAttempted = _registrationAttempted,
                RegistrationPrepared = _registrationPrepared,
                RegistrationEnvironmentReady = status.IsCompanionReady,
                RegistrationCompleted = registrationRecord.RegistrationCompleted,
                LastRegistrationAttemptUnixTimeMs = _lastRegistrationAttemptAt?.ToUnixTimeMilliseconds() ?? 0,
                RegistrationStatus = _registrationStatus,
                LastRegistrationMessage = _lastRegistrationMessage,
                LastRegistrationHResultHex = _lastRegistrationHResult == 0
                    ? string.Empty
                    : WindowsWebAuthnPluginNative.ToHex(_lastRegistrationHResult),
                AuthenticatorStateCode = registrationRecord.AuthenticatorStateCode,
                AuthenticatorStateLabel = registrationRecord.AuthenticatorStateLabel,
                HasOperationSigningPublicKey =
                    !string.IsNullOrWhiteSpace(registrationRecord.OperationSigningPublicKeyBase64),
                OperationSigningPublicKeyStoredAtUnixTimeMs =
                    registrationRecord.OperationSigningPublicKeyStoredAtUnixTimeMs,
                ComSkeletonReady = comSnapshot.ComSkeletonReady,
                ComClassIdMatchesManifest = comSnapshot.ClassIdMatchesManifest,
                ComFactoryReady = comSnapshot.FactoryReady,
                ComAuthenticatorReady = comSnapshot.AuthenticatorReady,
                ComLastProbeUnixTimeMs = comSnapshot.LastProbeUnixTimeMs,
                ComLastProbeMessage = comSnapshot.LastProbeMessage,
                ComAuthenticatorTypeName = comSnapshot.AuthenticatorTypeName,
                ComClassFactoryRegistered = classFactorySnapshot.IsRegistered,
                ComClassFactoryRegistrationCookie = classFactorySnapshot.RegistrationCookie,
                ComClassFactoryLastRegistrationUnixTimeMs = classFactorySnapshot.LastRegistrationUnixTimeMs,
                ComClassFactoryLastMessage = classFactorySnapshot.LastMessage,
                ComClassFactoryLastHResultHex = classFactorySnapshot.LastHResult == 0
                    ? string.Empty
                    : WindowsWebAuthnPluginNative.ToHex(classFactorySnapshot.LastHResult),
                CallbackTotalCount = callbackSnapshot.TotalCount,
                CallbackMakeCredentialCount = callbackSnapshot.MakeCredentialCount,
                CallbackGetAssertionCount = callbackSnapshot.GetAssertionCount,
                CallbackCancelOperationCount = callbackSnapshot.CancelOperationCount,
                CallbackGetLockStatusCount = callbackSnapshot.GetLockStatusCount,
                CallbackLastUnixTimeMs = callbackSnapshot.LastCallbackUnixTimeMs,
                CallbackLastKind = callbackSnapshot.LastCallbackKind,
                CallbackLastMessage = callbackSnapshot.LastCallbackMessage,
                CallbackLastHResultHex = callbackSnapshot.LastCallbackHResultHex,
                LatestOperationId = operationSnapshot.OperationId,
                LatestOperationKind = operationSnapshot.Kind,
                LatestOperationState = operationSnapshot.State,
                LatestOperationSource = operationSnapshot.Source,
                LatestOperationCreatedAtUnixTimeMs = operationSnapshot.CreatedAtUnixTimeMs,
                LatestOperationUpdatedAtUnixTimeMs = operationSnapshot.UpdatedAtUnixTimeMs,
                LatestOperationRequestPointerPresent = operationSnapshot.RequestPointerPresent,
                LatestOperationResponsePointerPresent = operationSnapshot.ResponsePointerPresent,
                LatestOperationCancelPointerPresent = operationSnapshot.CancelPointerPresent,
                LatestOperationMessage = operationSnapshot.Message,
                LatestOperationHResultHex = operationSnapshot.HResultHex,
                ActivationCount = activation.ActivationCount,
                LastActivationUnixTimeMs = activation.LastActivationUnixTimeMs,
                LastActivationSource = activation.LastActivationSource,
                StartedFromPluginActivation = activation.StartedFromPluginActivation,
                CreateRequestCount = _createRequestCount,
                LastCreateRequestUnixTimeMs = _lastCreateRequestAt?.ToUnixTimeMilliseconds() ?? 0,
                LastCreateRequestRpId = _lastCreateRequestRpId,
                LastCreateRequestUsername = _lastCreateRequestUsername,
                LastCreateRequestMessage = _lastCreateRequestMessage,
            };
        }
    }

    public CompanionIpcResponse HandlePluginActivation(string source, bool isStartupActivation = false)
    {
        _pluginActivationService.RecordPluginActivation(source, isStartupActivation);
        var classFactorySnapshot = _classFactoryRegistrationService.EnsureRegistered();

        if (!classFactorySnapshot.IsRegistered)
        {
            return BuildResponse(
                false,
                $"The plugin activation path reached the companion, but the local COM class factory could not be registered. {classFactorySnapshot.LastMessage}");
        }

        var status = _statusService.Probe();
        if (!status.IsCompanionReady)
        {
            return BuildResponse(
                true,
                "The plugin activation path reached the companion, but packaged/plugin prerequisites are still missing.");
        }

        var response = PrepareRegistration();
        response.Message = response.Success
            ? $"The plugin activation path reached the companion. {response.Message}"
            : $"The plugin activation path reached the companion, but registration is still blocked. {response.Message}";

        return response;
    }

    public void Dispose()
    {
        _classFactoryRegistrationService.Dispose();
    }

    public CompanionIpcResponse PrepareRegistration()
    {
        var status = _statusService.Probe();
        var attemptedAt = DateTimeOffset.UtcNow;

        lock (_syncRoot)
        {
            _registrationAttempted = true;
            _lastRegistrationAttemptAt = attemptedAt;
        }

        if (!status.IsCompanionReady)
        {
            return CompleteRegistrationAttempt(
                success: false,
                attemptedAt,
                hResult: 0,
                updateRecord: record =>
                {
                    record.RegistrationCompleted = false;
                },
                statusMessage:
                    "Plugin registration is blocked because the packaged companion prerequisites are not fully satisfied yet.",
                detailMessage:
                    "The companion attempted Windows plugin registration, but the required packaged/plugin prerequisites are still missing.");
        }

        var existingState = WindowsWebAuthnPluginNative.TryGetAuthenticatorState();
        if (existingState.IsRegistered)
        {
            var signingKeyResult = EnsureOperationSigningPublicKey();
            var alreadyRegisteredMessage = existingState.StateLabel == "enabled"
                ? "The Windows plugin authenticator is already registered and enabled."
                : "The Windows plugin authenticator is already registered, but Windows still reports it as disabled.";

            return CompleteRegistrationAttempt(
                success: true,
                attemptedAt,
                hResult: existingState.HResult,
                updateRecord: record =>
                {
                    record.RegistrationCompleted = true;
                    record.AuthenticatorStateCode = existingState.RawState;
                    record.AuthenticatorStateLabel = existingState.StateLabel;
                    if (signingKeyResult.Success && signingKeyResult.OperationSigningPublicKey is { Length: > 0 } keyBytes)
                    {
                        record.OperationSigningPublicKeyBase64 = Convert.ToBase64String(keyBytes);
                        record.OperationSigningPublicKeyStoredAtUnixTimeMs = attemptedAt.ToUnixTimeMilliseconds();
                    }
                },
                statusMessage: alreadyRegisteredMessage,
                detailMessage: signingKeyResult.Success
                    ? alreadyRegisteredMessage
                    : $"{alreadyRegisteredMessage} {signingKeyResult.Message}");
        }

        var addResult = WindowsWebAuthnPluginNative.TryAddAuthenticator();
        if (!addResult.Success)
        {
            var refreshedState = WindowsWebAuthnPluginNative.TryGetAuthenticatorState();
            return CompleteRegistrationAttempt(
                success: false,
                attemptedAt,
                addResult.HResult,
                updateRecord: record =>
                {
                    record.RegistrationCompleted = refreshedState.IsRegistered;
                    record.AuthenticatorStateCode = refreshedState.RawState;
                    record.AuthenticatorStateLabel = refreshedState.StateLabel;
                },
                statusMessage:
                    "Windows rejected the plugin authenticator registration request.",
                detailMessage: addResult.Message);
        }

        var stateAfterRegistration = WindowsWebAuthnPluginNative.TryGetAuthenticatorState();
        var operationSigningKeyBytes = addResult.OperationSigningPublicKey;
        if (operationSigningKeyBytes is not { Length: > 0 })
        {
            var signingKeyResult = EnsureOperationSigningPublicKey();
            if (signingKeyResult.Success)
            {
                operationSigningKeyBytes = signingKeyResult.OperationSigningPublicKey;
            }
        }

        var successMessage = stateAfterRegistration.StateLabel == "enabled"
            ? "The Windows plugin authenticator was registered and is already enabled."
            : "The Windows plugin authenticator was registered. Windows still reports the authenticator as disabled, so the user may still need to enable it in Settings.";

        return CompleteRegistrationAttempt(
            success: true,
            attemptedAt,
            addResult.HResult,
            updateRecord: record =>
            {
                record.RegistrationCompleted = true;
                record.AuthenticatorStateCode = stateAfterRegistration.RawState;
                record.AuthenticatorStateLabel = stateAfterRegistration.StateLabel;

                if (operationSigningKeyBytes is { Length: > 0 })
                {
                    record.OperationSigningPublicKeyBase64 = Convert.ToBase64String(operationSigningKeyBytes);
                    record.OperationSigningPublicKeyStoredAtUnixTimeMs = attemptedAt.ToUnixTimeMilliseconds();
                }
            },
            statusMessage: successMessage,
            detailMessage: successMessage);
    }

    public CompanionIpcResponse CaptureCreatePasskeyRequest(CompanionCreatePasskeyRequest? request)
    {
        var normalizedRequest = NormalizeRequest(request);
        var status = _statusService.Probe();
        var requestedAt = DateTimeOffset.UtcNow;
        var registrationRecord = RefreshRegistrationRecordSnapshot(status);

        lock (_syncRoot)
        {
            _createRequestCount += 1;
            _lastCreateRequestAt = requestedAt;
            _lastCreateRequestRpId = normalizedRequest.RpId;
            _lastCreateRequestUsername = normalizedRequest.Username;

            if (string.IsNullOrWhiteSpace(normalizedRequest.RpId) ||
                string.IsNullOrWhiteSpace(normalizedRequest.Username))
            {
                _lastCreateRequestMessage =
                    "The create-passkey request was rejected because rpId or username is missing.";
                return BuildResponse(false, _lastCreateRequestMessage);
            }

            if (!status.IsCompanionReady)
            {
                _lastCreateRequestMessage =
                    $"The create-passkey request for {normalizedRequest.RpId} was captured, but the packaged companion is not ready for Windows plugin callbacks yet.";
                return BuildResponse(false, _lastCreateRequestMessage);
            }

            if (!registrationRecord.RegistrationCompleted)
            {
                _lastCreateRequestMessage =
                    $"The create-passkey request for {normalizedRequest.RpId} was captured, but the Windows plugin authenticator is not registered yet.";
                return BuildResponse(false, _lastCreateRequestMessage);
            }

            _lastCreateRequestMessage =
                $"The create-passkey request for {normalizedRequest.RpId} was captured. The next step is wiring it into the Windows plugin callback path.";
            return BuildResponse(true, _lastCreateRequestMessage);
        }
    }

    public CompanionIpcResponse ResolveLatestOperation(CompanionResolveOperationRequest? request)
    {
        var resolution = request?.Resolution?.Trim().ToLowerInvariant() ?? "approved";
        var snapshot = _operationStateService.ResolveLatestOperation(
            resolution,
            request?.Message?.Trim() ?? string.Empty);

        var message = snapshot.State switch
        {
            "approved-skeleton" =>
                "The latest plugin operation was marked as approved in the current skeleton workflow.",
            "rejected-skeleton" =>
                "The latest plugin operation was marked as rejected in the current skeleton workflow.",
            "idle" =>
                "The tracked plugin operation was cleared from the current companion session.",
            _ => snapshot.Message,
        };

        return BuildResponse(success: true, message);
    }

    private CompanionIpcResponse CompleteRegistrationAttempt(
        bool success,
        DateTimeOffset attemptedAt,
        int hResult,
        Action<PluginRegistrationRecord> updateRecord,
        string statusMessage,
        string detailMessage)
    {
        lock (_syncRoot)
        {
            _registrationPrepared = success;
            _lastRegistrationHResult = hResult;
            _registrationStatus = statusMessage;
            _lastRegistrationMessage = detailMessage;

            _registrationRecord.LastRegistrationAttemptUnixTimeMs = attemptedAt.ToUnixTimeMilliseconds();
            _registrationRecord.LastRegistrationHResult = hResult;
            _registrationRecord.LastRegistrationMessage = detailMessage;

            updateRecord(_registrationRecord);
            SaveRegistrationRecord(_registrationRecord);

            return BuildResponse(success, detailMessage);
        }
    }

    private PluginRegistrationRecord RefreshRegistrationRecordSnapshot(CompanionStatusSnapshot status)
    {
        lock (_syncRoot)
        {
            if (!status.IsCompanionReady)
            {
                return CloneRegistrationRecord(_registrationRecord);
            }

            var nativeState = WindowsWebAuthnPluginNative.TryGetAuthenticatorState();
            _registrationRecord.RegistrationCompleted = nativeState.IsRegistered;
            _registrationRecord.AuthenticatorStateCode = nativeState.RawState;
            _registrationRecord.AuthenticatorStateLabel = nativeState.StateLabel;

            if (_registrationRecord.RegistrationCompleted &&
                string.IsNullOrWhiteSpace(_registrationRecord.OperationSigningPublicKeyBase64))
            {
                var signingKeyResult = EnsureOperationSigningPublicKey();
                if (signingKeyResult.Success && signingKeyResult.OperationSigningPublicKey is { Length: > 0 } keyBytes)
                {
                    _registrationRecord.OperationSigningPublicKeyBase64 = Convert.ToBase64String(keyBytes);
                    _registrationRecord.OperationSigningPublicKeyStoredAtUnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }
            }

            if (!_registrationAttempted)
            {
                _registrationStatus = nativeState.IsRegistered
                    ? nativeState.StateLabel == "enabled"
                        ? "The Windows plugin authenticator is registered and enabled."
                        : "The Windows plugin authenticator is registered, but Windows still reports it as disabled."
                    : "Plugin registration has not been attempted yet.";
            }

            SaveRegistrationRecord(_registrationRecord);
            return CloneRegistrationRecord(_registrationRecord);
        }
    }

    private PluginOperationSigningKeyResult EnsureOperationSigningPublicKey()
    {
        return WindowsWebAuthnPluginNative.TryGetOperationSigningPublicKey();
    }

    private CompanionIpcResponse BuildResponse(bool success, string message)
    {
        return new CompanionIpcResponse
        {
            Success = success,
            Message = message,
            Snapshot = null,
            Workflow = GetWorkflowSnapshot(),
        };
    }

    private static CompanionCreatePasskeyRequest NormalizeRequest(CompanionCreatePasskeyRequest? request)
    {
        return new CompanionCreatePasskeyRequest
        {
            RpId = request?.RpId?.Trim() ?? string.Empty,
            Username = request?.Username?.Trim() ?? string.Empty,
            DisplayName = request?.DisplayName?.Trim() ?? string.Empty,
            UserHandle = request?.UserHandle?.Trim() ?? string.Empty,
        };
    }

    private static string BuildRegistrationStatusFromRecord(PluginRegistrationRecord record)
    {
        if (record.RegistrationCompleted)
        {
            return record.AuthenticatorStateLabel == "enabled"
                ? "The Windows plugin authenticator is registered and enabled."
                : "The Windows plugin authenticator is registered, but Windows still reports it as disabled.";
        }

        return "Plugin registration has not been attempted yet.";
    }

    private string BuildRegistrationRecordPath()
    {
        var directoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PasswordVault",
            "PasskeyCompanion");

        Directory.CreateDirectory(directoryPath);
        return Path.Combine(directoryPath, RegistrationRecordFileName);
    }

    private PluginRegistrationRecord LoadRegistrationRecord()
    {
        try
        {
            if (!File.Exists(_registrationRecordPath))
            {
                return new PluginRegistrationRecord();
            }

            var json = File.ReadAllText(_registrationRecordPath);
            return JsonSerializer.Deserialize<PluginRegistrationRecord>(json) ?? new PluginRegistrationRecord();
        }
        catch
        {
            return new PluginRegistrationRecord();
        }
    }

    private void SaveRegistrationRecord(PluginRegistrationRecord record)
    {
        try
        {
            var json = JsonSerializer.Serialize(record, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true,
            });
            File.WriteAllText(_registrationRecordPath, json);
        }
        catch
        {
        }
    }

    private static PluginRegistrationRecord CloneRegistrationRecord(PluginRegistrationRecord record)
    {
        return new PluginRegistrationRecord
        {
            LastRegistrationAttemptUnixTimeMs = record.LastRegistrationAttemptUnixTimeMs,
            LastRegistrationMessage = record.LastRegistrationMessage,
            LastRegistrationHResult = record.LastRegistrationHResult,
            RegistrationCompleted = record.RegistrationCompleted,
            AuthenticatorStateCode = record.AuthenticatorStateCode,
            AuthenticatorStateLabel = record.AuthenticatorStateLabel,
            OperationSigningPublicKeyBase64 = record.OperationSigningPublicKeyBase64,
            OperationSigningPublicKeyStoredAtUnixTimeMs = record.OperationSigningPublicKeyStoredAtUnixTimeMs,
        };
    }
}
