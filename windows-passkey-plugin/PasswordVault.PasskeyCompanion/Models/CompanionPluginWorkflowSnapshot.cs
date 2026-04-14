namespace PasswordVault.PasskeyCompanion.Models;

public sealed class CompanionPluginWorkflowSnapshot
{
    public string WorkflowMode { get; init; } = "native-registration";

    public bool RegistrationAttempted { get; init; }

    public bool RegistrationPrepared { get; init; }

    public bool RegistrationEnvironmentReady { get; init; }

    public bool RegistrationCompleted { get; init; }

    public long LastRegistrationAttemptUnixTimeMs { get; init; }

    public string RegistrationStatus { get; init; } = string.Empty;

    public string LastRegistrationMessage { get; init; } = string.Empty;

    public string LastRegistrationHResultHex { get; init; } = string.Empty;

    public int AuthenticatorStateCode { get; init; }

    public string AuthenticatorStateLabel { get; init; } = "unknown";

    public bool HasOperationSigningPublicKey { get; init; }

    public long OperationSigningPublicKeyStoredAtUnixTimeMs { get; init; }

    public bool ComSkeletonReady { get; init; }

    public bool ComClassIdMatchesManifest { get; init; }

    public bool ComFactoryReady { get; init; }

    public bool ComAuthenticatorReady { get; init; }

    public long ComLastProbeUnixTimeMs { get; init; }

    public string ComLastProbeMessage { get; init; } = string.Empty;

    public string ComAuthenticatorTypeName { get; init; } = string.Empty;

    public bool ComClassFactoryRegistered { get; init; }

    public uint ComClassFactoryRegistrationCookie { get; init; }

    public long ComClassFactoryLastRegistrationUnixTimeMs { get; init; }

    public string ComClassFactoryLastMessage { get; init; } = string.Empty;

    public string ComClassFactoryLastHResultHex { get; init; } = string.Empty;

    public int CallbackTotalCount { get; init; }

    public int CallbackMakeCredentialCount { get; init; }

    public int CallbackGetAssertionCount { get; init; }

    public int CallbackCancelOperationCount { get; init; }

    public int CallbackGetLockStatusCount { get; init; }

    public long CallbackLastUnixTimeMs { get; init; }

    public string CallbackLastKind { get; init; } = string.Empty;

    public string CallbackLastMessage { get; init; } = string.Empty;

    public string CallbackLastHResultHex { get; init; } = string.Empty;

    public string LatestOperationId { get; init; } = string.Empty;

    public string LatestOperationKind { get; init; } = string.Empty;

    public string LatestOperationState { get; init; } = "idle";

    public string LatestOperationSource { get; init; } = string.Empty;

    public long LatestOperationCreatedAtUnixTimeMs { get; init; }

    public long LatestOperationUpdatedAtUnixTimeMs { get; init; }

    public bool LatestOperationRequestPointerPresent { get; init; }

    public bool LatestOperationResponsePointerPresent { get; init; }

    public bool LatestOperationCancelPointerPresent { get; init; }

    public string LatestOperationMessage { get; init; } = string.Empty;

    public string LatestOperationHResultHex { get; init; } = string.Empty;

    public int ActivationCount { get; init; }

    public long LastActivationUnixTimeMs { get; init; }

    public string LastActivationSource { get; init; } = string.Empty;

    public bool StartedFromPluginActivation { get; init; }

    public int CreateRequestCount { get; init; }

    public long LastCreateRequestUnixTimeMs { get; init; }

    public string LastCreateRequestRpId { get; init; } = string.Empty;

    public string LastCreateRequestUsername { get; init; } = string.Empty;

    public string LastCreateRequestMessage { get; init; } = string.Empty;
}
