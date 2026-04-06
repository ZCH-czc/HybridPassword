using Microsoft.Maui.Storage;
using System.Globalization;

namespace blazorApp.Services;

public sealed class HostAutoLockService : IHostAutoLockService
{
    private const string WindowsTrayAutoLockKey = "password_vault.windows.tray_auto_lock_minutes";
    private const string AndroidBackgroundAutoLockKey = "password_vault.android.background_auto_lock_minutes";

    private readonly IHostNotificationService _notificationService;
    private readonly IHostWebEventService _hostWebEventService;
    private readonly object _timerSync = new();

    private CancellationTokenSource? _lockCountdownCts;

    public HostAutoLockService(
        IHostNotificationService notificationService,
        IHostWebEventService hostWebEventService)
    {
        _notificationService = notificationService;
        _hostWebEventService = hostWebEventService;
    }

    public int TrayAutoLockMinutes => Math.Max(0, Preferences.Default.Get(WindowsTrayAutoLockKey, 0));

    public int BackgroundAutoLockMinutes => Math.Max(0, Preferences.Default.Get(AndroidBackgroundAutoLockKey, 0));

    public async Task<HostOperationResult> SetTrayAutoLockMinutesAsync(int minutes)
    {
        var normalizedMinutes = NormalizeMinutes(minutes);
        Preferences.Default.Set(WindowsTrayAutoLockKey, normalizedMinutes);

        if (normalizedMinutes > 0)
        {
            await _notificationService.PrepareAsync();
        }

        return new HostOperationResult
        {
            Success = true,
            Message = GetDelayUpdatedMessage(
                normalizedMinutes,
                "收纳到托盘后自动锁定时间已更新。",
                "Tray auto-lock delay updated."),
        };
    }

    public async Task<HostOperationResult> SetBackgroundAutoLockMinutesAsync(int minutes)
    {
        var normalizedMinutes = NormalizeMinutes(minutes);
        Preferences.Default.Set(AndroidBackgroundAutoLockKey, normalizedMinutes);

        if (normalizedMinutes > 0)
        {
            await _notificationService.PrepareAsync();
        }

        return new HostOperationResult
        {
            Success = true,
            Message = GetDelayUpdatedMessage(
                normalizedMinutes,
                "后台自动锁定时间已更新。",
                "Background auto-lock delay updated."),
        };
    }

    public void NotifyTrayHidden()
    {
#if WINDOWS
        ScheduleAutoLock(
            TrayAutoLockMinutes,
            "tray",
            "已在托盘中自动锁定",
            "Password Vault locked in tray",
            minutes => $"Password Vault 已在托盘中静置 {minutes} 分钟后自动锁定。",
            minutes => $"Password Vault locked after {minutes} minute(s) in the tray.");
#endif
    }

    public void NotifyTrayVisible()
    {
#if WINDOWS
        CancelCountdown();
        _ = _hostWebEventService.FlushPendingAsync();
#endif
    }

    public void NotifyAppBackgrounded()
    {
#if ANDROID
        ScheduleAutoLock(
            BackgroundAutoLockMinutes,
            "background",
            "已在后台自动锁定",
            "Password Vault locked in background",
            minutes => $"Password Vault 在后台停留 {minutes} 分钟后已自动锁定。",
            minutes => $"Password Vault locked after {minutes} minute(s) in the background.");
#endif
    }

    public void NotifyAppForegrounded()
    {
#if ANDROID
        CancelCountdown();
        _ = _hostWebEventService.FlushPendingAsync();
#endif
    }

    private void ScheduleAutoLock(
        int delayMinutes,
        string reason,
        string zhTitle,
        string enTitle,
        Func<int, string> zhMessageFactory,
        Func<int, string> enMessageFactory)
    {
        CancelCountdown();

        if (delayMinutes <= 0)
        {
            return;
        }

        var cts = new CancellationTokenSource();

        lock (_timerSync)
        {
            _lockCountdownCts = cts;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), cts.Token);

                var useChinese = CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
                var title = useChinese ? zhTitle : enTitle;
                var message = useChinese ? zhMessageFactory(delayMinutes) : enMessageFactory(delayMinutes);

                await _notificationService.NotifyVaultLockedAsync(title, message);
                await _hostWebEventService.RequestVaultLockAsync(reason, message);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                lock (_timerSync)
                {
                    if (_lockCountdownCts == cts)
                    {
                        _lockCountdownCts = null;
                    }
                }

                cts.Dispose();
            }
        });
    }

    private void CancelCountdown()
    {
        CancellationTokenSource? cts;

        lock (_timerSync)
        {
            cts = _lockCountdownCts;
            _lockCountdownCts = null;
        }

        if (cts is null)
        {
            return;
        }

        try
        {
            cts.Cancel();
        }
        catch
        {
        }
        finally
        {
            cts.Dispose();
        }
    }

    private static int NormalizeMinutes(int minutes)
    {
        return minutes switch
        {
            <= 0 => 0,
            <= 1 => 1,
            <= 5 => 5,
            <= 15 => 15,
            <= 30 => 30,
            _ => 60,
        };
    }

    private static string GetDelayUpdatedMessage(int minutes, string zhEnabled, string enEnabled)
    {
        if (minutes <= 0)
        {
            return CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? "已关闭自动锁定计时。"
                : "Auto-lock timer disabled.";
        }

        return CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? zhEnabled
            : enEnabled;
    }
}
