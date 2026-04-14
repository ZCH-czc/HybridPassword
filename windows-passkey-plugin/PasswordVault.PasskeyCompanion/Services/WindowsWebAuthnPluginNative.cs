using PasswordVault.PasskeyCompanion.Models;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PasswordVault.PasskeyCompanion.Services;

internal static class WindowsWebAuthnPluginNative
{
    private const string WebAuthnLibrary = "webauthn.dll";
    private const int NteNotFound = unchecked((int)0x80090011);
    private const int NteExists = unchecked((int)0x8009000F);

    internal static PluginAuthenticatorStateProbeResult TryGetAuthenticatorState()
    {
        try
        {
            var hr = WebAuthNPluginGetAuthenticatorState(
                in PasskeyPluginManifestMetadata.AuthenticatorClassId,
                out var rawState);

            if (hr == 0)
            {
                return new PluginAuthenticatorStateProbeResult(
                    true,
                    rawState,
                    MapAuthenticatorState(rawState),
                    hr,
                    "The Windows plugin authenticator is registered.");
            }

            if (hr == NteNotFound)
            {
                return new PluginAuthenticatorStateProbeResult(
                    false,
                    0,
                    "unknown",
                    hr,
                    "The Windows plugin authenticator has not been registered yet.");
            }

            return new PluginAuthenticatorStateProbeResult(
                false,
                rawState,
                MapAuthenticatorState(rawState),
                hr,
                DescribeHResult("Windows could not query the current plugin authenticator state.", hr));
        }
        catch (Exception exception)
        {
            return new PluginAuthenticatorStateProbeResult(
                false,
                0,
                "unknown",
                Marshal.GetHRForException(exception),
                exception.Message);
        }
    }

    internal static PluginAddAuthenticatorResult TryAddAuthenticator()
    {
        var authenticatorInfo = PasskeyPluginManifestMetadata.AuthenticatorInfo;
        var authenticatorInfoBuffer = IntPtr.Zero;

        try
        {
            authenticatorInfoBuffer = Marshal.AllocHGlobal(authenticatorInfo.Length);
            Marshal.Copy(authenticatorInfo, 0, authenticatorInfoBuffer, authenticatorInfo.Length);

            var options = new WebAuthNPluginAddAuthenticatorOptions
            {
                AuthenticatorName = PasskeyPluginManifestMetadata.AuthenticatorName,
                AuthenticatorClassId = PasskeyPluginManifestMetadata.AuthenticatorClassId,
                PluginRpId = PasskeyPluginManifestMetadata.PluginRpId,
                LightThemeLogoSvg = PasskeyPluginManifestMetadata.LightThemeLogoSvgBase64,
                DarkThemeLogoSvg = PasskeyPluginManifestMetadata.DarkThemeLogoSvgBase64,
                AuthenticatorInfoLength = (uint)authenticatorInfo.Length,
                AuthenticatorInfo = authenticatorInfoBuffer,
                SupportedRpIdCount = 0,
                SupportedRpIds = IntPtr.Zero,
            };

            var hr = WebAuthNPluginAddAuthenticator(in options, out var responsePointer);
            if (hr == NteExists)
            {
                return new PluginAddAuthenticatorResult(
                    true,
                    hr,
                    null,
                    "The Windows plugin authenticator was already registered.");
            }

            if (hr != 0)
            {
                return new PluginAddAuthenticatorResult(
                    false,
                    hr,
                    null,
                    DescribeHResult("Windows rejected the plugin authenticator registration request.", hr));
            }

            try
            {
                var response = Marshal.PtrToStructure<WebAuthNPluginAddAuthenticatorResponse>(responsePointer);
                var operationSigningPublicKey = ReadUnmanagedBytes(response.OperationSigningPublicKey, response.OperationSigningPublicKeyLength);

                return new PluginAddAuthenticatorResult(
                    true,
                    hr,
                    operationSigningPublicKey,
                    "The Windows plugin authenticator registration request completed successfully.");
            }
            finally
            {
                if (responsePointer != IntPtr.Zero)
                {
                    WebAuthNPluginFreeAddAuthenticatorResponse(responsePointer);
                }
            }
        }
        catch (Exception exception)
        {
            return new PluginAddAuthenticatorResult(
                false,
                Marshal.GetHRForException(exception),
                null,
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "The Windows plugin authenticator registration request failed unexpectedly."
                    : exception.Message);
        }
        finally
        {
            if (authenticatorInfoBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(authenticatorInfoBuffer);
            }
        }
    }

