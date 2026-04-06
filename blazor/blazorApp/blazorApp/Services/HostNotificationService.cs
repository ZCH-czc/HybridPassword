using Microsoft.Maui.ApplicationModel;

#if WINDOWS
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
#endif

#if ANDROID
using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
#endif

namespace blazorApp.Services;

public sealed class HostNotificationService : IHostNotificationService, IDisposable
{
#if WINDOWS
    private bool _registered;
#endif

#if ANDROID
    private const string AutoLockChannelId = "password_vault_auto_lock";
    private const int AutoLockNotificationId = 3307;
#endif

    public HostNotificationService()
    {
#if WINDOWS
        try
        {
            AppNotificationManager.Default.Register();
            _registered = true;
        }
        catch
        {
            _registered = false;
        }
#endif

#if ANDROID
        CreateAndroidNotificationChannel();
#endif
    }

    public async Task PrepareAsync()
    {
#if ANDROID
        await EnsureAndroidNotificationPermissionAsync();
#else
        await Task.CompletedTask;
#endif
    }

    public async Task NotifyVaultLockedAsync(string title, string message)
    {
#if WINDOWS
        try
        {
            var notification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
        catch
        {
        }
#elif ANDROID
        await EnsureAndroidNotificationPermissionAsync();

        var activity = Platform.CurrentActivity;
        var context = activity?.ApplicationContext;
        if (context is null)
        {
            return;
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu &&
            ContextCompat.CheckSelfPermission(context, Manifest.Permission.PostNotifications) != Permission.Granted)
        {
            return;
        }

        var notification = new NotificationCompat.Builder(context, AutoLockChannelId)
            .SetContentTitle(title)
            .SetContentText(message)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(message))
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetAutoCancel(true)
            .SetPriority((int)NotificationPriority.Default)
            .SetVisibility((int)NotificationVisibility.Private)
            .Build();

        NotificationManagerCompat.From(context).Notify(AutoLockNotificationId, notification);
#else
        await Task.CompletedTask;
#endif
    }

    public void Dispose()
    {
#if WINDOWS
        if (_registered)
        {
            try
            {
                AppNotificationManager.Default.Unregister();
            }
            catch
            {
            }
        }
#endif
    }

#if ANDROID
    private static void CreateAndroidNotificationChannel()
    {
        var activity = Platform.CurrentActivity;
        var context = activity?.ApplicationContext;
        if (context is null || Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        var manager = context.GetSystemService(Android.Content.Context.NotificationService) as NotificationManager;
        if (manager is null)
        {
            return;
        }

        var channel = new NotificationChannel(
            AutoLockChannelId,
            "Vault security",
            NotificationImportance.Default)
        {
            Description = "Auto-lock reminders for Password Vault",
        };

        manager.CreateNotificationChannel(channel);
    }

    private static async Task EnsureAndroidNotificationPermissionAsync()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
        {
            return;
        }

        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            return;
        }

        if (ContextCompat.CheckSelfPermission(activity, Manifest.Permission.PostNotifications) == Permission.Granted)
        {
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(() => Permissions.RequestAsync<Permissions.PostNotifications>());
        }
        catch
        {
        }
    }
#endif
}
