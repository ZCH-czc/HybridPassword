using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System.IO;

#if WINDOWS
using System.Runtime.InteropServices;
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using Microsoft.Windows.AppLifecycle;
using WinRT.Interop;
#endif

#if ANDROID
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Net;
using Android.OS;
using Android.Provider;
using AndroidX.Core.View;
#endif

namespace blazorApp.Services;

public sealed class HostPlatformService : IHostPlatformService, IDisposable
{
    internal const string AndroidExcludeFromRecentsKey = "password_vault.android.exclude_from_recents";

    private const string WindowsMinimizeToTrayKey = "password_vault.windows.minimize_to_tray";
    private const string WindowsLaunchAtStartupKey = "password_vault.windows.launch_at_startup";
    private const string WindowsStartupTaskId = "PasswordVaultStartup";

    private Microsoft.Maui.Controls.Window? _mauiWindow;

#if WINDOWS
    private const int GwlWndProc = -4;
    private const uint TrayIconId = 1001;
    private const uint TrayCallbackMessage = 0x0400 + 1;
    private const uint WmLButtonUp = 0x0202;
    private const uint WmLButtonDoubleClick = 0x0203;
    private const uint WmRButtonUp = 0x0205;
    private const uint NimAdd = 0x00000000;
    private const uint NimDelete = 0x00000002;
    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;
    private const uint MfString = 0x00000000;
    private const uint MfSeparator = 0x00000800;
    private const uint TpmLeftAlign = 0x0000;
    private const uint TpmBottomAlign = 0x0020;
    private const uint TpmReturnCmd = 0x0100;
    private const uint ImageIcon = 1;
    private const uint LrLoadFromFile = 0x00000010;
    private const uint LrDefaultSize = 0x00000040;
    private const int SwHide = 0;
    private const int SwRestore = 9;
    private const int RestoreMenuId = 2001;
    private const int ExitMenuId = 2002;
    private static readonly IntPtr IdiApplication = new(32512);

    private MauiWinUIWindow? _nativeWindow;
    private AppWindow? _appWindow;
    private IntPtr _windowHandle = IntPtr.Zero;
    private IntPtr _previousWindowProc = IntPtr.Zero;
    private IntPtr _trayIconHandle = IntPtr.Zero;
    private TrayWindowProc? _trayWindowProc;
    private bool _trayIconVisible;
    private bool _exitRequested;
    private bool _ownsTrayIconHandle;
#endif

    public void AttachWindow(Microsoft.Maui.Controls.Window window)
    {
        _mauiWindow = window;
        window.HandlerChanged -= OnWindowHandlerChanged;
        window.HandlerChanged += OnWindowHandlerChanged;
        window.Destroying -= OnWindowDestroying;
        window.Destroying += OnWindowDestroying;
    }

    public async Task EnrichHostBridgeStateAsync(HostBridgeState state)
    {
        state.SafeAreaTop = 0;
        state.SafeAreaBottom = 0;

#if WINDOWS
        state.SupportsMinimizeToTray = true;
        state.MinimizeToTrayEnabled = Preferences.Default.Get(WindowsMinimizeToTrayKey, false);
        state.SupportsLaunchAtStartup = true;
        state.LaunchAtStartupEnabled = Preferences.Default.Get(WindowsLaunchAtStartupKey, false);
#elif ANDROID
        state.SupportsExcludeFromRecents = true;
        state.ExcludeFromRecentsEnabled = Preferences.Default.Get(AndroidExcludeFromRecentsKey, false);
        state.SupportsAutostartSettingsShortcut = true;

        AndroidHostPlatformBootstrap.ConfigureActivity(Platform.CurrentActivity);

        var safeArea = AndroidHostPlatformBootstrap.GetSafeAreaInsets(Platform.CurrentActivity);
        state.SafeAreaTop = safeArea.Top;
        state.SafeAreaBottom = safeArea.Bottom;
#endif

        await Task.CompletedTask;
    }

    public Task<HostOperationResult> SetMinimizeToTrayAsync(bool enabled)
    {
#if WINDOWS
        Preferences.Default.Set(WindowsMinimizeToTrayKey, enabled);

        return Task.FromResult(new HostOperationResult
        {
            Success = true,
            Message = enabled ? "关闭窗口时将收纳到系统托盘。" : "关闭窗口时将直接退出应用。",
        });
#else
        return Task.FromResult(BuildUnsupportedResult("当前平台不支持托盘收纳。"));
#endif
    }

