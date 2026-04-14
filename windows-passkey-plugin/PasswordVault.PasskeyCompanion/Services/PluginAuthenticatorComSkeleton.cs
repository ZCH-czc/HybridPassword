using PasswordVault.PasskeyCompanion.Models;
using System.Runtime.InteropServices;

namespace PasswordVault.PasskeyCompanion.Services;

[ComVisible(true)]
[Guid("A9E9A2D4-9A03-45A7-A8E3-7D521A3670F6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IPasswordVaultPluginAuthenticatorSkeleton
{
    [PreserveSig]
    int MakeCredential(IntPtr pluginOperationRequest, IntPtr pluginOperationResponse);

    [PreserveSig]
    int GetAssertion(IntPtr pluginOperationRequest, IntPtr pluginOperationResponse);

    [PreserveSig]
    int CancelOperation(IntPtr cancelOperationRequest);

    [PreserveSig]
    int GetLockStatus(out int pluginLockStatus);
}

internal interface IPasswordVaultPluginAuthenticatorFactorySkeleton
{
    IPasswordVaultPluginAuthenticatorSkeleton CreateInstance();
}

[ComVisible(true)]
[Guid(PasskeyPluginManifestMetadata.AuthenticatorClassIdString)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class PasswordVaultPluginAuthenticatorComObject : IPasswordVaultPluginAuthenticatorSkeleton
{
    private readonly PluginActivationService _pluginActivationService;
    private readonly PluginCallbackTraceService _callbackTraceService;
    private readonly PluginOperationStateService _operationStateService;

    internal PasswordVaultPluginAuthenticatorComObject(
        PluginActivationService pluginActivationService,
        PluginCallbackTraceService callbackTraceService,
        PluginOperationStateService operationStateService)
    {
        _pluginActivationService = pluginActivationService;
        _callbackTraceService = callbackTraceService;
        _operationStateService = operationStateService;
    }

    public int MakeCredential(IntPtr pluginOperationRequest, IntPtr pluginOperationResponse)
    {
        _pluginActivationService.RecordPluginActivation("com-make-credential");
        var hr = unchecked((int)0x80004001); // E_NOTIMPL
        _operationStateService.CaptureCredentialOperation(
            "make-credential",
            pluginOperationRequest,
            pluginOperationResponse,
            hr);
        _callbackTraceService.Record(
            "make-credential",
            BuildCallbackMessage(
                "MakeCredential",
                pluginOperationRequest,
                pluginOperationResponse),
            hr);
        return hr;
    }

    public int GetAssertion(IntPtr pluginOperationRequest, IntPtr pluginOperationResponse)
    {
        _pluginActivationService.RecordPluginActivation("com-get-assertion");
        var hr = unchecked((int)0x80004001); // E_NOTIMPL
        _operationStateService.CaptureCredentialOperation(
            "get-assertion",
            pluginOperationRequest,
            pluginOperationResponse,
            hr);
        _callbackTraceService.Record(
            "get-assertion",
            BuildCallbackMessage(
                "GetAssertion",
                pluginOperationRequest,
                pluginOperationResponse),
            hr);
        return hr;
    }

    public int CancelOperation(IntPtr cancelOperationRequest)
    {
        _pluginActivationService.RecordPluginActivation("com-cancel");
        _operationStateService.CaptureCancelOperation(cancelOperationRequest);
        _callbackTraceService.Record(
            "cancel-operation",
            $"CancelOperation reached the local COM authenticator. Request pointer present: {cancelOperationRequest != IntPtr.Zero}.",
            0);
        return 0;
    }

    public int GetLockStatus(out int pluginLockStatus)
    {
        _pluginActivationService.RecordPluginActivation("com-lock-status");
        pluginLockStatus = 0;
        _callbackTraceService.Record(
            "get-lock-status",
            "GetLockStatus reached the local COM authenticator. The skeleton currently reports the vault as unlocked.",
            0);
        return 0;
    }

    private static string BuildCallbackMessage(
        string kind,
        IntPtr requestPointer,
        IntPtr responsePointer)
    {
        return $"{kind} reached the local COM authenticator. Request pointer present: {requestPointer != IntPtr.Zero}. Response pointer present: {responsePointer != IntPtr.Zero}.";
    }
}

internal sealed class PasswordVaultPluginAuthenticatorFactorySkeleton
    : IPasswordVaultPluginAuthenticatorFactorySkeleton
{
    private readonly PluginActivationService _pluginActivationService;
    private readonly PluginCallbackTraceService _callbackTraceService;
    private readonly PluginOperationStateService _operationStateService;

    public PasswordVaultPluginAuthenticatorFactorySkeleton(
        PluginActivationService pluginActivationService,
        PluginCallbackTraceService callbackTraceService,
        PluginOperationStateService operationStateService)
    {
        _pluginActivationService = pluginActivationService;
        _callbackTraceService = callbackTraceService;
        _operationStateService = operationStateService;
    }

    public IPasswordVaultPluginAuthenticatorSkeleton CreateInstance()
    {
        return new PasswordVaultPluginAuthenticatorComObject(
            _pluginActivationService,
            _callbackTraceService,
            _operationStateService);
    }
}

internal sealed class PluginComServerSkeletonService
{
    private readonly PluginActivationService _pluginActivationService;
    private readonly PluginCallbackTraceService _callbackTraceService;
    private readonly PluginOperationStateService _operationStateService;
    private readonly object _syncRoot = new();
    private PluginComServerSnapshot _lastSnapshot = new()
    {
        LastProbeMessage = "The COM authenticator skeleton has not been probed yet.",
    };

    public PluginComServerSkeletonService(
        PluginActivationService pluginActivationService,
        PluginCallbackTraceService callbackTraceService,
        PluginOperationStateService operationStateService)
    {
        _pluginActivationService = pluginActivationService;
        _callbackTraceService = callbackTraceService;
        _operationStateService = operationStateService;
    }

    public PluginComServerSnapshot Probe()
    {
        var probedAt = DateTimeOffset.UtcNow;

        try
        {
            var classIdMatchesManifest =
                typeof(PasswordVaultPluginAuthenticatorComObject).GUID ==
                PasskeyPluginManifestMetadata.AuthenticatorClassId;

            var factory = new PasswordVaultPluginAuthenticatorFactorySkeleton(
                _pluginActivationService,
                _callbackTraceService,
                _operationStateService);
            var instance = factory.CreateInstance();

            var snapshot = new PluginComServerSnapshot
            {
                ComSkeletonReady = classIdMatchesManifest && factory is not null && instance is not null,
                ClassIdMatchesManifest = classIdMatchesManifest,
                FactoryReady = factory is not null,
                AuthenticatorReady = instance is not null,
                LastProbeUnixTimeMs = probedAt.ToUnixTimeMilliseconds(),
                LastProbeMessage = classIdMatchesManifest
                    ? "The local COM authenticator skeleton can be instantiated and its class ID matches the manifest."
                    : "The local COM authenticator skeleton exists, but its class ID does not match the packaged manifest.",
                AuthenticatorTypeName = typeof(PasswordVaultPluginAuthenticatorComObject).FullName ?? string.Empty,
            };

            lock (_syncRoot)
            {
                _lastSnapshot = snapshot;
                return _lastSnapshot;
            }
        }
        catch (Exception exception)
        {
            var snapshot = new PluginComServerSnapshot
            {
                ComSkeletonReady = false,
                ClassIdMatchesManifest =
                    typeof(PasswordVaultPluginAuthenticatorComObject).GUID ==
                    PasskeyPluginManifestMetadata.AuthenticatorClassId,
                FactoryReady = false,
                AuthenticatorReady = false,
                LastProbeUnixTimeMs = probedAt.ToUnixTimeMilliseconds(),
                LastProbeMessage = string.IsNullOrWhiteSpace(exception.Message)
                    ? "The COM authenticator skeleton could not be instantiated."
                    : exception.Message,
                AuthenticatorTypeName = typeof(PasswordVaultPluginAuthenticatorComObject).FullName ?? string.Empty,
            };

            lock (_syncRoot)
            {
                _lastSnapshot = snapshot;
                return _lastSnapshot;
            }
        }
    }

    public PluginComServerSnapshot GetLastSnapshot()
    {
        lock (_syncRoot)
        {
            return _lastSnapshot;
        }
    }
}
