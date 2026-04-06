namespace blazorApp.Services;

public interface IHostWebEventService
{
    void Attach(Microsoft.Maui.Controls.HybridWebView hybridWebView);

    Task RequestVaultLockAsync(string reason, string message);

    Task FlushPendingAsync();
}