    public async Task<HostOperationResult> SetLaunchAtStartupAsync(bool enabled)
    {
#if WINDOWS
        try
        {
            await EnsureWindowsStartupRegistrationAsync(enabled);
            Preferences.Default.Set(WindowsLaunchAtStartupKey, enabled);

            return new HostOperationResult
            {
                Success = true,
                Message = enabled ? "已启用开机自启动。" : "已关闭开机自启动。",
            };
        }
        catch (Exception ex)
        {
            return new HostOperationResult
            {
                Success = false,
                Message = $"更新开机自启动设置失败：{ex.Message}",
            };
        }
#else
        return BuildUnsupportedResult("当前平台不支持开机自启动设置。");
#endif
    }

    public Task<HostOperationResult> SetExcludeFromRecentsAsync(bool enabled)
    {
#if ANDROID
        Preferences.Default.Set(AndroidExcludeFromRecentsKey, enabled);
        var applied = AndroidHostPlatformBootstrap.ApplyExcludeFromRecents(Platform.CurrentActivity, enabled);

        return Task.FromResult(new HostOperationResult
        {
            Success = true,
            Message = applied
                ? (enabled ? "已从最近任务中隐藏当前应用。" : "应用已重新显示在最近任务中。")
                : "设置已保存，应用在当前设备上稍后会按此偏好生效。",
        });
#else
        return Task.FromResult(BuildUnsupportedResult("当前平台不支持最近任务卡片设置。"));
#endif
    }

    public Task<HostOperationResult> OpenAutostartSettingsAsync()
    {
#if ANDROID
        return AndroidHostPlatformBootstrap.OpenAutostartSettingsAsync();
#else
        return Task.FromResult(BuildUnsupportedResult("当前平台没有自启动设置入口。"));
#endif
    }

    public void Dispose()
    {
#if WINDOWS
        RemoveTrayIcon();
        ReleaseTrayIconHandle();
        UnhookWindowProcedure();

        if (_appWindow is not null)
        {
            _appWindow.Closing -= OnAppWindowClosing;
            _appWindow = null;
        }
#endif

        if (_mauiWindow is not null)
        {
            _mauiWindow.HandlerChanged -= OnWindowHandlerChanged;
            _mauiWindow.Destroying -= OnWindowDestroying;
        }
    }

    private static HostOperationResult BuildUnsupportedResult(string message)
    {
        return new HostOperationResult
        {
            Success = false,
            Message = message,
        };
    }

    private void OnWindowHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        if (sender is Microsoft.Maui.Controls.Window window)
        {
            ConfigureWindowsWindow(window);
            _ = EnsureWindowsStartupRegistrationSilentlyAsync();
        }
#endif
    }

    private void OnWindowDestroying(object? sender, EventArgs e)
    {
        Dispose();
    }

