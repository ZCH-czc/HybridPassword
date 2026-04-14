using System.Runtime.InteropServices;

namespace blazorApp.Services;

internal static class WindowsPasskeyNative
{
    private const string WebAuthnLibrary = "webauthn.dll";
    private const string GetPlatformCredentialListExport = "WebAuthNGetPlatformCredentialList";
    private const string DeletePlatformCredentialExport = "WebAuthNDeletePlatformCredential";
    private const string FreePlatformCredentialListExport = "WebAuthNFreePlatformCredentialList";

    private const uint WEBAUTHN_GET_CREDENTIALS_OPTIONS_CURRENT_VERSION = 1;
    private const uint WEBAUTHN_CTAP_TRANSPORT_USB = 0x00000001;
    private const uint WEBAUTHN_CTAP_TRANSPORT_NFC = 0x00000002;
    private const uint WEBAUTHN_CTAP_TRANSPORT_BLE = 0x00000004;
    private const uint WEBAUTHN_CTAP_TRANSPORT_TEST = 0x00000008;
    private const uint WEBAUTHN_CTAP_TRANSPORT_INTERNAL = 0x00000010;
    private const uint WEBAUTHN_CTAP_TRANSPORT_HYBRID = 0x00000020;
    private const uint WEBAUTHN_CTAP_TRANSPORT_SMART_CARD = 0x00000040;

    private static readonly Lazy<bool> ExportAvailability = new(DetectExportAvailability);

