using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;

#if ANDROID
using Android.Runtime;
using Android.Security.Keystore;
using AndroidX.Biometric;
using AndroidX.Core.Content;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;
using Microsoft.Maui.ApplicationModel;
#endif

#if WINDOWS
using Windows.Security.Cryptography;
using Windows.Security.Credentials;
using Windows.Storage.Streams;
#endif

namespace blazorApp.Services;

public sealed class BiometricUnlockService : IBiometricUnlockService
{
    private const string BiometricEnabledKey = "password_vault.biometric.enabled";
    private const string BiometricPayloadStorageKey = "password_vault.biometric.payload";
    private const string BiometricReauthIntervalKey = "password_vault.biometric.reauth_interval_hours";
    private const string BiometricLastManualUnlockAtKey = "password_vault.biometric.last_manual_unlock_at";

#if WINDOWS
    private const string WindowsHelloCredentialName = "PasswordVaultBiometric";
#endif

#if ANDROID
    private const string AndroidKeystoreAlias = "password_vault_biometric_key";
#endif

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly byte[] WindowsHelloChallengeBytes =
        Encoding.UTF8.GetBytes("PasswordVaultBiometricWrap:v1");

    public async Task<HostBridgeState> GetBridgeStateAsync()
    {
        var availability = await CheckAvailabilityAsync();
        var storedPayload = await ReadProtectedPayloadAsync();
        var isEnabled =
            availability.IsSupported &&
            availability.IsAvailable &&
            storedPayload is not null &&
            Preferences.Default.Get(BiometricEnabledKey, false);

        return new HostBridgeState
        {
            IsSupported = availability.IsSupported,
            IsBiometricAvailable = availability.IsAvailable,
            IsBiometricEnabled = isEnabled,
            BiometricLabel = availability.BiometricLabel,
            Platform = availability.Platform,
            Message = availability.Message,
        };
    }

