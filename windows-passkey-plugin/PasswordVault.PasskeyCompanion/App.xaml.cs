using PasswordVault.PasskeyCompanion.Models;
using PasswordVault.PasskeyCompanion.Services;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace PasswordVault.PasskeyCompanion;

public partial class App : Application
{
    private const string SingleInstanceMutexName = "PasswordVault.PasskeyCompanion.Singleton";
    private static readonly TimeSpan ParentMonitorInterval = TimeSpan.FromSeconds(2);

    private CompanionIpcServer? _ipcServer;
    private Mutex? _instanceMutex;
    private bool _ownsMutex;
    private CancellationTokenSource? _parentMonitorCts;
    private Task? _parentMonitorTask;
    private CompanionStatusService? _statusService;
    private PluginRegistrationService? _pluginRegistrationService;
    private PluginActivationService? _pluginActivationService;
    private CompanionRuntimeState _runtimeState = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _instanceMutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            HandleExistingInstanceRequest(e.Args);
            Shutdown();
            return;
        }

        _ownsMutex = true;

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var hostSessionLink = TryParseHostSessionLink(e.Args);
        if (hostSessionLink is not null)
        {
            var isParentAlive = IsExpectedHostProcessAlive(hostSessionLink);
            _runtimeState = BuildRuntimeState(hostSessionLink, isParentAlive);

            if (!isParentAlive)
            {
                Shutdown();
                return;
            }
        }

        _statusService = new CompanionStatusService(() => _runtimeState);
        _pluginActivationService = new PluginActivationService();
        _pluginRegistrationService = new PluginRegistrationService(_statusService, _pluginActivationService);

        if (IsPluginActivationRequested(e.Args))
        {
            _ = _pluginRegistrationService.HandlePluginActivation(
                "startup-activation",
                isStartupActivation: true);
        }

        if (ShouldShowStatusWindow(e.Args))
        {
            ShowStatusWindow();
        }

        if (hostSessionLink is not null)
        {
            StartParentMonitor(hostSessionLink);
        }

        _ipcServer = new CompanionIpcServer(
            _statusService,
            _pluginRegistrationService,
            ActivateMainWindow,
            ShowStatusWindow,
            ShutdownCompanion);
        _ipcServer.Start();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_ipcServer is not null)
        {
            await _ipcServer.DisposeAsync();
            _ipcServer = null;
        }

        _pluginRegistrationService?.Dispose();
        _pluginRegistrationService = null;

        if (_parentMonitorCts is not null)
        {
            _parentMonitorCts.Cancel();
        }

        if (_parentMonitorTask is not null)
        {
            try
            {
                await _parentMonitorTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _parentMonitorCts?.Dispose();
        _parentMonitorCts = null;
        _parentMonitorTask = null;

        if (_ownsMutex)
        {
            _instanceMutex?.ReleaseMutex();
            _ownsMutex = false;
        }
        _instanceMutex?.Dispose();
        _instanceMutex = null;

        base.OnExit(e);
    }

    private void ActivateMainWindow()
    {
        Dispatcher.Invoke(() =>
        {
            if (MainWindow is null)
            {
                return;
            }

            if (!MainWindow.IsVisible)
            {
                MainWindow.Show();
            }

            if (MainWindow.WindowState == WindowState.Minimized)
            {
                MainWindow.WindowState = WindowState.Normal;
            }

            MainWindow.Activate();
            MainWindow.Topmost = true;
            MainWindow.Topmost = false;
            MainWindow.Focus();
        }, DispatcherPriority.Normal);
    }

    private void HandleExistingInstanceRequest(string[] args)
    {
        try
        {
            if (ShouldShowStatusWindow(args))
            {
                SendCompanionCommand("show_status_window");
                return;
            }

            if (IsPluginActivationRequested(args))
            {
                SendCompanionCommand("plugin_activated");
                return;
            }

            SendCompanionCommand("activate");
        }
        catch
        {
        }
    }

    private void ShowStatusWindow()
    {
        Dispatcher.Invoke(() =>
        {
            if (MainWindow is MainWindow existingWindow)
            {
                if (!existingWindow.IsVisible)
                {
                    existingWindow.Show();
                }

                if (existingWindow.WindowState == WindowState.Minimized)
                {
                    existingWindow.WindowState = WindowState.Normal;
                }

                existingWindow.Activate();
                existingWindow.Focus();
                return;
            }

            var window = new MainWindow(_statusService ?? new CompanionStatusService());
            MainWindow = window;
            window.Show();
        }, DispatcherPriority.Normal);
    }

    private void ShutdownCompanion()
    {
        _ = Dispatcher.BeginInvoke(() => Shutdown(), DispatcherPriority.ApplicationIdle);
    }

    private static void SendCompanionCommand(string action)
    {
        using var client = new NamedPipeClientStream(
            ".",
            CompanionIpcServer.PipeName,
            PipeDirection.InOut,
            PipeOptions.None);

        client.Connect(500);

        using var writer = new StreamWriter(client, new UTF8Encoding(false), 1024, leaveOpen: true)
        {
            AutoFlush = true,
        };
        using var reader = new StreamReader(client, Encoding.UTF8, false, 1024, leaveOpen: true);

        var payload = JsonSerializer.Serialize(new { action });
        writer.WriteLine(payload);
        _ = reader.ReadLine();
    }

    private void StartParentMonitor(HostSessionLink hostSessionLink)
    {
        _parentMonitorCts?.Cancel();
        _parentMonitorCts?.Dispose();
        _parentMonitorCts = new CancellationTokenSource();
        var cancellationToken = _parentMonitorCts.Token;

        _parentMonitorTask = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var isAlive = IsExpectedHostProcessAlive(hostSessionLink);
                _runtimeState = BuildRuntimeState(hostSessionLink, isAlive);

                if (!isAlive)
                {
                    _ = Dispatcher.BeginInvoke(() => Shutdown(), DispatcherPriority.ApplicationIdle);
                    break;
                }

                await Task.Delay(ParentMonitorInterval, cancellationToken).ConfigureAwait(false);
            }
        }, cancellationToken);
    }

    private static bool ShouldShowStatusWindow(string[] args)
    {
        return args.Any(arg => string.Equals(arg, "--show-status", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPluginActivationRequested(string[] args)
    {
        return args.Any(arg =>
            string.Equals(arg, "--plugin-activated", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "-PluginActivated", StringComparison.OrdinalIgnoreCase));
    }

    private static HostSessionLink? TryParseHostSessionLink(string[] args)
    {
        if (!TryReadArgumentValue(args, "--parent-pid", out var parentPidText) ||
            !TryReadArgumentValue(args, "--parent-start-ticks", out var parentStartTicksText) ||
            !int.TryParse(parentPidText, out var parentPid) ||
            !long.TryParse(parentStartTicksText, out var parentStartTicks) ||
            parentPid <= 0 ||
            parentStartTicks <= 0)
        {
            return null;
        }

        return new HostSessionLink(parentPid, parentStartTicks);
    }

    private static bool TryReadArgumentValue(string[] args, string flag, out string value)
    {
        for (var index = 0; index < args.Length - 1; index += 1)
        {
            if (!string.Equals(args[index], flag, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = args[index + 1];
            return !string.IsNullOrWhiteSpace(value);
        }

        value = string.Empty;
        return false;
    }

    private static bool IsExpectedHostProcessAlive(HostSessionLink hostSessionLink)
    {
        try
        {
            using var process = Process.GetProcessById(hostSessionLink.ProcessId);
            if (process.HasExited)
            {
                return false;
            }

            return process.StartTime.ToUniversalTime().Ticks == hostSessionLink.StartTimeUtcTicks;
        }
        catch
        {
            return false;
        }
    }

    private static CompanionRuntimeState BuildRuntimeState(HostSessionLink? hostSessionLink, bool isParentAlive)
    {
        return new CompanionRuntimeState
        {
            HasHostSessionLink = hostSessionLink is not null,
            HostProcessId = hostSessionLink?.ProcessId ?? 0,
            HostStartTimeUtcTicks = hostSessionLink?.StartTimeUtcTicks ?? 0,
            HostProcessAlive = isParentAlive,
        };
    }

    private sealed record HostSessionLink(int ProcessId, long StartTimeUtcTicks);
}
