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
                    Message = "保存文件前需要系统存储权限。",
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
                    Message = saveResult.Exception?.Message ?? "文件保存已取消。",
                    FileName = fileName,
                    FilePath = saveResult.FilePath ?? string.Empty,
                };
            }

            return new HostFileOperationResult
            {
                Success = true,
                Message = "文件已保存。",
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

    public async Task<HostFileOperationResult> PickCsvFileAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "选择要导入的 CSV 文件",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "text/csv", "text/comma-separated-values", "application/csv" } },
                    { DevicePlatform.WinUI, new[] { ".csv" } },
                    { DevicePlatform.iOS, new[] { "public.comma-separated-values-text" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.comma-separated-values-text" } },
                }),
            });

            if (result is null)
            {
                return new HostFileOperationResult
                {
                    Success = false,
                    Cancelled = true,
                    Message = "已取消选择文件。",
                };
            }

            await using var stream = await result.OpenReadAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            var content = await reader.ReadToEndAsync();

            return new HostFileOperationResult
            {
                Success = true,
                Message = "文件已读取。",
                FileName = result.FileName ?? string.Empty,
                FilePath = result.FullPath ?? string.Empty,
                Content = content,
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
