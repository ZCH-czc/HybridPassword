using PasswordVault.PasskeyCompanion.Models;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace PasswordVault.PasskeyCompanion.Services;

public sealed class CompanionIpcServer : IAsyncDisposable
{
    public const string PipeName = "PasswordVault.PasskeyCompanion.v1";

    private readonly CompanionStatusService _statusService;
    private readonly PluginRegistrationService _pluginRegistrationService;
    private readonly Action? _activateWindow;
    private readonly Action? _showStatusWindow;
    private readonly Action? _shutdownApp;
    private readonly CancellationTokenSource _cts = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private Task? _backgroundTask;

    public CompanionIpcServer(
        CompanionStatusService statusService,
        PluginRegistrationService pluginRegistrationService,
        Action? activateWindow = null,
        Action? showStatusWindow = null,
        Action? shutdownApp = null)
    {
        _statusService = statusService;
        _pluginRegistrationService = pluginRegistrationService;
        _activateWindow = activateWindow;
        _showStatusWindow = showStatusWindow;
        _shutdownApp = shutdownApp;
    }

    public void Start()
    {
        _backgroundTask ??= Task.Run(RunAsync);
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        if (_backgroundTask is not null)
        {
            try
            {
                await _backgroundTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _cts.Dispose();
    }

    private async Task RunAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(_cts.Token).ConfigureAwait(false);
                await HandleClientAsync(server, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(250, _cts.Token).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true)
        {
            AutoFlush = true,
        };

        var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        var request = DeserializeRequest(line);
        var response = BuildResponse(request);
        var payload = JsonSerializer.Serialize(response, _jsonOptions);
        await writer.WriteLineAsync(payload).ConfigureAwait(false);
    }

    private CompanionIpcRequest DeserializeRequest(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new CompanionIpcRequest();
        }

        try
        {
            return JsonSerializer.Deserialize<CompanionIpcRequest>(payload, _jsonOptions) ?? new CompanionIpcRequest();
        }
        catch
        {
            return new CompanionIpcRequest();
        }
    }

    private CompanionIpcResponse BuildResponse(CompanionIpcRequest request)
    {
        var action = (request.Action ?? string.Empty).Trim().ToLowerInvariant();

        return action switch
        {
            "ping" => new CompanionIpcResponse
            {
                Success = true,
                Message = "Password Vault passkey companion is reachable.",
                Snapshot = null,
            },
            "get_status" => new CompanionIpcResponse
            {
                Success = true,
                Message = "Companion status snapshot generated.",
                Snapshot = _statusService.Probe(),
                Workflow = _pluginRegistrationService.GetWorkflowSnapshot(),
            },
            "get_plugin_workflow" => new CompanionIpcResponse
            {
                Success = true,
                Message = "Plugin workflow snapshot generated.",
                Snapshot = null,
                Workflow = _pluginRegistrationService.GetWorkflowSnapshot(),
            },
            "prepare_plugin_registration" => _pluginRegistrationService.PrepareRegistration(),
            "create_passkey_skeleton" => _pluginRegistrationService.CaptureCreatePasskeyRequest(
                DeserializePayload<CompanionCreatePasskeyRequest>(request.PayloadJson)),
            "resolve_latest_operation" => _pluginRegistrationService.ResolveLatestOperation(
                DeserializePayload<CompanionResolveOperationRequest>(request.PayloadJson)),
            "plugin_activated" => BuildPluginActivatedResponse(),
            "activate" => BuildActivationResponse(),
            "show_status_window" => BuildShowStatusWindowResponse(),
            "shutdown" => BuildShutdownResponse(),
            _ => new CompanionIpcResponse
            {
                Success = false,
                Message = "Unknown companion action.",
                Snapshot = null,
            },
        };
    }

    private T? DeserializePayload<T>(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payload, _jsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private CompanionIpcResponse BuildPluginActivatedResponse()
    {
        var response = _pluginRegistrationService.HandlePluginActivation("ipc-forward");
        response.Snapshot ??= _statusService.Probe();
        return response;
    }

    private CompanionIpcResponse BuildActivationResponse()
    {
        try
        {
            _activateWindow?.Invoke();

            return new CompanionIpcResponse
            {
                Success = true,
                Message = "The Windows passkey companion is already running in background mode.",
                Snapshot = null,
            };
        }
        catch (Exception exception)
        {
            return new CompanionIpcResponse
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to notify the Windows passkey companion."
                    : exception.Message,
                Snapshot = null,
            };
        }
    }

    private CompanionIpcResponse BuildShowStatusWindowResponse()
    {
        try
        {
            _showStatusWindow?.Invoke();

            return new CompanionIpcResponse
            {
                Success = true,
                Message = "The companion status window was requested.",
                Snapshot = null,
            };
        }
        catch (Exception exception)
        {
            return new CompanionIpcResponse
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to show the companion status window."
                    : exception.Message,
                Snapshot = null,
            };
        }
    }

    private CompanionIpcResponse BuildShutdownResponse()
    {
        try
        {
            _shutdownApp?.Invoke();

            return new CompanionIpcResponse
            {
                Success = true,
                Message = "The companion shutdown was requested.",
                Snapshot = null,
            };
        }
        catch (Exception exception)
        {
            return new CompanionIpcResponse
            {
                Success = false,
                Message = string.IsNullOrWhiteSpace(exception.Message)
                    ? "Unable to shut down the companion."
                    : exception.Message,
                Snapshot = null,
            };
        }
    }
}
