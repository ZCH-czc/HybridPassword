using System.Diagnostics;

namespace blazorApp.Services;

public sealed class PasskeyCompanionLauncherService : IPasskeyCompanionLauncherService
{
    private const string CompanionProcessName = "PasswordVault.PasskeyCompanion";
    private const string CompanionExecutableName = "PasswordVault.PasskeyCompanion.exe";
    private const string CompanionPathEnvironmentVariable = "PASSWORD_VAULT_PASSKEY_COMPANION_PATH";

    private static readonly string[] CandidateRelativePaths =
    [
        CompanionExecutableName,
        Path.Combine("PasskeyCompanion", CompanionExecutableName),
        Path.Combine(
            "windows-passkey-plugin",
            "PasswordVault.PasskeyCompanion",
            "bin",
            "Debug",
            "net10.0-windows10.0.19041.0",
            CompanionExecutableName),
        Path.Combine(
            "windows-passkey-plugin",
            "PasswordVault.PasskeyCompanion",
            "bin",
            "Release",
            "net10.0-windows10.0.19041.0",
            CompanionExecutableName),
        Path.Combine(
            "windows-passkey-plugin",
            "PasswordVault.PasskeyCompanion",
            "bin",
            "Release",
            "net10.0-windows10.0.19041.0",
            "publish",
            CompanionExecutableName),
    ];

#if WINDOWS
    private readonly object _pathLock = new();
    private string? _cachedExecutablePath;
#endif

    private readonly IPasskeyDiagnosticsService _diagnosticsService;
    private readonly IPasskeyCompanionClientService _companionClientService;

    public PasskeyCompanionLauncherService(
        IPasskeyDiagnosticsService diagnosticsService,
        IPasskeyCompanionClientService companionClientService)
    {
        _diagnosticsService = diagnosticsService;
        _companionClientService = companionClientService;
    }

    public bool CanLaunchCompanionApp()
    {
#if WINDOWS
        return !string.IsNullOrWhiteSpace(TryResolveExecutablePath());
#else
        return false;
#endif
    }

    public Task<HostOperationResult> LaunchAsync(CancellationToken cancellationToken = default)
    {
#if WINDOWS
        return LaunchCoreAsync(restartIfNeeded: true, cancellationToken);
#else
        return Task.FromResult(new HostOperationResult
        {
            Success = false,
            Message = "The Windows passkey companion can only be launched on Windows.",
        });
#endif
    }

    public Task<HostOperationResult> RestartAsync(CancellationToken cancellationToken = default)
    {
#if WINDOWS
        return RestartCoreAsync(cancellationToken);
#else
        return Task.FromResult(new HostOperationResult
        {
            Success = false,
            Message = "The Windows passkey companion can only be restarted on Windows.",
        });
#endif
    }

    public Task<HostOperationResult> StopAsync(CancellationToken cancellationToken = default)
    {
#if WINDOWS
        return StopCoreAsync(cancellationToken);
#else
        return Task.FromResult(new HostOperationResult
        {
            Success = false,
            Message = "The Windows passkey companion can only be stopped on Windows.",
        });
#endif
    }

#if WINDOWS
    private async Task<HostOperationResult> LaunchCoreAsync(bool restartIfNeeded, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = await _companionClientService.TryGetStatusAsync(cancellationToken).ConfigureAwait(false);
            if (status.IsReachable)
            {
                _diagnosticsService.AddInfo("companion-launcher", "The Windows passkey companion is already running.");
                return new HostOperationResult
                {
                    Success = true,
                    Message = "The Windows passkey companion is already running in background mode.",
                };
            }

            var executablePath = TryResolveExecutablePath();
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                _diagnosticsService.AddWarning(
                    "companion-launcher",
                    "The Windows passkey companion executable could not be located. Build the companion app first or set PASSWORD_VAULT_PASSKEY_COMPANION_PATH.");
                return new HostOperationResult
                {
                    Success = false,
                    Message =
                        "The Windows passkey companion executable could not be located. Build the companion app first or set PASSWORD_VAULT_PASSKEY_COMPANION_PATH.",
                };
            }

            if (restartIfNeeded && Process.GetProcessesByName(CompanionProcessName).Any())
            {
                _diagnosticsService.AddWarning(
                    "companion-launcher",
                    "A stale Windows passkey companion process was detected. It will be restarted.");
                await StopExistingProcessesAsync(cancellationToken).ConfigureAwait(false);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = BuildLaunchArguments(),
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory,
                UseShellExecute = true,
            };

            Process.Start(startInfo);
            _diagnosticsService.AddInfo(
                "companion-launcher",
                $"The Windows passkey companion was launched from {executablePath}.");

            var becameReady = await WaitForCompanionReadyAsync(cancellationToken).ConfigureAwait(false);
            if (!becameReady)
            {
                _diagnosticsService.AddWarning(
                    "companion-launcher",
                    "The Windows passkey companion started, but the background bridge did not become ready in time.");
                return new HostOperationResult
                {
                    Success = false,
                    Message = "The Windows passkey companion started, but the background bridge did not become ready in time.",
                };
            }

