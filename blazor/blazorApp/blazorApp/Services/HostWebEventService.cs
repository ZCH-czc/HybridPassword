using System.Text.Json;
using Microsoft.Maui.Controls;

namespace blazorApp.Services;

public sealed class HostWebEventService : IHostWebEventService
{
    private readonly SemaphoreSlim _dispatchLock = new(1, 1);
    private HybridWebView? _hybridWebView;
    private string? _pendingScript;

    public void Attach(HybridWebView hybridWebView)
    {
        _hybridWebView = hybridWebView;
    }

    public async Task RequestVaultLockAsync(string reason, string message)
    {
        var payload = JsonSerializer.Serialize(new
        {
            reason,
            message,
        });

        var script =
            $"window.dispatchEvent(new CustomEvent('password-vault-host-lock', {{ detail: {payload} }}));";

        await DispatchOrQueueAsync(script);
    }

    public async Task RequestIncrementalSyncApplyAsync(string recordsJson, string sourceLabel)
    {
        var payload = JsonSerializer.Serialize(new
        {
            sourceLabel,
            records = JsonSerializer.Deserialize<object>(recordsJson),
        });

        var script =
            $"window.dispatchEvent(new CustomEvent('password-vault-host-sync-apply', {{ detail: {payload} }}));";

        await DispatchOrQueueAsync(script);
    }

    public async Task FlushPendingAsync()
    {
        if (string.IsNullOrWhiteSpace(_pendingScript))
        {
            return;
        }

        await DispatchOrQueueAsync(_pendingScript);
    }

    private async Task DispatchOrQueueAsync(string script)
    {
        await _dispatchLock.WaitAsync();

        try
        {
            if (!await TryDispatchAsync(script))
            {
                _pendingScript = script;
                return;
            }

            if (string.Equals(_pendingScript, script, StringComparison.Ordinal))
            {
                _pendingScript = null;
            }
        }
        finally
        {
            _dispatchLock.Release();
        }
    }

    private async Task<bool> TryDispatchAsync(string script)
    {
        if (_hybridWebView is null)
        {
            return false;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(() => _hybridWebView.EvaluateJavaScriptAsync(script));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
