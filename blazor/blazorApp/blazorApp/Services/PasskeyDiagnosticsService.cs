namespace blazorApp.Services;

public sealed class PasskeyDiagnosticsService : IPasskeyDiagnosticsService
{
    private const int MaxEntries = 80;
    private const long DeduplicationWindowMs = 15_000;

    private readonly object _lock = new();
    private readonly List<PasskeyLogEntryState> _entries = [];

    public void AddInfo(string source, string message)
    {
        Add("info", source, message);
    }

    public void AddWarning(string source, string message)
    {
        Add("warning", source, message);
    }

    public void AddError(string source, string message)
    {
        Add("error", source, message);
    }

    public IReadOnlyList<PasskeyLogEntryState> GetEntries()
    {
        lock (_lock)
        {
            return _entries
                .Select(entry => new PasskeyLogEntryState
                {
                    TimestampUnixTimeMs = entry.TimestampUnixTimeMs,
                    Level = entry.Level,
                    Source = entry.Source,
                    Message = entry.Message,
                    RepeatCount = entry.RepeatCount,
                })
                .ToArray();
        }
    }

    private void Add(string level, string source, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var normalizedSource = string.IsNullOrWhiteSpace(source) ? "passkey" : source.Trim();
        var normalizedMessage = message.Trim();

        lock (_lock)
        {
            var existing = _entries.FirstOrDefault(entry =>
                string.Equals(entry.Level, level, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(entry.Source, normalizedSource, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(entry.Message, normalizedMessage, StringComparison.Ordinal));

            if (existing is not null && now - existing.TimestampUnixTimeMs <= DeduplicationWindowMs)
            {
                existing.TimestampUnixTimeMs = now;
                existing.RepeatCount += 1;
                return;
            }

            _entries.Insert(0, new PasskeyLogEntryState
            {
                TimestampUnixTimeMs = now,
                Level = level,
                Source = normalizedSource,
                Message = normalizedMessage,
                RepeatCount = 1,
            });

            if (_entries.Count > MaxEntries)
            {
                _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);
            }
        }
    }
}
