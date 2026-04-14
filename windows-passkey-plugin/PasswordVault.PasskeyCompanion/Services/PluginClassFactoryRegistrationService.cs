using PasswordVault.PasskeyCompanion.Models;
using System.Runtime.InteropServices;

namespace PasswordVault.PasskeyCompanion.Services;

[ComVisible(true)]
[Guid("00000001-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IClassFactoryNative
{
    [PreserveSig]
    int CreateInstance(IntPtr outer, ref Guid iid, out IntPtr result);

    [PreserveSig]
    int LockServer([MarshalAs(UnmanagedType.Bool)] bool lockServer);
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal sealed class PasswordVaultPluginAuthenticatorClassFactory : IClassFactoryNative
{
    private static readonly Guid IUnknownGuid = new("00000000-0000-0000-C000-000000000046");
    private const int ClassENoAggregation = unchecked((int)0x80040110);

    private readonly PluginActivationService _pluginActivationService;
    private readonly PluginCallbackTraceService _callbackTraceService;
    private readonly PluginOperationStateService _operationStateService;

    public PasswordVaultPluginAuthenticatorClassFactory(
        PluginActivationService pluginActivationService,
        PluginCallbackTraceService callbackTraceService,
        PluginOperationStateService operationStateService)
    {
        _pluginActivationService = pluginActivationService;
        _callbackTraceService = callbackTraceService;
        _operationStateService = operationStateService;
    }

    public int CreateInstance(IntPtr outer, ref Guid iid, out IntPtr result)
    {
        _pluginActivationService.RecordPluginActivation("com-class-factory-create-instance");
        result = IntPtr.Zero;

        if (outer != IntPtr.Zero)
        {
            return ClassENoAggregation;
        }

        IntPtr unknown = IntPtr.Zero;

        try
        {
            var instance = new PasswordVaultPluginAuthenticatorComObject(
                _pluginActivationService,
                _callbackTraceService,
                _operationStateService);
            unknown = Marshal.GetIUnknownForObject(instance);

            if (iid == IUnknownGuid)
            {
                result = unknown;
                unknown = IntPtr.Zero;
                return 0;
            }

            return Marshal.QueryInterface(unknown, in iid, out result);
        }
        catch (Exception exception)
        {
            return Marshal.GetHRForException(exception);
        }
        finally
        {
            if (unknown != IntPtr.Zero)
            {
                Marshal.Release(unknown);
            }
        }
    }

    public int LockServer(bool lockServer)
    {
        _pluginActivationService.RecordPluginActivation(
            lockServer
                ? "com-class-factory-lock-server"
                : "com-class-factory-unlock-server");
        return 0;
    }
}

internal sealed class PluginClassFactoryRegistrationService : IDisposable
{
    private const uint ClsctxLocalServer = 0x4;
    private const uint RegclsMultipleUse = 1;

    private readonly PluginActivationService _pluginActivationService;
    private readonly PluginCallbackTraceService _callbackTraceService;
    private readonly PluginOperationStateService _operationStateService;
    private readonly object _syncRoot = new();

    private PasswordVaultPluginAuthenticatorClassFactory? _classFactory;
    private IntPtr _classFactoryUnknown = IntPtr.Zero;
    private uint _registrationCookie;
    private bool _disposed;
    private PluginClassFactoryRegistrationSnapshot _snapshot = new();

    public PluginClassFactoryRegistrationService(
        PluginActivationService pluginActivationService,
        PluginCallbackTraceService callbackTraceService,
        PluginOperationStateService operationStateService)
    {
        _pluginActivationService = pluginActivationService;
        _callbackTraceService = callbackTraceService;
        _operationStateService = operationStateService;
    }

    public PluginClassFactoryRegistrationSnapshot EnsureRegistered()
    {
        lock (_syncRoot)
        {
            if (_disposed)
            {
                _snapshot = CreateSnapshot(
                    isRegistered: false,
                    registrationCookie: 0,
                    lastMessage: "The local COM class factory is already disposed.",
                    lastHResult: unchecked((int)0x80004005));
                return _snapshot;
            }

            if (_registrationCookie != 0)
            {
                _snapshot = CreateSnapshot(
                    isRegistered: true,
                    registrationCookie: _registrationCookie,
                    lastMessage: "The local COM class factory is already registered for this companion session.",
                    lastHResult: 0);
                return _snapshot;
            }

            try
            {
                _classFactory = new PasswordVaultPluginAuthenticatorClassFactory(
                    _pluginActivationService,
                    _callbackTraceService,
                    _operationStateService);
                _classFactoryUnknown = Marshal.GetIUnknownForObject(_classFactory);

                var classId = PasskeyPluginManifestMetadata.AuthenticatorClassId;
                var hr = CoRegisterClassObject(
                    ref classId,
                    _classFactoryUnknown,
                    ClsctxLocalServer,
                    RegclsMultipleUse,
                    out _registrationCookie);

                if (hr != 0)
                {
                    ReleaseClassFactoryReference();

                    _snapshot = CreateSnapshot(
                        isRegistered: false,
                        registrationCookie: 0,
                        lastMessage: $"Windows rejected the local COM class factory registration request. ({WindowsWebAuthnPluginNative.ToHex(hr)})",
                        lastHResult: hr);
                    return _snapshot;
                }

                _pluginActivationService.RecordPluginActivation("com-class-factory-registered");
                _snapshot = CreateSnapshot(
                    isRegistered: true,
                    registrationCookie: _registrationCookie,
                    lastMessage: $"The local COM class factory is registered with cookie {_registrationCookie}.",
                    lastHResult: 0);
                return _snapshot;
            }
            catch (Exception exception)
            {
                ReleaseClassFactoryReference();

                var hr = Marshal.GetHRForException(exception);
                _snapshot = CreateSnapshot(
                    isRegistered: false,
                    registrationCookie: 0,
                    lastMessage: string.IsNullOrWhiteSpace(exception.Message)
                        ? "The local COM class factory could not be registered."
                        : exception.Message,
                    lastHResult: hr);
                return _snapshot;
            }
        }
    }

    public PluginClassFactoryRegistrationSnapshot GetSnapshot()
    {
        lock (_syncRoot)
        {
            return new PluginClassFactoryRegistrationSnapshot
            {
                IsRegistered = _snapshot.IsRegistered,
                RegistrationCookie = _snapshot.RegistrationCookie,
                LastRegistrationUnixTimeMs = _snapshot.LastRegistrationUnixTimeMs,
                LastMessage = _snapshot.LastMessage,
                LastHResult = _snapshot.LastHResult,
            };
        }
    }

    public PluginClassFactoryRegistrationSnapshot Revoke()
    {
        lock (_syncRoot)
        {
            if (_registrationCookie == 0)
            {
                _snapshot = CreateSnapshot(
                    isRegistered: false,
                    registrationCookie: 0,
                    lastMessage: "The local COM class factory is not registered in this session.",
                    lastHResult: 0);
                return _snapshot;
            }

            try
            {
                var cookie = _registrationCookie;
                var hr = CoRevokeClassObject(cookie);
                ReleaseClassFactoryReference();
                _registrationCookie = 0;

                _snapshot = CreateSnapshot(
                    isRegistered: false,
                    registrationCookie: 0,
                    lastMessage: hr == 0
                        ? $"The local COM class factory registration cookie {cookie} was revoked."
                        : $"Windows returned {WindowsWebAuthnPluginNative.ToHex(hr)} while revoking local COM class factory cookie {cookie}.",
                    lastHResult: hr);
                return _snapshot;
            }
            catch (Exception exception)
            {
                var hr = Marshal.GetHRForException(exception);
                _snapshot = CreateSnapshot(
                    isRegistered: false,
                    registrationCookie: 0,
                    lastMessage: string.IsNullOrWhiteSpace(exception.Message)
                        ? "The local COM class factory could not be revoked cleanly."
                        : exception.Message,
                    lastHResult: hr);
                return _snapshot;
            }
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _ = Revoke();
    }

    private static PluginClassFactoryRegistrationSnapshot CreateSnapshot(
        bool isRegistered,
        uint registrationCookie,
        string lastMessage,
        int lastHResult)
    {
        return new PluginClassFactoryRegistrationSnapshot
        {
            IsRegistered = isRegistered,
            RegistrationCookie = registrationCookie,
            LastRegistrationUnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            LastMessage = lastMessage,
            LastHResult = lastHResult,
        };
    }

    private void ReleaseClassFactoryReference()
    {
        if (_classFactoryUnknown != IntPtr.Zero)
        {
            Marshal.Release(_classFactoryUnknown);
            _classFactoryUnknown = IntPtr.Zero;
        }

        _classFactory = null;
    }

    [DllImport("ole32.dll")]
    private static extern int CoRegisterClassObject(
        ref Guid rclsid,
        IntPtr pUnk,
        uint dwClsContext,
        uint flags,
        out uint lpdwRegister);

    [DllImport("ole32.dll")]
    private static extern int CoRevokeClassObject(uint dwRegister);
}
