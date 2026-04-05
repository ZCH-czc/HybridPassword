using blazorApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace blazorApp;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;

#if WINDOWS
        var userDataFolder = Path.Combine(FileSystem.AppDataDirectory, "WebView2");
        Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", userDataFolder);
#endif
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_serviceProvider.GetRequiredService<MainPage>())
        {
            Title = "Password Vault",
        };

        _serviceProvider.GetRequiredService<IHostPlatformService>().AttachWindow(window);
        return window;
    }
}
