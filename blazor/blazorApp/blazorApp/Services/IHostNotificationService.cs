namespace blazorApp.Services;

public interface IHostNotificationService
{
    Task PrepareAsync();

    Task NotifyVaultLockedAsync(string title, string message);
}
