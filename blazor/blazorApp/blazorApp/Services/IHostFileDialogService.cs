namespace blazorApp.Services;

public interface IHostFileDialogService
{
    Task<HostFileOperationResult> SaveTextFileAsync(string fileName, string content, string mimeType);

    Task<HostFileOperationResult> PickCsvFileAsync();
}
