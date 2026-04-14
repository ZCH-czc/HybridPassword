using System.Text;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Storage;

namespace blazorApp.Services;

public sealed class HostFileDialogService : IHostFileDialogService
{
    private readonly IFileSaver _fileSaver;

    public HostFileDialogService(IFileSaver fileSaver)
    {
        _fileSaver = fileSaver;
    }

    public async Task<HostFileOperationResult> SaveTextFileAsync(string fileName, string content, string mimeType)
    {
        try
        {
#if ANDROID
            var permissionGranted = await EnsureAndroidStoragePermissionAsync();
            if (!permissionGranted)
            {
                return new HostFileOperationResult
                {
                    Success = false,
                    Message = "Storage permission is required before saving files.",
                };
            }
#endif

            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content ?? string.Empty));
            var saveResult = await _fileSaver.SaveAsync(fileName, stream, CancellationToken.None);

            if (!saveResult.IsSuccessful)
            {
                return new HostFileOperationResult
                {
                    Success = false,
                    Cancelled = saveResult.Exception is null,
                    Message = saveResult.Exception?.Message ?? "File saving was cancelled.",
                    FileName = fileName,
                    FilePath = saveResult.FilePath ?? string.Empty,
                };
            }

            return new HostFileOperationResult
            {
                Success = true,
                Message = "File saved.",
                FileName = fileName,
                FilePath = saveResult.FilePath ?? string.Empty,
            };
        }
        catch (Exception exception)
        {
            return new HostFileOperationResult
            {
                Success = false,
                Message = exception.Message,
                FileName = fileName,
            };
        }
    }

    public async Task<HostFileOperationResult> PickImportFileAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choose a file to import",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    {
                        DevicePlatform.Android,
                        new[]
                        {
                            "text/csv",
                            "text/comma-separated-values",
                            "application/csv",
                            "application/zip",
                            "application/x-zip-compressed",
                            "application/octet-stream",
                        }
                    },
                    { DevicePlatform.WinUI, new[] { ".csv", ".1pux", ".zip" } },
                    { DevicePlatform.iOS, new[] { "public.comma-separated-values-text", "public.zip-archive" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.comma-separated-values-text", "public.zip-archive" } },
                }),
            });

            if (result is null)
            {
                return new HostFileOperationResult
                {
                    Success = false,
                    Cancelled = true,
                    Message = "File selection was cancelled.",
                };
            }

            await using var stream = await result.OpenReadAsync();
            await using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            var extension = Path.GetExtension(result.FileName);
            var isCsv = string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase);

            return new HostFileOperationResult
            {
                Success = true,
                Message = "File loaded.",
                FileName = result.FileName ?? string.Empty,
                FilePath = result.FullPath ?? string.Empty,
                Content = isCsv ? Encoding.UTF8.GetString(bytes) : string.Empty,
                ContentBase64 = Convert.ToBase64String(bytes),
            };
        }
        catch (Exception exception)
        {
            return new HostFileOperationResult
            {
                Success = false,
                Message = exception.Message,
            };
        }
    }

#if ANDROID
    private static async Task<bool> EnsureAndroidStoragePermissionAsync()
    {
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
        {
            return true;
        }

        var readPermission = await Permissions.RequestAsync<Permissions.StorageRead>();
        var writePermission = await Permissions.RequestAsync<Permissions.StorageWrite>();

        return readPermission == PermissionStatus.Granted &&
               writePermission == PermissionStatus.Granted;
    }
#endif
}