#if WINDOWS
    private void ConfigureWindowsWindow(Microsoft.Maui.Controls.Window window)
    {
        if (window.Handler?.PlatformView is not MauiWinUIWindow nativeWindow)
        {
            return;
        }

        _nativeWindow = nativeWindow;
        _windowHandle = WindowNative.GetWindowHandle(nativeWindow);

        if (_appWindow is not null)
        {
            _appWindow.Closing -= OnAppWindowClosing;
        }

        _appWindow = nativeWindow.AppWindow;
        _appWindow.Closing += OnAppWindowClosing;

        HookWindowProcedure();
    }

    private async Task EnsureWindowsStartupRegistrationSilentlyAsync()
    {
        try
        {
            await EnsureWindowsStartupRegistrationAsync(
                Preferences.Default.Get(WindowsLaunchAtStartupKey, false));
        }
        catch
        {
        }
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_exitRequested || !Preferences.Default.Get(WindowsMinimizeToTrayKey, false))
        {
            RemoveTrayIcon();
            return;
        }

        args.Cancel = true;
        MinimizeToTray();
    }

    private void MinimizeToTray()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        AddTrayIcon();
        ShowWindow(_windowHandle, SwHide);
    }

    private void RestoreFromTray()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_windowHandle != IntPtr.Zero)
            {
                ShowWindow(_windowHandle, SwRestore);
                SetForegroundWindow(_windowHandle);
            }

            _nativeWindow?.Activate();
            RemoveTrayIcon();
        });
    }

    private void ExitApplication()
    {
        _exitRequested = true;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            RemoveTrayIcon();
            _nativeWindow?.Close();
        });
    }

    private void AddTrayIcon()
    {
        if (_trayIconVisible || _windowHandle == IntPtr.Zero)
        {
            return;
        }

        var data = CreateNotifyIconData();
        Shell_NotifyIcon(NimAdd, ref data);
        _trayIconVisible = true;
    }

    private void RemoveTrayIcon()
    {
        if (!_trayIconVisible || _windowHandle == IntPtr.Zero)
        {
            return;
        }

        var data = CreateNotifyIconData();
        Shell_NotifyIcon(NimDelete, ref data);
        _trayIconVisible = false;
    }

    private NOTIFYICONDATA CreateNotifyIconData()
    {
        return new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _windowHandle,
            uID = TrayIconId,
            uFlags = NifMessage | NifIcon | NifTip,
            uCallbackMessage = TrayCallbackMessage,
            hIcon = ResolveTrayIconHandle(),
            szTip = "Password Vault",
        };
    }

    private IntPtr ResolveTrayIconHandle()
    {
        if (_trayIconHandle != IntPtr.Zero)
        {
            return _trayIconHandle;
        }

        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "appicon.ico");
            if (File.Exists(iconPath))
            {
                var loadedIcon = LoadImage(
                    IntPtr.Zero,
                    iconPath,
                    ImageIcon,
                    0,
                    0,
                    LrLoadFromFile | LrDefaultSize);

                if (loadedIcon != IntPtr.Zero)
                {
                    _trayIconHandle = loadedIcon;
                    _ownsTrayIconHandle = true;
                    return _trayIconHandle;
                }
            }
        }
        catch
        {
        }

        _trayIconHandle = LoadIcon(IntPtr.Zero, IdiApplication);
        _ownsTrayIconHandle = false;
        return _trayIconHandle;
    }

    private void ReleaseTrayIconHandle()
    {
        if (!_ownsTrayIconHandle || _trayIconHandle == IntPtr.Zero)
        {
            _trayIconHandle = IntPtr.Zero;
            _ownsTrayIconHandle = false;
            return;
        }

        DestroyIcon(_trayIconHandle);
        _trayIconHandle = IntPtr.Zero;
        _ownsTrayIconHandle = false;
    }

    private void ShowTrayMenu()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        var menu = CreatePopupMenu();
        if (menu == IntPtr.Zero)
        {
            return;
        }

        try
        {
            AppendMenu(menu, MfString, RestoreMenuId, "显示主窗口");
            AppendMenu(menu, MfSeparator, 0, null);
            AppendMenu(menu, MfString, ExitMenuId, "退出");

            GetCursorPos(out var point);
            SetForegroundWindow(_windowHandle);

            var command = TrackPopupMenuEx(
                menu,
                TpmLeftAlign | TpmBottomAlign | TpmReturnCmd,
                point.X,
                point.Y,
                _windowHandle,
                IntPtr.Zero);

            switch (command.ToInt32())
            {
                case RestoreMenuId:
                    RestoreFromTray();
                    break;
                case ExitMenuId:
                    ExitApplication();
                    break;
            }
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    private void HookWindowProcedure()
    {
        if (_windowHandle == IntPtr.Zero || _previousWindowProc != IntPtr.Zero)
        {
            return;
        }

        _trayWindowProc = WindowProcedure;
        _previousWindowProc = SetWindowLongPtr(
            _windowHandle,
            GwlWndProc,
            Marshal.GetFunctionPointerForDelegate(_trayWindowProc));
    }

    private void UnhookWindowProcedure()
    {
        if (_windowHandle == IntPtr.Zero || _previousWindowProc == IntPtr.Zero)
        {
            return;
        }

        SetWindowLongPtr(_windowHandle, GwlWndProc, _previousWindowProc);
        _previousWindowProc = IntPtr.Zero;
        _trayWindowProc = null;
    }

    private IntPtr WindowProcedure(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == TrayCallbackMessage)
        {
            var mouseMessage = unchecked((uint)lParam.ToInt64());
            if (mouseMessage is WmLButtonUp or WmLButtonDoubleClick)
            {
                RestoreFromTray();
                return IntPtr.Zero;
            }

            if (mouseMessage == WmRButtonUp)
            {
                ShowTrayMenu();
                return IntPtr.Zero;
            }
        }

        return CallWindowProc(_previousWindowProc, hWnd, message, wParam, lParam);
    }

    private static Task EnsureWindowsStartupRegistrationAsync(bool enabled)
    {
        return Task.Run(() =>
        {
            if (enabled)
            {
                ActivationRegistrationManager.RegisterForStartupActivation(
                    WindowsStartupTaskId,
                    string.Empty);
            }
            else
            {
                ActivationRegistrationManager.UnregisterForStartupActivation(WindowsStartupTaskId);
            }
        });
    }

    private delegate IntPtr TrayWindowProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;

        public uint uVersionOrTimeout;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;

        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadImage(
        IntPtr hInst,
        string lpszName,
        uint uType,
        int cxDesired,
        int cyDesired,
        uint fuLoad);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, int uIdNewItem, string? lpNewItem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr TrackPopupMenuEx(
        IntPtr hMenu,
        uint uFlags,
        int x,
        int y,
        IntPtr hwnd,
        IntPtr lptpm);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(
        IntPtr lpPrevWndFunc,
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }
#endif
}

