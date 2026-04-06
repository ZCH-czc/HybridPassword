using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using blazorApp.Services;
using Microsoft.Extensions.Logging;

namespace blazorApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddSingleton<IBiometricUnlockService, BiometricUnlockService>();
        builder.Services.AddSingleton<IFileSaver>(FileSaver.Default);
        builder.Services.AddSingleton<IHostFileDialogService, HostFileDialogService>();
        builder.Services.AddSingleton<IHostWebEventService, HostWebEventService>();
        builder.Services.AddSingleton<IHostNotificationService, HostNotificationService>();
        builder.Services.AddSingleton<IHostAutoLockService, HostAutoLockService>();
        builder.Services.AddSingleton<IHostPlatformService, HostPlatformService>();
        builder.Services.AddSingleton<IHostSyncService, HostSyncService>();
        builder.Services.AddSingleton<PasswordVaultHostBridge>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
