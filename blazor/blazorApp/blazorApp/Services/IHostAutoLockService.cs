namespace blazorApp.Services;

public interface IHostAutoLockService
{
    int TrayAutoLockMinutes { get; }

    int BackgroundAutoLockMinutes { get; }

    Task<HostOperationResult> SetTrayAutoLockMinutesAsync(int minutes);

    Task<HostOperationResult> SetBackgroundAutoLockMinutesAsync(int minutes);

    void NotifyTrayHidden();

    void NotifyTrayVisible();

    void NotifyAppBackgrounded();

    void NotifyAppForegrounded();
}