    internal static PluginOperationSigningKeyResult TryGetOperationSigningPublicKey()
    {
        try
        {
            var hr = WebAuthNPluginGetOperationSigningPublicKey(
                in PasskeyPluginManifestMetadata.AuthenticatorClassId,
                out var byteLength,
                out var keyPointer);

            if (hr != 0)
            {
                return new PluginOperationSigningKeyResult(
                    false,
                    null,
                    hr,
                    DescribeHResult("Windows could not read the plugin operation-signing public key.", hr));
            }

            try
            {
                var bytes = ReadUnmanagedBytes(keyPointer, byteLength);
                return new PluginOperationSigningKeyResult(
                    true,
                    bytes,
                    hr,
                    "The plugin operation-signing public key was loaded from Windows.");
            }
            finally
            {
                if (keyPointer != IntPtr.Zero)
                {
                    WebAuthNPluginFreePublicKeyResponse(keyPointer);
                }
            }
        }
        catch (Exception exception)
        {
            return new PluginOperationSigningKeyResult(
                false,
                null,
                Marshal.GetHRForException(exception),
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "The plugin operation-signing public key could not be read."
                    : exception.Message);
        }
    }

    internal static string MapAuthenticatorState(int rawState)
    {
        return rawState switch
        {
            2 => "enabled",
            1 => "disabled",
            _ => "unknown",
        };
    }

    internal static string DescribeHResult(string prefix, int hr)
    {
        var detail = hr switch
        {
            0 => "The operation completed successfully.",
            NteNotFound => "Windows reports that the plugin authenticator is not registered yet.",
            NteExists => "Windows reports that the plugin authenticator is already registered.",
            _ => new Win32Exception(hr).Message,
        };

        return $"{prefix} ({ToHex(hr)}) {detail}";
    }

    internal static string ToHex(int hr)
    {
        return $"0x{unchecked((uint)hr):X8}";
    }

    private static byte[]? ReadUnmanagedBytes(IntPtr pointer, uint length)
    {
        if (pointer == IntPtr.Zero || length == 0)
        {
            return null;
        }

        var bytes = new byte[length];
        Marshal.Copy(pointer, bytes, 0, bytes.Length);
        return bytes;
    }

    [DllImport(WebAuthnLibrary, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int WebAuthNPluginAddAuthenticator(
        in WebAuthNPluginAddAuthenticatorOptions pluginAddAuthenticatorOptions,
        out IntPtr pluginAddAuthenticatorResponse);

    [DllImport(WebAuthnLibrary, ExactSpelling = true)]
    private static extern void WebAuthNPluginFreeAddAuthenticatorResponse(IntPtr pluginAddAuthenticatorResponse);

    [DllImport(WebAuthnLibrary, ExactSpelling = true)]
    private static extern int WebAuthNPluginGetAuthenticatorState(
        in Guid authenticatorClassId,
        out int authenticatorState);

    [DllImport(WebAuthnLibrary, ExactSpelling = true)]
    private static extern int WebAuthNPluginGetOperationSigningPublicKey(
        in Guid authenticatorClassId,
        out uint operationSigningPublicKeyLength,
        out IntPtr operationSigningPublicKey);

    [DllImport(WebAuthnLibrary, ExactSpelling = true)]
    private static extern void WebAuthNPluginFreePublicKeyResponse(IntPtr operationSigningPublicKey);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WebAuthNPluginAddAuthenticatorOptions
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string AuthenticatorName;

        public Guid AuthenticatorClassId;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string PluginRpId;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string LightThemeLogoSvg;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string DarkThemeLogoSvg;

        public uint AuthenticatorInfoLength;

        public IntPtr AuthenticatorInfo;

        public uint SupportedRpIdCount;

        public IntPtr SupportedRpIds;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WebAuthNPluginAddAuthenticatorResponse
    {
        public uint OperationSigningPublicKeyLength;
        public IntPtr OperationSigningPublicKey;
    }
}

internal sealed record PluginAuthenticatorStateProbeResult(
    bool IsRegistered,
    int RawState,
    string StateLabel,
    int HResult,
    string Message);

internal sealed record PluginAddAuthenticatorResult(
    bool Success,
    int HResult,
    byte[]? OperationSigningPublicKey,
    string Message);

internal sealed record PluginOperationSigningKeyResult(
    bool Success,
    byte[]? OperationSigningPublicKey,
    int HResult,
    string Message);