    public async Task<HostOperationResult> EnableAsync(string vaultKeyBase64, int reauthIntervalHours)
    {
        if (string.IsNullOrWhiteSpace(vaultKeyBase64))
        {
            return BuildFailure("No local vault key is available. Unlock with the master password first.");
        }

        var availability = await CheckAvailabilityAsync();
        if (!availability.IsSupported)
        {
            return BuildFailure("Biometric unlock is not integrated in this host.");
        }

        if (!availability.IsAvailable)
        {
            return BuildFailure(availability.Message);
        }

        try
        {
            await ProtectVaultKeyAsync(vaultKeyBase64, availability.BiometricLabel, recreatePlatformKey: true);
            Preferences.Default.Set(BiometricEnabledKey, true);
            Preferences.Default.Set(BiometricReauthIntervalKey, NormalizeReauthIntervalHours(reauthIntervalHours));
            Preferences.Default.Set(BiometricLastManualUnlockAtKey, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            return new HostOperationResult
            {
                Success = true,
                Message = $"{availability.BiometricLabel} enabled.",
                IsBiometricEnabled = true,
            };
        }
        catch (Exception ex)
        {
            await ClearStoredBiometricStateAsync();
            return BuildFailure($"Unable to enable biometrics: {ex.Message}");
        }
    }

    public async Task<HostOperationResult> DisableAsync()
    {
        try
        {
            await ClearStoredBiometricStateAsync();

            return new HostOperationResult
            {
                Success = true,
                Message = "Biometric unlock disabled.",
                IsBiometricEnabled = false,
            };
        }
        catch (Exception ex)
        {
            return BuildFailure($"Unable to disable biometrics: {ex.Message}");
        }
    }

    public async Task<HostOperationResult> UnlockAsync()
    {
        var availability = await CheckAvailabilityAsync();
        if (!availability.IsSupported)
        {
            return BuildFailure("Biometric unlock is not integrated in this host.");
        }

        if (!availability.IsAvailable)
        {
            return BuildFailure(availability.Message);
        }

        if (!Preferences.Default.Get(BiometricEnabledKey, false))
        {
            return BuildFailure("Biometric unlock has not been enabled yet.");
        }

        var storedPayload = await ReadProtectedPayloadAsync();
        if (storedPayload is null)
        {
            await ClearStoredBiometricStateAsync();
            return BuildFailure("The stored biometric device key could not be found.");
        }

        if (IsManualUnlockExpired())
        {
            return new HostOperationResult
            {
                Success = false,
                Message = "Manual master-password unlock is required again.",
                IsBiometricEnabled = true,
                RequiresManualUnlock = true,
            };
        }

        try
        {
            var vaultKeyBase64 = await UnprotectVaultKeyAsync(storedPayload, availability.BiometricLabel);
            if (string.IsNullOrWhiteSpace(vaultKeyBase64))
            {
                await ClearStoredBiometricStateAsync();
                return BuildFailure("The stored biometric device key is no longer valid.");
            }

            return new HostOperationResult
            {
                Success = true,
                Message = "Device verification succeeded.",
                VaultKeyBase64 = vaultKeyBase64,
                IsBiometricEnabled = true,
            };
        }
        catch (Exception ex)
        {
            return BuildFailure(ex.Message);
        }
    }

    public async Task<HostOperationResult> UpdateStoredVaultKeyAsync(
        string vaultKeyBase64,
        int reauthIntervalHours,
        bool markManualUnlock)
    {
        if (!Preferences.Default.Get(BiometricEnabledKey, false))
        {
            return BuildFailure("Biometric unlock is not enabled.");
        }

        try
        {
            Preferences.Default.Set(BiometricReauthIntervalKey, NormalizeReauthIntervalHours(reauthIntervalHours));

            if (markManualUnlock)
            {
                Preferences.Default.Set(
                    BiometricLastManualUnlockAtKey,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }

            if (!string.IsNullOrWhiteSpace(vaultKeyBase64))
            {
                var availability = await CheckAvailabilityAsync();
                if (!availability.IsAvailable)
                {
                    return BuildFailure(availability.Message);
                }

                await ProtectVaultKeyAsync(vaultKeyBase64, availability.BiometricLabel, recreatePlatformKey: false);
            }

            return new HostOperationResult
            {
                Success = true,
                Message = "Biometric unlock settings updated.",
                IsBiometricEnabled = true,
            };
        }
        catch (Exception ex)
        {
            return BuildFailure($"Unable to update biometric unlock settings: {ex.Message}");
        }
    }

    private static int NormalizeReauthIntervalHours(int reauthIntervalHours)
    {
        return reauthIntervalHours < 0 ? 0 : reauthIntervalHours;
    }

    private bool IsManualUnlockExpired()
    {
        var reauthIntervalHours = Preferences.Default.Get(BiometricReauthIntervalKey, 0);
        if (reauthIntervalHours <= 0)
        {
            return false;
        }

        var lastManualUnlockAt = Preferences.Default.Get(BiometricLastManualUnlockAtKey, 0L);
        if (lastManualUnlockAt <= 0)
        {
            return true;
        }

        var elapsed = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(lastManualUnlockAt);
        return elapsed > TimeSpan.FromHours(reauthIntervalHours);
    }

    private static HostOperationResult BuildFailure(string message)
    {
        return new HostOperationResult
        {
            Success = false,
            Message = message,
            IsBiometricEnabled = false,
        };
    }

    private async Task StoreProtectedPayloadAsync(ProtectedVaultPayload payload)
    {
        await SecureStorage.Default.SetAsync(
            BiometricPayloadStorageKey,
            JsonSerializer.Serialize(payload, JsonOptions));
    }

    private async Task<ProtectedVaultPayload?> ReadProtectedPayloadAsync()
    {
        try
        {
            var value = await SecureStorage.Default.GetAsync(BiometricPayloadStorageKey);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return JsonSerializer.Deserialize<ProtectedVaultPayload>(value, JsonOptions);
        }
        catch
        {
            SecureStorage.Default.Remove(BiometricPayloadStorageKey);
            return null;
        }
    }

    private async Task ClearStoredBiometricStateAsync()
    {
        SecureStorage.Default.Remove(BiometricPayloadStorageKey);
        Preferences.Default.Remove(BiometricEnabledKey);
        Preferences.Default.Remove(BiometricReauthIntervalKey);
        Preferences.Default.Remove(BiometricLastManualUnlockAtKey);

#if WINDOWS
        try
        {
            await KeyCredentialManager.DeleteAsync(WindowsHelloCredentialName);
        }
        catch
        {
        }
#endif

#if ANDROID
        try
        {
            var keyStore = KeyStore.GetInstance("AndroidKeyStore");
            keyStore.Load(null);
            if (keyStore.ContainsAlias(AndroidKeystoreAlias))
            {
                keyStore.DeleteEntry(AndroidKeystoreAlias);
            }
        }
        catch
        {
        }
#endif
    }

    private async Task ProtectVaultKeyAsync(string vaultKeyBase64, string biometricLabel, bool recreatePlatformKey)
    {
#if WINDOWS
        var payload = await ProtectVaultKeyWithWindowsHelloAsync(vaultKeyBase64, recreatePlatformKey);
        await StoreProtectedPayloadAsync(payload);
        return;
#elif ANDROID
        var payload = await ProtectVaultKeyWithAndroidAsync(vaultKeyBase64, biometricLabel, recreatePlatformKey);
        await StoreProtectedPayloadAsync(payload);
        return;
#else
        throw new InvalidOperationException("Biometric key wrapping is not available on this platform.");
#endif
    }

    private async Task<string> UnprotectVaultKeyAsync(ProtectedVaultPayload payload, string biometricLabel)
    {
#if WINDOWS
        return await UnprotectVaultKeyWithWindowsHelloAsync(payload);
#elif ANDROID
        return await UnprotectVaultKeyWithAndroidAsync(payload, biometricLabel);
#else
        throw new InvalidOperationException("Biometric key unwrapping is not available on this platform.");
#endif
    }

    private async Task<BiometricAvailabilityResult> CheckAvailabilityAsync()
    {
#if WINDOWS
        try
        {
            var isSupported = await KeyCredentialManager.IsSupportedAsync();
            if (!isSupported)
            {
                return new BiometricAvailabilityResult(
                    true,
                    false,
                    "Windows Hello",
                    "windows",
                    "Configure Windows Hello in system settings first.");
            }

            return new BiometricAvailabilityResult(true, true, "Windows Hello", "windows", string.Empty);
        }
        catch (Exception ex)
        {
            return new BiometricAvailabilityResult(
                true,
                false,
                "Windows Hello",
                "windows",
                $"Unable to read Windows Hello status: {ex.Message}");
        }
#elif ANDROID
        try
        {
            var activity = Platform.CurrentActivity;
            if (activity is null)
            {
                return new BiometricAvailabilityResult(
                    true,
                    false,
                    "Fingerprint or face unlock",
                    "android",
                    "The Android host is not ready yet.");
            }

            const int authenticators = (int)BiometricManager.Authenticators.BiometricStrong;
            var status = BiometricManager.From(activity).CanAuthenticate(authenticators);

            return status switch
            {
                BiometricManager.BiometricSuccess => new BiometricAvailabilityResult(
                    true,
                    true,
                    "Fingerprint or face unlock",
                    "android",
                    string.Empty),
                BiometricManager.BiometricErrorNoHardware => new BiometricAvailabilityResult(
                    true,
                    false,
                    "Fingerprint or face unlock",
                    "android",
                    "This device does not provide biometric hardware."),
                BiometricManager.BiometricErrorHwUnavailable => new BiometricAvailabilityResult(
                    true,
                    false,
                    "Fingerprint or face unlock",
                    "android",
                    "Biometric hardware is temporarily unavailable."),
                BiometricManager.BiometricErrorNoneEnrolled => new BiometricAvailabilityResult(
                    true,
                    false,
                    "Fingerprint or face unlock",
                    "android",
                    "Enroll a fingerprint or face in system settings first."),
                BiometricManager.BiometricErrorSecurityUpdateRequired => new BiometricAvailabilityResult(
                    true,
                    false,
                    "Fingerprint or face unlock",
                    "android",
                    "A system security update is required before biometrics can be used."),
                _ => new BiometricAvailabilityResult(
                    true,
                    false,
                    "Fingerprint or face unlock",
                    "android",
                    "Biometric unlock is currently unavailable."),
            };
        }
        catch (Exception ex)
        {
            return new BiometricAvailabilityResult(
                true,
                false,
                "Fingerprint or face unlock",
                "android",
                $"Unable to read Android biometric status: {ex.Message}");
        }
#else
        return await Task.FromResult(new BiometricAvailabilityResult(
            false,
            false,
            "Biometrics",
            "web",
            "Biometric unlock is not available on this platform."));
#endif
    }

#if WINDOWS
    private async Task<ProtectedVaultPayload> ProtectVaultKeyWithWindowsHelloAsync(
        string vaultKeyBase64,
        bool recreatePlatformKey)
    {
        var credential = await GetWindowsHelloCredentialAsync(recreatePlatformKey);
        var wrappingKey = await DeriveWindowsHelloWrappingKeyAsync(credential);
        return ProtectStringPayload(vaultKeyBase64, wrappingKey);
    }

    private async Task<string> UnprotectVaultKeyWithWindowsHelloAsync(ProtectedVaultPayload payload)
    {
        var credential = await GetWindowsHelloCredentialAsync(recreatePlatformKey: false);
        var wrappingKey = await DeriveWindowsHelloWrappingKeyAsync(credential);
        return UnprotectStringPayload(payload, wrappingKey);
    }

    private static async Task<KeyCredential> GetWindowsHelloCredentialAsync(bool recreatePlatformKey)
    {
        KeyCredentialRetrievalResult retrievalResult;

        if (recreatePlatformKey)
        {
            retrievalResult = await KeyCredentialManager.RequestCreateAsync(
                WindowsHelloCredentialName,
                KeyCredentialCreationOption.ReplaceExisting);
        }
        else
        {
            retrievalResult = await KeyCredentialManager.OpenAsync(WindowsHelloCredentialName);
            if (retrievalResult.Status == KeyCredentialStatus.NotFound)
            {
                retrievalResult = await KeyCredentialManager.RequestCreateAsync(
                    WindowsHelloCredentialName,
                    KeyCredentialCreationOption.ReplaceExisting);
            }
        }

        if (retrievalResult.Status != KeyCredentialStatus.Success || retrievalResult.Credential is null)
        {
            throw new InvalidOperationException(MapWindowsHelloStatus(retrievalResult.Status));
        }

        return retrievalResult.Credential;
    }

    private static async Task<byte[]> DeriveWindowsHelloWrappingKeyAsync(KeyCredential credential)
    {
        var challengeBuffer = CryptographicBuffer.CreateFromByteArray(WindowsHelloChallengeBytes);
        var signResult = await credential.RequestSignAsync(challengeBuffer);

        if (signResult.Status != KeyCredentialStatus.Success)
        {
            throw new InvalidOperationException(MapWindowsHelloStatus(signResult.Status));
        }

        CryptographicBuffer.CopyToByteArray(signResult.Result, out var signatureBytes);
        return SHA256.HashData(signatureBytes);
    }

    private static string MapWindowsHelloStatus(KeyCredentialStatus status)
    {
        return status switch
        {
            KeyCredentialStatus.UserCanceled => "Windows Hello verification was cancelled.",
            KeyCredentialStatus.NotFound => "Windows Hello is not configured for this device.",
            KeyCredentialStatus.UnknownError => "Windows Hello failed.",
            KeyCredentialStatus.SecurityDeviceLocked => "Windows Hello is temporarily locked.",
            KeyCredentialStatus.UserPrefersPassword => "Manual master-password unlock is required.",
            _ => "Windows Hello is currently unavailable.",
        };
    }
#endif

#if ANDROID
    private static async Task<ProtectedVaultPayload> ProtectVaultKeyWithAndroidAsync(
        string vaultKeyBase64,
        string biometricLabel,
        bool recreatePlatformKey)
    {
        var cipher = CreateAndroidEncryptCipher(recreatePlatformKey);
        var authenticatedCipher = await RequestAndroidCipherAsync(
            $"Use {biometricLabel} to enable fast unlock",
            cipher);

        var encryptedBytes = authenticatedCipher.DoFinal(Encoding.UTF8.GetBytes(vaultKeyBase64));
        var iv = authenticatedCipher.GetIV() ?? Array.Empty<byte>();

        return new ProtectedVaultPayload(
            Convert.ToBase64String(iv),
            Convert.ToBase64String(encryptedBytes));
    }

    private static async Task<string> UnprotectVaultKeyWithAndroidAsync(
        ProtectedVaultPayload payload,
        string biometricLabel)
    {
        try
        {
            var cipher = CreateAndroidDecryptCipher(payload);
            var authenticatedCipher = await RequestAndroidCipherAsync(
                $"Use {biometricLabel} to unlock your vault",
                cipher);

            var plainBytes = authenticatedCipher.DoFinal(Convert.FromBase64String(payload.CipherText));
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (KeyPermanentlyInvalidatedException)
        {
            throw new InvalidOperationException(
                "Biometric enrollment changed on this device. Re-enable biometrics after a manual unlock.");
        }
    }

    private static Cipher CreateAndroidEncryptCipher(bool recreatePlatformKey)
    {
        var secretKey = GetOrCreateAndroidSecretKey(recreatePlatformKey);
        var cipher = Cipher.GetInstance("AES/GCM/NoPadding");
        cipher.Init(Javax.Crypto.CipherMode.EncryptMode, secretKey);
        return cipher;
    }

    private static Cipher CreateAndroidDecryptCipher(ProtectedVaultPayload payload)
    {
        var secretKey = GetOrCreateAndroidSecretKey(recreatePlatformKey: false);
        var cipher = Cipher.GetInstance("AES/GCM/NoPadding");
        var iv = Convert.FromBase64String(payload.Iv);
        cipher.Init(Javax.Crypto.CipherMode.DecryptMode, secretKey, new GCMParameterSpec(128, iv));
        return cipher;
    }

    private static IKey GetOrCreateAndroidSecretKey(bool recreatePlatformKey)
    {
        var keyStore = KeyStore.GetInstance("AndroidKeyStore");
        keyStore.Load(null);

        if (recreatePlatformKey && keyStore.ContainsAlias(AndroidKeystoreAlias))
        {
            keyStore.DeleteEntry(AndroidKeystoreAlias);
        }

        if (!keyStore.ContainsAlias(AndroidKeystoreAlias))
        {
            var keyGenerator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, "AndroidKeyStore");
            var builder = new KeyGenParameterSpec.Builder(
                    AndroidKeystoreAlias,
                    KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                .SetBlockModes(KeyProperties.BlockModeGcm)
                .SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)
                .SetUserAuthenticationRequired(true)
                .SetInvalidatedByBiometricEnrollment(true);

            keyGenerator.Init(builder.Build());
            keyGenerator.GenerateKey();
        }

        var entry = keyStore.GetEntry(AndroidKeystoreAlias, null) as KeyStore.SecretKeyEntry;
        if (entry?.SecretKey is null)
        {
            throw new InvalidOperationException("Unable to initialize the Android biometric key.");
        }

        return entry.SecretKey;
    }

    private static Task<Cipher> RequestAndroidCipherAsync(string reason, Cipher cipher)
    {
        var taskCompletionSource = new TaskCompletionSource<Cipher>();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var currentActivity = Platform.CurrentActivity;
                if (currentActivity is not AndroidX.Fragment.App.FragmentActivity activity)
                {
                    taskCompletionSource.TrySetException(
                        new InvalidOperationException("The Android activity is not ready yet."));
                    return;
                }

                var executor = ContextCompat.GetMainExecutor(activity);
                if (executor is null)
                {
                    taskCompletionSource.TrySetException(
                        new InvalidOperationException("The Android main executor is unavailable."));
                    return;
                }

                var callback = new AndroidBiometricCipherCallback(taskCompletionSource);
                var prompt = new BiometricPrompt(activity, executor, callback);
                var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                    .SetTitle("Password Vault")
                    .SetSubtitle(reason)
                    .SetNegativeButtonText("Cancel")
                    .Build();

                prompt.Authenticate(promptInfo, new BiometricPrompt.CryptoObject(cipher));
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetException(
                    new InvalidOperationException($"Android biometric prompt failed: {ex.Message}", ex));
            }
        });

