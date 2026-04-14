using PasswordVault.PasskeyCompanion.Models;

namespace PasswordVault.PasskeyCompanion.Services;

public sealed class PluginOperationStateService
{
    private const int NotImplementedHResult = unchecked((int)0x80004001);

    private readonly object _syncRoot = new();
    private PluginOperationSnapshot _snapshot = new()
    {
        State = "idle",
        Message = "No plugin operation has been captured yet.",
    };

    public PluginOperationSnapshot CaptureCredentialOperation(
        string kind,
        IntPtr requestPointer,
        IntPtr responsePointer,
        int hResult)
    {
        var normalizedKind = string.IsNullOrWhiteSpace(kind) ? "unknown" : kind.Trim().ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;
        var snapshot = new PluginOperationSnapshot
        {
            OperationId = Guid.NewGuid().ToString("N"),
            Kind = normalizedKind,
            State = BuildOperationState(hResult),
            Source = "com-callback",
            CreatedAtUnixTimeMs = now.ToUnixTimeMilliseconds(),
            UpdatedAtUnixTimeMs = now.ToUnixTimeMilliseconds(),
            RequestPointerPresent = requestPointer != IntPtr.Zero,
            ResponsePointerPresent = responsePointer != IntPtr.Zero,
            CancelPointerPresent = false,
            Message = BuildCredentialMessage(normalizedKind, requestPointer, responsePointer, hResult),
            HResultHex = hResult == 0 ? string.Empty : WindowsWebAuthnPluginNative.ToHex(hResult),
        };

        lock (_syncRoot)
        {
            _snapshot = snapshot;
            return CloneSnapshot(_snapshot);
        }
    }

    public PluginOperationSnapshot CaptureCancelOperation(IntPtr cancelRequestPointer)
    {
        var now = DateTimeOffset.UtcNow;

        lock (_syncRoot)
        {
            var hasExistingOperation = !string.IsNullOrWhiteSpace(_snapshot.OperationId);

            _snapshot = new PluginOperationSnapshot
            {
                OperationId = hasExistingOperation ? _snapshot.OperationId : Guid.NewGuid().ToString("N"),
                Kind = hasExistingOperation ? _snapshot.Kind : "cancel-operation",
                State = "cancel-requested",
                Source = "com-callback",
                CreatedAtUnixTimeMs = hasExistingOperation
                    ? _snapshot.CreatedAtUnixTimeMs
                    : now.ToUnixTimeMilliseconds(),
                UpdatedAtUnixTimeMs = now.ToUnixTimeMilliseconds(),
                RequestPointerPresent = hasExistingOperation && _snapshot.RequestPointerPresent,
                ResponsePointerPresent = hasExistingOperation && _snapshot.ResponsePointerPresent,
                CancelPointerPresent = cancelRequestPointer != IntPtr.Zero,
                Message = hasExistingOperation
                    ? $"Windows requested cancellation for the active {_snapshot.Kind} plugin operation."
                    : "Windows requested cancellation, but no active plugin operation was being tracked yet.",
                HResultHex = string.Empty,
            };

            return CloneSnapshot(_snapshot);
        }
    }

    public PluginOperationSnapshot GetSnapshot()
    {
        lock (_syncRoot)
        {
            return CloneSnapshot(_snapshot);
        }
    }

    public PluginOperationSnapshot ResolveLatestOperation(string resolution, string message)
    {
        var normalizedResolution = string.IsNullOrWhiteSpace(resolution)
            ? "approved"
            : resolution.Trim().ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;

        lock (_syncRoot)
        {
            if (normalizedResolution == "clear")
            {
                _snapshot = new PluginOperationSnapshot
                {
                    State = "idle",
                    Message = string.IsNullOrWhiteSpace(message)
                        ? "The tracked plugin operation was cleared from the companion session."
                        : message,
                    UpdatedAtUnixTimeMs = now.ToUnixTimeMilliseconds(),
                };

                return CloneSnapshot(_snapshot);
            }

            var hasExistingOperation = !string.IsNullOrWhiteSpace(_snapshot.OperationId);
            var nextState = normalizedResolution switch
            {
                "reject" or "rejected" => "rejected-skeleton",
                _ => "approved-skeleton",
            };

            _snapshot = new PluginOperationSnapshot
            {
                OperationId = hasExistingOperation ? _snapshot.OperationId : Guid.NewGuid().ToString("N"),
                Kind = hasExistingOperation ? _snapshot.Kind : "unknown",
                State = nextState,
                Source = "companion-manual-resolution",
                CreatedAtUnixTimeMs = hasExistingOperation
                    ? _snapshot.CreatedAtUnixTimeMs
                    : now.ToUnixTimeMilliseconds(),
                UpdatedAtUnixTimeMs = now.ToUnixTimeMilliseconds(),
                RequestPointerPresent = hasExistingOperation && _snapshot.RequestPointerPresent,
                ResponsePointerPresent = hasExistingOperation && _snapshot.ResponsePointerPresent,
                CancelPointerPresent = hasExistingOperation && _snapshot.CancelPointerPresent,
                Message = BuildResolutionMessage(nextState, message),
                HResultHex = string.Empty,
            };

            return CloneSnapshot(_snapshot);
        }
    }

    private static string BuildOperationState(int hResult)
    {
        if (hResult == 0)
        {
            return "captured";
        }

        if (hResult == NotImplementedHResult)
        {
            return "captured-not-implemented";
        }

        return "captured-error";
    }

    private static string BuildCredentialMessage(
        string kind,
        IntPtr requestPointer,
        IntPtr responsePointer,
        int hResult)
    {
        var operationLabel = kind switch
        {
            "make-credential" => "MakeCredential",
            "get-assertion" => "GetAssertion",
            _ => kind,
        };

        var baseMessage =
            $"{operationLabel} reached the plugin authenticator. Request pointer present: {requestPointer != IntPtr.Zero}. Response pointer present: {responsePointer != IntPtr.Zero}.";

        if (hResult == 0)
        {
            return $"{baseMessage} The operation is captured and ready for handling.";
        }

        if (hResult == NotImplementedHResult)
        {
            return $"{baseMessage} The callback is now captured as a structured operation, but the authenticator still returned E_NOTIMPL.";
        }

        return $"{baseMessage} Windows received {WindowsWebAuthnPluginNative.ToHex(hResult)} from the current authenticator skeleton.";
    }

    private static string BuildResolutionMessage(string state, string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            return message;
        }

        return state switch
        {
            "approved-skeleton" =>
                "The companion marked the latest plugin operation as approved in the current development skeleton.",
            "rejected-skeleton" =>
                "The companion marked the latest plugin operation as rejected in the current development skeleton.",
            _ => "The plugin operation state was updated.",
        };
    }

    private static PluginOperationSnapshot CloneSnapshot(PluginOperationSnapshot snapshot)
    {
        return new PluginOperationSnapshot
        {
            OperationId = snapshot.OperationId,
            Kind = snapshot.Kind,
            State = snapshot.State,
            Source = snapshot.Source,
            CreatedAtUnixTimeMs = snapshot.CreatedAtUnixTimeMs,
            UpdatedAtUnixTimeMs = snapshot.UpdatedAtUnixTimeMs,
            RequestPointerPresent = snapshot.RequestPointerPresent,
            ResponsePointerPresent = snapshot.ResponsePointerPresent,
            CancelPointerPresent = snapshot.CancelPointerPresent,
            Message = snapshot.Message,
            HResultHex = snapshot.HResultHex,
        };
    }
}