#if ANDROID
internal static class AndroidHostPlatformBootstrap
{
    public static void ConfigureActivity(Activity? activity)
    {
        if (activity?.Window is null)
        {
            return;
        }

        WindowCompat.SetDecorFitsSystemWindows(activity.Window, false);
        activity.Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
        activity.Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            activity.Window.NavigationBarContrastEnforced = false;
        }

        ApplyExcludeFromRecents(
            activity,
            Preferences.Default.Get(HostPlatformService.AndroidExcludeFromRecentsKey, false));
    }

    public static SafeAreaInsets GetSafeAreaInsets(Activity? activity)
    {
        if (activity?.Window?.DecorView is null)
        {
            return new SafeAreaInsets(0, 0);
        }

        var rootInsets = ViewCompat.GetRootWindowInsets(activity.Window.DecorView);
        if (rootInsets is not null)
        {
            var systemBarInsets = rootInsets.GetInsets(WindowInsetsCompat.Type.SystemBars());
            var gestureInsets = rootInsets.GetInsets(WindowInsetsCompat.Type.SystemGestures());

            return new SafeAreaInsets(
                systemBarInsets.Top,
                Math.Max(systemBarInsets.Bottom, gestureInsets.Bottom));
        }

        return new SafeAreaInsets(
            ResolveDimensionInPixels(activity, "status_bar_height"),
            ResolveDimensionInPixels(activity, "navigation_bar_height"));
    }

    public static bool ApplyExcludeFromRecents(Activity? activity, bool enabled)
    {
        if (activity is null)
        {
            return false;
        }

        try
        {
            var activityManager =
                activity.GetSystemService(Android.Content.Context.ActivityService) as ActivityManager;
            var appTasks = activityManager?.AppTasks;
            if (appTasks is null)
            {
                return false;
            }

            foreach (var appTask in appTasks)
            {
                var taskInfo = appTask?.TaskInfo;
                if (taskInfo?.Id == activity.TaskId)
                {
                    appTask!.SetExcludeFromRecents(enabled);
                    return true;
                }
            }
        }
        catch
        {
        }

        return false;
    }

    public static Task<HostOperationResult> OpenAutostartSettingsAsync()
    {
        var taskCompletionSource = new TaskCompletionSource<HostOperationResult>();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var activity = Platform.CurrentActivity;
            if (activity is null)
            {
                taskCompletionSource.TrySetResult(new HostOperationResult
                {
                    Success = false,
                    Message = "当前 Android 宿主尚未就绪，暂时无法打开系统设置。",
                });
                return;
            }

            var packageUri = Android.Net.Uri.Parse($"package:{activity.PackageName}");
            var intents = new[]
            {
                new Intent().SetAction(Settings.ActionRequestIgnoreBatteryOptimizations).SetData(packageUri),
                new Intent().SetAction(Settings.ActionIgnoreBatteryOptimizationSettings),
                new Intent().SetAction(Settings.ActionApplicationDetailsSettings).SetData(packageUri),
            };

            foreach (var intent in intents)
            {
                if (TryStartIntent(activity, intent))
                {
                    taskCompletionSource.TrySetResult(new HostOperationResult
                    {
                        Success = true,
                        Message = "已打开系统相关设置，请在系统中允许自启动、后台运行或关闭电池限制。",
                    });
                    return;
                }
            }

            taskCompletionSource.TrySetResult(new HostOperationResult
            {
                Success = false,
                Message = "当前设备没有可用的系统设置入口，请在系统设置中手动允许自启动或后台运行。",
            });
        });

        return taskCompletionSource.Task;
    }

    private static bool TryStartIntent(Activity activity, Intent intent)
    {
        try
        {
            intent.AddFlags(ActivityFlags.NewTask);

            if (intent.ResolveActivity(activity.PackageManager) is null)
            {
                return false;
            }

            activity.StartActivity(intent);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int ResolveDimensionInPixels(Activity activity, string name)
    {
        try
        {
            var resourceId = activity.Resources?.GetIdentifier(name, "dimen", "android") ?? 0;
            if (resourceId <= 0 || activity.Resources is null)
            {
                return 0;
            }

            return activity.Resources.GetDimensionPixelSize(resourceId);
        }
        catch
        {
            return 0;
        }
    }

    internal readonly record struct SafeAreaInsets(int Top, int Bottom);
}
#endif
