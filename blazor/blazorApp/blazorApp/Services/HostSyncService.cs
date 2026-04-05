using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace blazorApp.Services;

public sealed class HostSyncService : IHostSyncService, IDisposable
{
    private const string WebDavBaseUrlKey = "password_vault.sync.webdav.base_url";
    private const string WebDavRemotePathKey = "password_vault.sync.webdav.remote_path";
    private const string WebDavUsernameKey = "password_vault.sync.webdav.username";
    private const string WebDavPasswordKey = "password_vault.sync.webdav.password";
    private const string WebDavHasPasswordKey = "password_vault.sync.webdav.has_password";
    private const string DeviceIdKey = "password_vault.sync.device_id";
    private const string DeviceNameKey = "password_vault.sync.device_name";
    private const int SnapshotPort = 49321;
    private const int DiscoveryPort = 49322;
    private const string DiscoveryMessage = "PASSWORD_VAULT_DISCOVER_V1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly object _snapshotLock = new();
    private readonly CancellationTokenSource _lifetimeCts = new();

    private TcpListener? _snapshotListener;
    private Task? _snapshotServerTask;
    private UdpClient? _discoveryResponder;
    private Task? _discoveryResponderTask;
    private int _boundSnapshotPort = SnapshotPort;
    private string _publishedSnapshot = string.Empty;
    private SyncPreview _publishedPreview = new();
    private long _publishedAt;

    public HostSyncService()
    {
        EnsureBackgroundServicesStarted();
    }

    public Task<SyncSettingsState> GetSyncSettingsAsync()
    {
        return Task.FromResult(new SyncSettingsState
        {
            DeviceId = GetOrCreateDeviceId(),
            DeviceName = GetCurrentDeviceName(),
            WebDav = new WebDavSettingsState
            {
                BaseUrl = Preferences.Default.Get(WebDavBaseUrlKey, string.Empty),
                RemotePath = Preferences.Default.Get(WebDavRemotePathKey, "password-vault-sync.json"),
                Username = Preferences.Default.Get(WebDavUsernameKey, string.Empty),
                HasPassword = HasStoredWebDavPassword(),
            },
        });
    }

