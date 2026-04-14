using blazorApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace blazorApp;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private int _passkeyCompanionCleanupTriggered;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

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
        window.Destroying += OnWindowDestroying;
        _ = _serviceProvider.GetRequiredService<IPasskeyHostService>().EnsureCompanionOnHostStartAsync();
        return window;
    }

    protected override void CleanUp()
    {
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        TryCleanupPasskeyCompanion();
        base.CleanUp();
    }

    private void OnWindowDestroying(object? sender, EventArgs e)
    {
        TryCleanupPasskeyCompanion();
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        TryCleanupPasskeyCompanion();
    }

    private void TryCleanupPasskeyCompanion()
    {
        if (Interlocked.Exchange(ref _passkeyCompanionCleanupTriggered, 1) != 0)
        {
            return;
        }

        try
        {
            _serviceProvider
                .GetRequiredService<IPasskeyHostService>()
                .CleanupCompanionOnHostExitAsync()
                .GetAwaiter()
                .GetResult();
        }
        catch
        {
        }
    }
}