    internal static bool IsAvailable()
    {
#if WINDOWS
        return OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) && ExportAvailability.Value;
#else
        return false;
#endif
    }

    internal static IReadOnlyList<PasskeyMetadataState> ListPasskeys()
    {
        EnsureAvailable();

        var options = new WEBAUTHN_GET_CREDENTIALS_OPTIONS
        {
            dwVersion = WEBAUTHN_GET_CREDENTIALS_OPTIONS_CURRENT_VERSION,
            pwszRpId = IntPtr.Zero,
            bBrowserInPrivateMode = 0,
        };

        IntPtr listPointer = IntPtr.Zero;
        var result = WebAuthNGetPlatformCredentialList(ref options, out listPointer);
        Marshal.ThrowExceptionForHR(result);

        try
        {
            return ReadCredentialList(listPointer);
        }
        finally
        {
            if (listPointer != IntPtr.Zero)
            {
                WebAuthNFreePlatformCredentialList(listPointer);
            }
        }
    }

    internal static uint GetApiVersion()
    {
        if (!OperatingSystem.IsWindows())
        {
            return 0;
        }

        try
        {
            return WebAuthNGetApiVersionNumber();
        }
        catch
        {
            return 0;
        }
    }

    internal static bool HasPlatformAuthenticator()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            var result = WebAuthNIsUserVerifyingPlatformAuthenticatorAvailable(out var available);
            return result >= 0 && available != 0;
        }
        catch
        {
            return false;
        }
    }

    internal static void DeletePasskey(string nativeProviderRecordId)
    {
        EnsureAvailable();

        if (string.IsNullOrWhiteSpace(nativeProviderRecordId))
        {
            throw new InvalidOperationException("The passkey identifier is missing.");
        }

        byte[] credentialId;
        try
        {
            credentialId = Convert.FromBase64String(nativeProviderRecordId);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("The passkey identifier is invalid.", exception);
        }

        var result = WebAuthNDeletePlatformCredential((uint)credentialId.Length, credentialId);
        Marshal.ThrowExceptionForHR(result);
    }

    private static IReadOnlyList<PasskeyMetadataState> ReadCredentialList(IntPtr listPointer)
    {
        if (listPointer == IntPtr.Zero)
        {
            return [];
        }

        var list = Marshal.PtrToStructure<WEBAUTHN_CREDENTIAL_DETAILS_LIST>(listPointer);
        if (list.cCredentialDetails == 0 || list.ppCredentialDetails == IntPtr.Zero)
        {
            return [];
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var records = new List<PasskeyMetadataState>((int)list.cCredentialDetails);

        for (var index = 0; index < list.cCredentialDetails; index++)
        {
            var detailPointer = Marshal.ReadIntPtr(list.ppCredentialDetails, index * IntPtr.Size);
            if (detailPointer == IntPtr.Zero)
            {
                continue;
            }

            var detail = Marshal.PtrToStructure<WEBAUTHN_CREDENTIAL_DETAILS>(detailPointer);
            var rpInformation = detail.pRpInformation == IntPtr.Zero
                ? default
                : Marshal.PtrToStructure<WEBAUTHN_RP_ENTITY_INFORMATION>(detail.pRpInformation);
            var userInformation = detail.pUserInformation == IntPtr.Zero
                ? default
                : Marshal.PtrToStructure<WEBAUTHN_USER_ENTITY_INFORMATION>(detail.pUserInformation);
            byte[] credentialIdBytes = ReadBytes(detail.pbCredentialID, detail.cbCredentialID);
            byte[] userHandleBytes = detail.pUserInformation == IntPtr.Zero
                ? []
                : ReadBytes(userInformation.pbId, userInformation.cbId);

            var credentialIdBase64 = credentialIdBytes.Length == 0
                ? string.Empty
                : Convert.ToBase64String(credentialIdBytes);

            records.Add(new PasskeyMetadataState
            {
                NativeProviderRecordId = credentialIdBase64,
                CredentialId = credentialIdBase64,
                RpId = PtrToString(rpInformation.pwszId),
                Username = PtrToString(userInformation.pwszName),
                DisplayName = PtrToString(userInformation.pwszDisplayName),
                UserHandle = userHandleBytes.Length == 0 ? string.Empty : Convert.ToBase64String(userHandleBytes),
                TransportHints = DecodeTransportHints(detail.dwTransports),
                AuthenticatorName = PtrToString(detail.pwszAuthenticatorName),
                AttestationFormat = string.Empty,
                IsRemovable = detail.bRemovable != 0,
                IsBackedUp = detail.bBackedUp != 0,
                CreatedAt = 0,
                UpdatedAt = now,
                LastUsedAt = null,
            });
        }

        return records;
    }

    private static byte[] ReadBytes(IntPtr pointer, uint length)
    {
        if (pointer == IntPtr.Zero || length == 0)
        {
            return [];
        }

        var buffer = new byte[length];
        Marshal.Copy(pointer, buffer, 0, checked((int)length));
        return buffer;
    }

    private static string PtrToString(IntPtr pointer)
    {
        return pointer == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(pointer) ?? string.Empty;
    }

    private static string[] DecodeTransportHints(uint transports)
    {
        var hints = new List<string>();

        if ((transports & WEBAUTHN_CTAP_TRANSPORT_USB) != 0)
        {
            hints.Add("usb");
        }

        if ((transports & WEBAUTHN_CTAP_TRANSPORT_NFC) != 0)
        {
            hints.Add("nfc");
        }

        if ((transports & WEBAUTHN_CTAP_TRANSPORT_BLE) != 0)
        {
            hints.Add("ble");
        }

        if ((transports & WEBAUTHN_CTAP_TRANSPORT_INTERNAL) != 0)
        {
            hints.Add("internal");
        }

        if ((transports & WEBAUTHN_CTAP_TRANSPORT_HYBRID) != 0)
        {
            hints.Add("hybrid");
        }

        if ((transports & WEBAUTHN_CTAP_TRANSPORT_SMART_CARD) != 0)
        {
            hints.Add("smart-card");
        }

        if ((transports & WEBAUTHN_CTAP_TRANSPORT_TEST) != 0)
        {
            hints.Add("test");
        }

        return [.. hints];
    }

    private static void EnsureAvailable()
    {
        if (!IsAvailable())
        {
            throw new PlatformNotSupportedException("Windows platform credential APIs are not available on this device.");
        }
    }

    private static bool DetectExportAvailability()
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
            return NativeLibrary.TryGetExport(handle, GetPlatformCredentialListExport, out _) &&
                   NativeLibrary.TryGetExport(handle, DeletePlatformCredentialExport, out _) &&
                   NativeLibrary.TryGetExport(handle, FreePlatformCredentialListExport, out _);
        }
        finally
        {
            NativeLibrary.Free(handle);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WEBAUTHN_GET_CREDENTIALS_OPTIONS
    {
        public uint dwVersion;
        public IntPtr pwszRpId;
        public int bBrowserInPrivateMode;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WEBAUTHN_CREDENTIAL_DETAILS_LIST
    {
        public uint cCredentialDetails;
        public IntPtr ppCredentialDetails;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WEBAUTHN_CREDENTIAL_DETAILS
    {
        public uint dwVersion;
        public uint cbCredentialID;
        public IntPtr pbCredentialID;
        public IntPtr pRpInformation;
        public IntPtr pUserInformation;
        public int bRemovable;
        public int bBackedUp;
        public IntPtr pwszAuthenticatorName;
        public uint cbAuthenticatorLogo;
        public IntPtr pbAuthenticatorLogo;
        public int bThirdPartyPayment;
        public uint dwTransports;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WEBAUTHN_RP_ENTITY_INFORMATION
    {
        public uint dwVersion;
        public IntPtr pwszId;
        public IntPtr pwszName;
        public IntPtr pwszIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WEBAUTHN_USER_ENTITY_INFORMATION
    {
        public uint dwVersion;
        public uint cbId;
        public IntPtr pbId;
        public IntPtr pwszName;
        public IntPtr pwszIcon;
        public IntPtr pwszDisplayName;
    }

    [DllImport(WebAuthnLibrary, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int WebAuthNGetPlatformCredentialList(
        ref WEBAUTHN_GET_CREDENTIALS_OPTIONS pGetCredentialsOptions,
        out IntPtr ppCredentialDetailsList);

    [DllImport(WebAuthnLibrary, ExactSpelling = true)]
    private static extern void WebAuthNFreePlatformCredentialList(IntPtr pCredentialDetailsList);

    [DllImport(WebAuthnLibrary, ExactSpelling = true)]
    private static extern uint WebAuthNGetApiVersionNumber();

    [DllImport(WebAuthnLibrary, ExactSpelling = true)]
    private static extern int WebAuthNIsUserVerifyingPlatformAuthenticatorAvailable(out int pbIsUserVerifyingPlatformAuthenticatorAvailable);

    [DllImport(WebAuthnLibrary, ExactSpelling = true)]
    private static extern int WebAuthNDeletePlatformCredential(
        uint cbCredentialId,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] pbCredentialId);
}
