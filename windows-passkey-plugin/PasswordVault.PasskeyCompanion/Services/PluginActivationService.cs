using PasswordVault.PasskeyCompanion.Models;

namespace PasswordVault.PasskeyCompanion.Services;

public sealed class PluginActivationService
{
    private readonly object _syncRoot = new();
    private int _activationCount;
    private DateTimeOffset? _lastActivationAt;
    private string _lastActivationSource = string.Empty;
    private bool _startedFromPluginActivation;

    public void RecordPluginActivation(string source, bool isStartupActivation = false)
    {
        lock (_syncRoot)
        {
            _activationCount += 1;
            _lastActivationAt = DateTimeOffset.UtcNow;
            _lastActivationSource = string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim();
            _startedFromPluginActivation |= isStartupActivation;
        }
    }

    public PluginActivationSnapshot GetSnapshot()
    {
        lock (_syncRoot)
        {
            return new PluginActivationSnapshot
            {
                ActivationCount = _activationCount,
                LastActivationUnixTimeMs = _lastActivationAt?.ToUnixTimeMilliseconds() ?? 0,
                LastActivationSource = _lastActivationSource,
                StartedFromPluginActivation = _startedFromPluginActivation,
            };
        }
    }
}