            return new HostOperationResult
            {
                Success = true,
                Message = "The Windows passkey companion was launched in background mode.",
            };
        }
        catch (OperationCanceledException)
        {
            _diagnosticsService.AddWarning("companion-launcher", "Launching the Windows passkey companion was canceled.");
            return new HostOperationResult
            {
                Success = false,
                Message = "Launching the Windows passkey companion was canceled.",
            };
        }
        catch (Exception exception)
        {
            _diagnosticsService.AddError(
                "companion-launcher",
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to launch the Windows passkey companion."
                    : exception.Message);
            return new HostOperationResult
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to launch the Windows passkey companion."
                    : exception.Message,
            };
        }
    }

    private async Task<HostOperationResult> RestartCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _diagnosticsService.AddInfo("companion-launcher", "Restarting the Windows passkey companion.");

            var shutdownResult = await _companionClientService.TryShutdownAsync(cancellationToken).ConfigureAwait(false);
            if (!shutdownResult.Success && Process.GetProcessesByName(CompanionProcessName).Any())
            {
                _diagnosticsService.AddWarning(
                    "companion-launcher",
                    "The Windows passkey companion did not respond to shutdown. Existing processes will be terminated.");
            }

            await StopExistingProcessesAsync(cancellationToken).ConfigureAwait(false);
            return await LaunchCoreAsync(restartIfNeeded: false, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _diagnosticsService.AddWarning("companion-launcher", "Restarting the Windows passkey companion was canceled.");
            return new HostOperationResult
            {
                Success = false,
                Message = "Restarting the Windows passkey companion was canceled.",
            };
        }
        catch (Exception exception)
        {
            _diagnosticsService.AddError(
                "companion-launcher",
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to restart the Windows passkey companion."
                    : exception.Message);
            return new HostOperationResult
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to restart the Windows passkey companion."
                    : exception.Message,
            };
        }
    }

    private async Task<HostOperationResult> StopCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _diagnosticsService.AddInfo("companion-launcher", "Stopping the Windows passkey companion.");

            var shutdownResult = await _companionClientService.TryShutdownAsync(cancellationToken).ConfigureAwait(false);

            for (var attempt = 0; attempt < 10; attempt += 1)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Process.GetProcessesByName(CompanionProcessName).Any())
                {
                    _diagnosticsService.AddInfo(
                        "companion-launcher",
                        "The Windows passkey companion stopped successfully.");
                    return new HostOperationResult
                    {
                        Success = true,
                        Message = "The Windows passkey companion stopped successfully.",
                    };
                }

                await Task.Delay(120, cancellationToken).ConfigureAwait(false);
            }

            if (!shutdownResult.Success)
            {
                _diagnosticsService.AddWarning(
                    "companion-launcher",
                    "The Windows passkey companion did not stop after the shutdown request. Existing processes will be terminated.");
            }

            await StopExistingProcessesAsync(cancellationToken).ConfigureAwait(false);
            return new HostOperationResult
            {
                Success = true,
                Message = "The Windows passkey companion was stopped.",
            };
        }
        catch (OperationCanceledException)
        {
            _diagnosticsService.AddWarning("companion-launcher", "Stopping the Windows passkey companion was canceled.");
            return new HostOperationResult
            {
                Success = false,
                Message = "Stopping the Windows passkey companion was canceled.",
            };
        }
        catch (Exception exception)
        {
            _diagnosticsService.AddError(
                "companion-launcher",
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to stop the Windows passkey companion."
                    : exception.Message);
            return new HostOperationResult
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to stop the Windows passkey companion."
                    : exception.Message,
            };
        }
    }

    private string? TryResolveExecutablePath()
    {
        lock (_pathLock)
        {
            if (!string.IsNullOrWhiteSpace(_cachedExecutablePath) && File.Exists(_cachedExecutablePath))
            {
                return _cachedExecutablePath;
            }

            var explicitPath = Environment.GetEnvironmentVariable(CompanionPathEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
            {
                _cachedExecutablePath = explicitPath;
                return _cachedExecutablePath;
            }

            foreach (var root in EnumerateCandidateRoots())
            {
                foreach (var relativePath in CandidateRelativePaths)
                {
                    var candidatePath = Path.GetFullPath(Path.Combine(root, relativePath));
                    if (!File.Exists(candidatePath))
                    {
                        continue;
                    }

                    _cachedExecutablePath = candidatePath;
                    return _cachedExecutablePath;
                }
            }

            return null;
        }
    }

    private static IEnumerable<string> EnumerateCandidateRoots()
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var basePath in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(basePath);
            for (var depth = 0; current is not null && depth < 10; depth += 1, current = current.Parent)
            {
                if (visited.Add(current.FullName))
                {
                    yield return current.FullName;
                }
            }
        }
    }

    private async Task<bool> WaitForCompanionReadyAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt += 1)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var status = await _companionClientService.TryGetStatusAsync(cancellationToken).ConfigureAwait(false);
            if (status.IsReachable)
            {
                return true;
            }

            await Task.Delay(150, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private async Task StopExistingProcessesAsync(CancellationToken cancellationToken)
    {
        foreach (var process in Process.GetProcessesByName(CompanionProcessName))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (process.HasExited)
                {
                    continue;
                }

                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                _diagnosticsService.AddInfo(
                    "companion-launcher",
                    $"Stopped stale companion process {process.Id}.");
            }
            catch (Exception exception)
            {
                _diagnosticsService.AddWarning(
                    "companion-launcher",
                    string.IsNullOrWhiteSpace(exception.Message)
                        ? $"Unable to stop stale companion process {process.Id}."
                        : exception.Message);
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private static string BuildLaunchArguments()
    {
        using var currentProcess = Process.GetCurrentProcess();
        var parentPid = currentProcess.Id;
        var parentStartTicks = currentProcess.StartTime.ToUniversalTime().Ticks;
        return $"--background --parent-pid {parentPid} --parent-start-ticks {parentStartTicks}";
    }
#endif
}
