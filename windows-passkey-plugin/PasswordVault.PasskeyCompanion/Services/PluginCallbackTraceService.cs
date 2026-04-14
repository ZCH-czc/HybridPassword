using PasswordVault.PasskeyCompanion.Models;

namespace PasswordVault.PasskeyCompanion.Services;

internal sealed class PluginCallbackTraceService
{
    private readonly object _syncRoot = new();

    private int _makeCredentialCount;
    private int _getAssertionCount;
    private int _cancelOperationCount;
    private int _getLockStatusCount;
    private DateTimeOffset? _lastCallbackAt;
    private string _lastCallbackKind = string.Empty;
    private string _lastCallbackMessage = "No plugin callback has been captured yet.";
    private int _lastCallbackHResult;

    public void Record(
        string kind,
        string message,
        int hResult = 0,
        bool incrementCount = true)
    {
        lock (_syncRoot)
        {
            switch (kind)
            {
                case "make-credential":
                    if (incrementCount)
                    {
                        _makeCredentialCount += 1;
                    }
                    break;
                case "get-assertion":
                    if (incrementCount)
                    {
                        _getAssertionCount += 1;
                    }
                    break;
                case "cancel-operation":
                    if (incrementCount)
                    {
                        _cancelOperationCount += 1;
                    }
                    break;
                case "get-lock-status":
                    if (incrementCount)
                    {
                        _getLockStatusCount += 1;
                    }
                    break;
            }

            _lastCallbackAt = DateTimeOffset.UtcNow;
            _lastCallbackKind = kind;
            _lastCallbackMessage = message;
            _lastCallbackHResult = hResult;
        }
    }

    public PluginCallbackTraceSnapshot GetSnapshot()
    {
        lock (_syncRoot)
        {
            return new PluginCallbackTraceSnapshot
            {
                TotalCount = _makeCredentialCount + _getAssertionCount + _cancelOperationCount + _getLockStatusCount,
                MakeCredentialCount = _makeCredentialCount,
                GetAssertionCount = _getAssertionCount,
                CancelOperationCount = _cancelOperationCount,
                GetLockStatusCount = _getLockStatusCount,
                LastCallbackUnixTimeMs = _lastCallbackAt?.ToUnixTimeMilliseconds() ?? 0,
                LastCallbackKind = _lastCallbackKind,
                LastCallbackMessage = _lastCallbackMessage,
                LastCallbackHResultHex = _lastCallbackHResult == 0
                    ? string.Empty
                    : WindowsWebAuthnPluginNative.ToHex(_lastCallbackHResult),
            };
        }
    }
}
