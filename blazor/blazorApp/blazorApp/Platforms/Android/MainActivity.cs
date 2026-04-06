using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using blazorApp.Services;
using Microsoft.Maui;

namespace blazorApp;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        AndroidHostPlatformBootstrap.ConfigureActivity(this);
    }

    protected override void OnStop()
    {
        base.OnStop();
        ResolveAutoLockService()?.NotifyAppBackgrounded();
    }

    protected override void OnResume()
    {
        base.OnResume();
        ResolveAutoLockService()?.NotifyAppForegrounded();
    }

    private static IHostAutoLockService? ResolveAutoLockService()
    {
        return IPlatformApplication.Current?.Services?.GetService(typeof(IHostAutoLockService)) as IHostAutoLockService;
    }
}