    public async Task<HostOperationResult> SaveWebDavSettingsAsync(SaveWebDavSettingsRequest request)
    {
        try
        {
            Preferences.Default.Set(WebDavBaseUrlKey, request?.BaseUrl?.Trim() ?? string.Empty);
            Preferences.Default.Set(WebDavRemotePathKey, request?.RemotePath?.Trim() ?? string.Empty);
            Preferences.Default.Set(WebDavUsernameKey, request?.Username?.Trim() ?? string.Empty);

            if (request?.UpdatePassword == true)
            {
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    SecureStorage.Default.Remove(WebDavPasswordKey);
                    Preferences.Default.Set(WebDavHasPasswordKey, false);
                }
                else
                {
                    await SecureStorage.Default.SetAsync(WebDavPasswordKey, request.Password);
                    Preferences.Default.Set(WebDavHasPasswordKey, true);
                }
            }

            return new HostOperationResult
            {
                Success = true,
                Message = "WebDAV 配置已保存。",
            };
        }
        catch (Exception ex)
        {
            return new HostOperationResult
            {
                Success = false,
                Message = $"保存 WebDAV 配置失败：{ex.Message}",
            };
        }
    }

    public async Task<HostOperationResult> UploadSnapshotToWebDavAsync(SnapshotTransferRequest request)
    {
        try
        {
            var content = request?.Content ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content))
            {
                return BuildFailure("当前没有可上传的同步数据。");
            }

            using var client = await CreateWebDavClientAsync();
            using var message = new HttpRequestMessage(HttpMethod.Put, await BuildWebDavUriAsync())
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            };

            using var response = await client.SendAsync(message, _lifetimeCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return BuildFailure($"WebDAV 上传失败：{(int)response.StatusCode} {response.ReasonPhrase}");
            }

            return new HostOperationResult
            {
                Success = true,
                Message = "当前数据已上传到 WebDAV。",
            };
        }
        catch (Exception ex)
        {
            return BuildFailure($"WebDAV 上传失败：{ex.Message}");
        }
    }

    public async Task<HostTextOperationResult> DownloadSnapshotFromWebDavAsync()
    {
        try
        {
            using var client = await CreateWebDavClientAsync();
            using var response = await client.GetAsync(await BuildWebDavUriAsync(), _lifetimeCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                return new HostTextOperationResult
                {
                    Success = false,
                    Message = $"WebDAV 下载失败：{(int)response.StatusCode} {response.ReasonPhrase}",
                };
            }

            return new HostTextOperationResult
            {
                Success = true,
                Message = "已从 WebDAV 拉取同步数据。",
                Content = await response.Content.ReadAsStringAsync(_lifetimeCts.Token),
            };
        }
        catch (Exception ex)
        {
            return new HostTextOperationResult
            {
                Success = false,
                Message = $"WebDAV 下载失败：{ex.Message}",
            };
        }
    }

    public Task<HostOperationResult> PublishLanSnapshotAsync(PublishLanSnapshotRequest request)
    {
        EnsureBackgroundServicesStarted();

        if (request is null || string.IsNullOrWhiteSpace(request.SnapshotContent))
        {
            return Task.FromResult(BuildFailure("当前没有可发布的局域网同步数据。"));
        }

        if (!string.IsNullOrWhiteSpace(request.DeviceName))
        {
            Preferences.Default.Set(DeviceNameKey, request.DeviceName.Trim());
        }

        lock (_snapshotLock)
        {
            _publishedSnapshot = request.SnapshotContent;
            _publishedPreview = request.Preview ?? new SyncPreview();
            _publishedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        return Task.FromResult(new HostOperationResult
        {
            Success = true,
            Message = "当前设备数据已发布到局域网同步通道。",
        });
    }

    public Task<HostOperationResult> SetLanDeviceNameAsync(UpdateDeviceNameRequest request)
    {
        Preferences.Default.Set(DeviceNameKey, request?.DeviceName?.Trim() ?? GetDefaultDeviceName());

        return Task.FromResult(new HostOperationResult
        {
            Success = true,
            Message = "设备名称已更新。",
        });
    }

    public async Task<IReadOnlyList<LanDeviceSummary>> ScanLanDevicesAsync()
    {
        EnsureBackgroundServicesStarted();

        var results = new Dictionary<string, LanDeviceSummary>(StringComparer.OrdinalIgnoreCase);
        var deviceId = GetOrCreateDeviceId();

        using var client = new UdpClient(0)
        {
            EnableBroadcast = true,
        };

        var discoverBytes = Encoding.UTF8.GetBytes(DiscoveryMessage);
        foreach (var endpoint in GetBroadcastEndpoints())
        {
            try
            {
                await client.SendAsync(discoverBytes, discoverBytes.Length, endpoint);
            }
            catch
            {
            }
        }

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2.4));

        while (!timeoutCts.IsCancellationRequested)
        {
            try
            {
                var result = await client.ReceiveAsync(timeoutCts.Token);
                var responseText = Encoding.UTF8.GetString(result.Buffer);
                var summary = JsonSerializer.Deserialize<LanDeviceSummary>(responseText, JsonOptions);
                if (summary is null || string.IsNullOrWhiteSpace(summary.DeviceId))
                {
                    continue;
                }

                summary.Host = result.RemoteEndPoint.Address.ToString();
                summary.IsCurrentDevice = string.Equals(summary.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase);

                var key = $"{summary.DeviceId}@{summary.Host}:{summary.Port}";
                results[key] = summary;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
            }
        }

        return results.Values
            .OrderByDescending(device => device.SnapshotAvailable)
            .ThenBy(device => device.IsCurrentDevice)
            .ThenBy(device => device.DeviceName)
            .ToList();
    }

    public async Task<HostTextOperationResult> DownloadLanSnapshotAsync(DownloadLanSnapshotRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Host) || request.Port <= 0)
        {
            return new HostTextOperationResult
            {
                Success = false,
                Message = "目标设备信息不完整，无法拉取同步数据。",
            };
        }

        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10),
            };

            var response = await client.GetAsync(
                new Uri($"http://{request.Host}:{request.Port}/snapshot"),
                _lifetimeCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                return new HostTextOperationResult
                {
                    Success = false,
                    Message = $"从局域网设备拉取失败：{(int)response.StatusCode} {response.ReasonPhrase}",
                };
            }

            return new HostTextOperationResult
            {
                Success = true,
                Message = "已从局域网设备拉取同步数据。",
                Content = await response.Content.ReadAsStringAsync(_lifetimeCts.Token),
            };
        }
        catch (Exception ex)
        {
            return new HostTextOperationResult
            {
                Success = false,
                Message = $"从局域网设备拉取失败：{ex.Message}",
            };
        }
    }

    public void Dispose()
    {
        _lifetimeCts.Cancel();

        try
        {
            _snapshotListener?.Stop();
        }
        catch
        {
        }

        try
        {
            _discoveryResponder?.Dispose();
        }
        catch
        {
        }

        _lifetimeCts.Dispose();
    }

    private HostOperationResult BuildFailure(string message)
    {
        return new HostOperationResult
        {
            Success = false,
            Message = message,
        };
    }

    private void EnsureBackgroundServicesStarted()
    {
        if (_snapshotListener is null)
        {
            StartSnapshotServer();
        }

        if (_discoveryResponder is null)
        {
            StartDiscoveryResponder();
        }
    }

    private void StartSnapshotServer()
    {
        try
        {
            _snapshotListener = new TcpListener(IPAddress.Any, SnapshotPort);
            _snapshotListener.Start();
        }
        catch
        {
            _snapshotListener = new TcpListener(IPAddress.Any, 0);
            _snapshotListener.Start();
        }
        _boundSnapshotPort = ((IPEndPoint)_snapshotListener.LocalEndpoint).Port;
        _snapshotServerTask = Task.Run(() => RunSnapshotServerAsync(_lifetimeCts.Token));
    }

    private async Task RunSnapshotServerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _snapshotListener is not null)
        {
            TcpClient? client = null;

            try
            {
                client = await _snapshotListener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                continue;
            }

            _ = Task.Run(() => HandleSnapshotRequestAsync(client, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleSnapshotRequestAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var clientLifetime = client;
        client.ReceiveTimeout = 5000;
        client.SendTimeout = 5000;

        try
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true);

            var requestLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(requestLine))
            {
                return;
            }

            string? line;
            do
            {
                line = await reader.ReadLineAsync();
            } while (!string.IsNullOrEmpty(line));

            var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !string.Equals(parts[0], "GET", StringComparison.OrdinalIgnoreCase))
            {
                await WriteHttpResponseAsync(stream, 405, "Method Not Allowed", "text/plain", "Only GET is supported.", cancellationToken);
                return;
            }

            if (!string.Equals(parts[1], "/snapshot", StringComparison.OrdinalIgnoreCase))
            {
                await WriteHttpResponseAsync(stream, 404, "Not Found", "text/plain", "Not found.", cancellationToken);
                return;
            }

            string snapshot;
            lock (_snapshotLock)
            {
                snapshot = _publishedSnapshot;
            }

            if (string.IsNullOrWhiteSpace(snapshot))
            {
                await WriteHttpResponseAsync(stream, 503, "Service Unavailable", "text/plain", "No snapshot published.", cancellationToken);
                return;
            }

            await WriteHttpResponseAsync(stream, 200, "OK", "application/json; charset=utf-8", snapshot, cancellationToken);
        }
        catch
        {
        }
    }

    private static async Task WriteHttpResponseAsync(
        NetworkStream stream,
        int statusCode,
        string statusText,
        string contentType,
        string content,
        CancellationToken cancellationToken)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
        var headers = string.Join(
            "\r\n",
            [
                $"HTTP/1.1 {statusCode} {statusText}",
                $"Content-Type: {contentType}",
                $"Content-Length: {payloadBytes.Length}",
                "Connection: close",
                string.Empty,
                string.Empty,
            ]);

        var headerBytes = Encoding.UTF8.GetBytes(headers);
        await stream.WriteAsync(headerBytes, cancellationToken);
        await stream.WriteAsync(payloadBytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private void StartDiscoveryResponder()
    {
        _discoveryResponder = new UdpClient(DiscoveryPort)
        {
            EnableBroadcast = true,
        };

        _discoveryResponderTask = Task.Run(() => RunDiscoveryResponderAsync(_lifetimeCts.Token));
    }

    private async Task RunDiscoveryResponderAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _discoveryResponder is not null)
        {
            try
            {
                var result = await _discoveryResponder.ReceiveAsync(cancellationToken);
                var message = Encoding.UTF8.GetString(result.Buffer);
                if (!string.Equals(message, DiscoveryMessage, StringComparison.Ordinal))
                {
                    continue;
                }

                var summary = BuildLocalLanSummary();
                var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(summary, JsonOptions));
                await _discoveryResponder.SendAsync(payload, payload.Length, result.RemoteEndPoint);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
            }
        }
    }

    private LanDeviceSummary BuildLocalLanSummary()
    {
        SyncPreview preview;
        var snapshotAvailable = false;
        var exportedAt = 0L;

        lock (_snapshotLock)
        {
            preview = _publishedPreview ?? new SyncPreview();
            snapshotAvailable = !string.IsNullOrWhiteSpace(_publishedSnapshot);
            exportedAt = _publishedAt;
        }

        return new LanDeviceSummary
        {
            DeviceId = GetOrCreateDeviceId(),
            DeviceName = GetCurrentDeviceName(),
            Host = string.Empty,
            Port = _boundSnapshotPort,
            SnapshotAvailable = snapshotAvailable,
            ExportedAt = exportedAt,
            Preview = preview,
        };
    }

    private async Task<HttpClient> CreateWebDavClientAsync()
    {
        var username = Preferences.Default.Get(WebDavUsernameKey, string.Empty);
        var password = await ReadWebDavPasswordAsync();

        var handler = new HttpClientHandler
        {
            PreAuthenticate = true,
        };

        if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(password))
        {
            handler.Credentials = new NetworkCredential(username, password);
        }

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15),
        };

        if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(password))
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        }

        return client;
    }

    private async Task<Uri> BuildWebDavUriAsync()
    {
        var baseUrl = Preferences.Default.Get(WebDavBaseUrlKey, string.Empty).Trim();
        var remotePath = Preferences.Default.Get(WebDavRemotePathKey, string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("请先填写 WebDAV 地址。");
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("WebDAV 地址格式不正确。");
        }

        if (string.IsNullOrWhiteSpace(remotePath))
        {
            return baseUri;
        }

        if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
        {
            baseUri = new Uri($"{baseUrl}/", UriKind.Absolute);
        }

        return new Uri(baseUri, remotePath.TrimStart('/'));
    }

    private bool HasStoredWebDavPassword()
    {
        return Preferences.Default.Get(WebDavHasPasswordKey, false);
    }

    private async Task<string> ReadWebDavPasswordAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(WebDavPasswordKey) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string GetCurrentDeviceName()
    {
        var stored = Preferences.Default.Get(DeviceNameKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(stored))
        {
            return stored;
        }

        var fallback = GetDefaultDeviceName();
        Preferences.Default.Set(DeviceNameKey, fallback);
        return fallback;
    }

    private string GetDefaultDeviceName()
    {
        var platformName = "当前设备";
        if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
        {
            platformName = "Windows 设备";
        }
        else if (DeviceInfo.Current.Platform == DevicePlatform.Android)
        {
            platformName = "Android 设备";
        }

        var rawName = DeviceInfo.Current.Name?.Trim();
        return string.IsNullOrWhiteSpace(rawName) ? platformName : rawName;
    }

    private string GetOrCreateDeviceId()
    {
        var existing = Preferences.Default.Get(DeviceIdKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var value = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(DeviceIdKey, value);
        return value;
    }

    private static IEnumerable<IPEndPoint> GetBroadcastEndpoints()
    {
        var endpoints = new Dictionary<string, IPEndPoint>(StringComparer.OrdinalIgnoreCase)
        {
            ["255.255.255.255"] = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort),
        };

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            IPInterfaceProperties? properties;
            try
            {
                properties = networkInterface.GetIPProperties();
            }
            catch
            {
                continue;
            }

            foreach (var unicastAddress in properties.UnicastAddresses)
            {
                if (unicastAddress.Address.AddressFamily != AddressFamily.InterNetwork ||
                    IPAddress.IsLoopback(unicastAddress.Address) ||
                    unicastAddress.IPv4Mask is null)
                {
                    continue;
                }

                try
                {
                    var addressBytes = unicastAddress.Address.GetAddressBytes();
                    var maskBytes = unicastAddress.IPv4Mask.GetAddressBytes();
                    var broadcastBytes = new byte[4];

                    for (var index = 0; index < 4; index += 1)
                    {
                        broadcastBytes[index] = (byte)(addressBytes[index] | (~maskBytes[index]));
                    }

                    var broadcastAddress = new IPAddress(broadcastBytes);
                    endpoints[broadcastAddress.ToString()] = new IPEndPoint(broadcastAddress, DiscoveryPort);
                }
                catch
                {
                }
            }
        }

        return endpoints.Values;
    }
}
