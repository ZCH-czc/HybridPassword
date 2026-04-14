namespace blazorApp.Services;

public interface IPasskeyDiagnosticsService
{
    void AddInfo(string source, string message);

    void AddWarning(string source, string message);

    void AddError(string source, string message);

    IReadOnlyList<PasskeyLogEntryState> GetEntries();
}
