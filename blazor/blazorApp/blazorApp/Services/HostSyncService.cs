using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
    private const string LanTlsCertificateKey = "password_vault.sync.lan.tls.certificate";
    private const string LanTlsCertificatePasswordKey = "password_vault.sync.lan.tls.password";
    private const int SnapshotPort = 49321;
    private const int DiscoveryPort = 49322;
    private const string DiscoveryMessage = "PASSWORD_VAULT_DISCOVER_V1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly object _snapshotLock = new();
    private readonly CancellationTokenSource _lifetimeCts = new();
    private readonly SemaphoreSlim _certificateLock = new(1, 1);

    private TcpListener? _snapshotListener;
    private Task? _snapshotServerTask;
    private UdpClient? _discoveryResponder;
    private Task? _discoveryResponderTask;
    private int _boundSnapshotPort = SnapshotPort;
    private string _publishedSnapshot = string.Empty;
    private SyncPreview _publishedPreview = new();
    private long _publishedAt;
    private X509Certificate2? _snapshotCertificate;
    private string _snapshotCertificateFingerprint = string.Empty;

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
                Message = "WebDAV configuration saved.",
            };
        }
        catch (Exception ex)
        {
            return BuildFailure($"Unable to save the WebDAV configuration: {ex.Message}");
        }
    }

    public async Task<HostOperationResult> UploadSnapshotToWebDavAsync(SnapshotTransferRequest request)
    {
        try
        {
            var content = request?.Content ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content))
            {
                return BuildFailure("There is no encrypted snapshot to upload.");
            }

            using var client = await CreateWebDavClientAsync();
            using var message = new HttpRequestMessage(HttpMethod.Put, await BuildWebDavUriAsync())
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            };

            using var response = await client.SendAsync(message, _lifetimeCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return BuildFailure($"WebDAV upload failed: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            return new HostOperationResult
            {
                Success = true,
                Message = "Current data uploaded to WebDAV.",
            };
        }
        catch (Exception ex)
        {
            return BuildFailure($"WebDAV upload failed: {ex.Message}");
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
                    Message = $"WebDAV download failed: {(int)response.StatusCode} {response.ReasonPhrase}",
                };
            }

            return new HostTextOperationResult
            {
                Success = true,
                Message = "Encrypted sync snapshot downloaded from WebDAV.",
                Content = await response.Content.ReadAsStringAsync(_lifetimeCts.Token),
            };
        }
        catch (Exception ex)
        {
            return new HostTextOperationResult
            {
                Success = false,
                Message = $"WebDAV download failed: {ex.Message}",
            };
        }
    }

    public Task<HostOperationResult> PublishLanSnapshotAsync(PublishLanSnapshotRequest request)
    {
        EnsureBackgroundServicesStarted();

        if (request is null || string.IsNullOrWhiteSpace(request.SnapshotContent))
        {
            return Task.FromResult(BuildFailure("There is no encrypted LAN snapshot to publish."));
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
            Message = "The encrypted snapshot is available on the local network over TLS.",
        });
    }

    public Task<HostOperationResult> SetLanDeviceNameAsync(UpdateDeviceNameRequest request)
    {
        Preferences.Default.Set(DeviceNameKey, request?.DeviceName?.Trim() ?? GetDefaultDeviceName());

        return Task.FromResult(new HostOperationResult
        {
            Success = true,
            Message = "Device name updated.",
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
                Message = "The selected device address is incomplete.",
            };
        }

        if (string.IsNullOrWhiteSpace(request.TlsFingerprintSha256))
        {
            return new HostTextOperationResult
            {
                Success = false,
                Message = "The selected device did not provide a TLS fingerprint.",
            };
        }

        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                    ValidateFingerprint(certificate, request.TlsFingerprintSha256),
            };

            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(10),
            };

            using var response = await client.GetAsync(
                new Uri($"https://{request.Host}:{request.Port}/snapshot"),
                _lifetimeCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                return new HostTextOperationResult
                {
                    Success = false,
                    Message = $"LAN snapshot download failed: {(int)response.StatusCode} {response.ReasonPhrase}",
                };
            }

            return new HostTextOperationResult
            {
                Success = true,
                Message = "Encrypted sync snapshot downloaded from the selected device.",
                Content = await response.Content.ReadAsStringAsync(_lifetimeCts.Token),
            };
        }
        catch (Exception ex)
        {
            return new HostTextOperationResult
            {
                Success = false,
                Message = $"LAN snapshot download failed: {ex.Message}",
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

        _snapshotCertificate?.Dispose();
        _certificateLock.Dispose();
        _lifetimeCts.Dispose();
    }

    private static HostOperationResult BuildFailure(string message)
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
            await using var networkStream = client.GetStream();
            using var sslStream = new SslStream(networkStream, false);
            var certificate = await GetOrCreateSnapshotCertificateAsync();

            await sslStream.AuthenticateAsServerAsync(
                new SslServerAuthenticationOptions
                {
                    ServerCertificate = certificate,
                    ClientCertificateRequired = false,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                },
                cancellationToken);

            using var reader = new StreamReader(sslStream, Encoding.UTF8, false, 1024, leaveOpen: true);

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
                await WriteHttpResponseAsync(
                    sslStream,
                    405,
                    "Method Not Allowed",
                    "text/plain; charset=utf-8",
                    "Only GET is supported.",
                    cancellationToken);
                return;
            }

            if (!string.Equals(parts[1], "/snapshot", StringComparison.OrdinalIgnoreCase))
            {
                await WriteHttpResponseAsync(
                    sslStream,
                    404,
                    "Not Found",
                    "text/plain; charset=utf-8",
                    "Not found.",
                    cancellationToken);
                return;
            }

            string snapshot;
            lock (_snapshotLock)
            {
                snapshot = _publishedSnapshot;
            }

            if (string.IsNullOrWhiteSpace(snapshot))
            {
                await WriteHttpResponseAsync(
                    sslStream,
                    503,
                    "Service Unavailable",
                    "text/plain; charset=utf-8",
                    "No snapshot published.",
                    cancellationToken);
                return;
            }

            await WriteHttpResponseAsync(
                sslStream,
                200,
                "OK",
                "application/json; charset=utf-8",
                snapshot,
                cancellationToken);
        }
        catch
        {
        }
    }

    private static async Task WriteHttpResponseAsync(
        Stream stream,
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

                var summary = await BuildLocalLanSummaryAsync();
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

    private async Task<LanDeviceSummary> BuildLocalLanSummaryAsync()
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
            TlsFingerprintSha256 = await GetSnapshotCertificateFingerprintAsync(),
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
            throw new InvalidOperationException("Please enter the WebDAV URL first.");
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("The WebDAV URL is invalid.");
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
        var platformName = "Current device";
        if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
        {
            platformName = "Windows device";
        }
        else if (DeviceInfo.Current.Platform == DevicePlatform.Android)
        {
            platformName = "Android device";
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

    private async Task<string> GetSnapshotCertificateFingerprintAsync()
    {
        var certificate = await GetOrCreateSnapshotCertificateAsync();
        _snapshotCertificateFingerprint = ComputeFingerprint(certificate);
        return _snapshotCertificateFingerprint;
    }

    private async Task<X509Certificate2> GetOrCreateSnapshotCertificateAsync()
    {
        if (_snapshotCertificate is not null)
        {
            return _snapshotCertificate;
        }

        await _certificateLock.WaitAsync(_lifetimeCts.Token);
        try
        {
            if (_snapshotCertificate is not null)
            {
                return _snapshotCertificate;
            }

            var storedCertificate = string.Empty;
            var storedPassword = string.Empty;

            try
            {
                storedCertificate = await SecureStorage.Default.GetAsync(LanTlsCertificateKey) ?? string.Empty;
                storedPassword = await SecureStorage.Default.GetAsync(LanTlsCertificatePasswordKey) ?? string.Empty;
            }
            catch
            {
            }

            if (!string.IsNullOrWhiteSpace(storedCertificate) && !string.IsNullOrWhiteSpace(storedPassword))
            {
                try
                {
                    var pfxBytes = Convert.FromBase64String(storedCertificate);
                    _snapshotCertificate = new X509Certificate2(
                        pfxBytes,
                        storedPassword,
                        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
                }
                catch
                {
                    SecureStorage.Default.Remove(LanTlsCertificateKey);
                    SecureStorage.Default.Remove(LanTlsCertificatePasswordKey);
                    _snapshotCertificate = null;
                }
            }

            if (_snapshotCertificate is null)
            {
                using var rsa = RSA.Create(2048);
                var request = new CertificateRequest(
                    $"CN=PasswordVault-{GetOrCreateDeviceId()}",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(false, false, 0, false));
                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                        false));
                request.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

                var sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("localhost");
                sanBuilder.AddIpAddress(IPAddress.Loopback);
                try
                {
                    sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
                }
                catch
                {
                }

                request.CertificateExtensions.Add(sanBuilder.Build());

                using var generatedCertificate = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddYears(5));

                var exportPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
                var exportedPfx = generatedCertificate.Export(X509ContentType.Pfx, exportPassword);

                try
                {
                    await SecureStorage.Default.SetAsync(
                        LanTlsCertificateKey,
                        Convert.ToBase64String(exportedPfx));
                    await SecureStorage.Default.SetAsync(
                        LanTlsCertificatePasswordKey,
                        exportPassword);
                }
                catch
                {
                }

                _snapshotCertificate = new X509Certificate2(
                    exportedPfx,
                    exportPassword,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
            }

            _snapshotCertificateFingerprint = ComputeFingerprint(_snapshotCertificate);
            return _snapshotCertificate;
        }
        finally
        {
            _certificateLock.Release();
        }
    }

    private static string ComputeFingerprint(X509Certificate2 certificate)
    {
        return Convert.ToHexString(SHA256.HashData(certificate.RawData));
    }

    private static bool ValidateFingerprint(X509Certificate? certificate, string expectedFingerprint)
    {
        if (certificate is null || string.IsNullOrWhiteSpace(expectedFingerprint))
        {
            return false;
        }

        using var certificate2 = certificate as X509Certificate2 ?? new X509Certificate2(certificate);
        var actualFingerprint = ComputeFingerprint(certificate2);
        return string.Equals(
            NormalizeFingerprint(actualFingerprint),
            NormalizeFingerprint(expectedFingerprint),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeFingerprint(string value)
    {
        return string.Concat(
            (value ?? string.Empty)
                .ToUpperInvariant()
                .Where(character => !char.IsWhiteSpace(character) && character != ':'));
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
