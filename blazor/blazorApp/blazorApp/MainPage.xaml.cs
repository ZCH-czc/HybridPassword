using blazorApp.Services;

namespace blazorApp;

public partial class MainPage : ContentPage
{
    public MainPage(PasswordVaultHostBridge hostBridge, IHostWebEventService hostWebEventService)
    {
        InitializeComponent();
        hybridWebView.SetInvokeJavaScriptTarget(hostBridge);
        hostWebEventService.Attach(hybridWebView);
        hybridWebView.WebViewInitialized += async (_, _) => await hostWebEventService.FlushPendingAsync();
    }
}