        return taskCompletionSource.Task;
    }

    private sealed class AndroidBiometricCipherCallback : BiometricPrompt.AuthenticationCallback
    {
        private readonly TaskCompletionSource<Cipher> _taskCompletionSource;

        public AndroidBiometricCipherCallback(TaskCompletionSource<Cipher> taskCompletionSource)
        {
            _taskCompletionSource = taskCompletionSource;
        }

        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
        {
            base.OnAuthenticationSucceeded(result);

            var cipher = result.CryptoObject?.Cipher;
            if (cipher is null)
            {
                _taskCompletionSource.TrySetException(
                    new InvalidOperationException(
                        "Biometric authentication succeeded, but no cipher handle was returned."));
                return;
            }

            _taskCompletionSource.TrySetResult(cipher);
        }

        public override void OnAuthenticationError([GeneratedEnum] int errorCode, Java.Lang.ICharSequence? errString)
        {
            base.OnAuthenticationError(errorCode, errString ?? new Java.Lang.String(string.Empty));
            _taskCompletionSource.TrySetException(
                new InvalidOperationException(errString?.ToString() ?? "Android biometric authentication failed."));
        }
    }
#endif

    private static ProtectedVaultPayload ProtectStringPayload(string plainText, byte[] wrappingKey)
    {
        using var aesGcm = new AesGcm(wrappingKey, 16);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tagBytes = new byte[16];

        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tagBytes);

        return new ProtectedVaultPayload(
            Convert.ToBase64String(nonce),
            Convert.ToBase64String([.. cipherBytes, .. tagBytes]));
    }

    private static string UnprotectStringPayload(ProtectedVaultPayload payload, byte[] wrappingKey)
    {
        using var aesGcm = new AesGcm(wrappingKey, 16);
        var nonce = Convert.FromBase64String(payload.Iv);
        var combinedBytes = Convert.FromBase64String(payload.CipherText);

        if (combinedBytes.Length < 16)
        {
            throw new InvalidOperationException(
                "The stored biometric device key is corrupted. Re-enable biometrics after a manual unlock.");
        }

        var cipherBytes = combinedBytes[..^16];
        var tagBytes = combinedBytes[^16..];
        var plainBytes = new byte[cipherBytes.Length];

        aesGcm.Decrypt(nonce, cipherBytes, tagBytes, plainBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private sealed record BiometricAvailabilityResult(
        bool IsSupported,
        bool IsAvailable,
        string BiometricLabel,
        string Platform,
        string Message);

    private sealed record ProtectedVaultPayload(string Iv, string CipherText);
}
