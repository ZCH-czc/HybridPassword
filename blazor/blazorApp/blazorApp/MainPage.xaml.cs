using blazorApp.Services;

namespace blazorApp;

public partial class MainPage : ContentPage
{
    public MainPage(PasswordVaultHostBridge hostBridge)
    {
        InitializeComponent();
        hybridWebView.SetInvokeJavaScriptTarget(hostBridge);
    }
}
