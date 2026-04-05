using Microsoft.Maui.Storage;

#if ANDROID
using Android.Runtime;
using AndroidX.Biometric;
using AndroidX.Core.Content;
using Microsoft.Maui.ApplicationModel;
#endif

#if WINDOWS
using Windows.Security.Credentials.UI;
#endif

namespace blazorApp.Services;

public sealed class BiometricUnlockService : IBiometricUnlockService
{
    private const string BiometricEnabledKey = "password_vault.biometric.enabled";
    private const string MasterPasswordStorageKey = "password_vault.biometric.master_password";

    public async Task<HostBridgeState> GetBridgeStateAsync()
    {
        var availability = await CheckAvailabilityAsync();
        var storedPassword = await ReadStoredPasswordAsync();
        var isEnabled = availability.IsSupported &&
            availability.IsAvailable &&
            !string.IsNullOrWhiteSpace(storedPassword) &&
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

    public async Task<HostOperationResult> EnableAsync(string masterPassword)
    {
        if (string.IsNullOrWhiteSpace(masterPassword))
        {
            return BuildFailure("当前没有可保存的主密码，请先手动解锁一次。");
        }

        var availability = await CheckAvailabilityAsync();
        if (!availability.IsSupported)
        {
            return BuildFailure("当前宿主未接入生物识别能力。");
        }

        if (!availability.IsAvailable)
        {
            return BuildFailure(availability.Message);
        }

        // 先完成一次设备验证，再把主密码写入宿主安全存储。
        var verification = await RequestVerificationAsync($"使用{availability.BiometricLabel}启用快速解锁");
        if (!verification.Success)
        {
            return BuildFailure(verification.Message);
        }

        try
        {
            await SecureStorage.Default.SetAsync(MasterPasswordStorageKey, masterPassword);
            Preferences.Default.Set(BiometricEnabledKey, true);

            return new HostOperationResult
            {
                Success = true,
                Message = $"{availability.BiometricLabel}已启用，下次可直接验证解锁。",
                IsBiometricEnabled = true,
            };
        }
        catch (Exception ex)
        {
            return BuildFailure($"保存主密码到安全存储失败：{ex.Message}");
        }
    }

    public Task<HostOperationResult> DisableAsync()
    {
        try
        {
            SecureStorage.Default.Remove(MasterPasswordStorageKey);
            Preferences.Default.Remove(BiometricEnabledKey);

            return Task.FromResult(new HostOperationResult
            {
                Success = true,
                Message = "生物识别解锁已关闭。",
                IsBiometricEnabled = false,
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(BuildFailure($"关闭生物识别失败：{ex.Message}"));
        }
    }

    public async Task<HostOperationResult> UnlockAsync()
    {
        var availability = await CheckAvailabilityAsync();
        if (!availability.IsSupported)
        {
            return BuildFailure("当前宿主未接入生物识别能力。");
        }

        if (!availability.IsAvailable)
        {
            return BuildFailure(availability.Message);
        }

        if (!Preferences.Default.Get(BiometricEnabledKey, false))
        {
            return BuildFailure("当前尚未启用生物识别解锁。");
        }

        var verification = await RequestVerificationAsync($"使用{availability.BiometricLabel}解锁密码库");
        if (!verification.Success)
        {
            return BuildFailure(verification.Message);
        }

        var masterPassword = await ReadStoredPasswordAsync();
        if (string.IsNullOrWhiteSpace(masterPassword))
        {
            Preferences.Default.Remove(BiometricEnabledKey);
            return BuildFailure("未找到已保存的主密码，请先手动解锁后重新启用生物识别。");
        }

        return new HostOperationResult
        {
            Success = true,
            Message = "设备验证通过。",
            MasterPassword = masterPassword,
            IsBiometricEnabled = true,
        };
    }

    public async Task<HostOperationResult> UpdateStoredMasterPasswordAsync(string masterPassword)
    {
        if (!Preferences.Default.Get(BiometricEnabledKey, false))
        {
            return BuildFailure("当前未启用生物识别解锁。");
        }

        if (string.IsNullOrWhiteSpace(masterPassword))
        {
            return BuildFailure("新的主密码为空，无法同步到原生宿主。");
        }

        try
        {
            await SecureStorage.Default.SetAsync(MasterPasswordStorageKey, masterPassword);
            return new HostOperationResult
            {
                Success = true,
                Message = "原生宿主中的主密码已同步更新。",
                IsBiometricEnabled = true,
            };
        }
        catch (Exception ex)
        {
            return BuildFailure($"同步主密码失败：{ex.Message}");
        }
    }

    private async Task<string?> ReadStoredPasswordAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(MasterPasswordStorageKey);
        }
        catch
        {
            SecureStorage.Default.Remove(MasterPasswordStorageKey);
            Preferences.Default.Remove(BiometricEnabledKey);
            return null;
        }
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

    private async Task<BiometricAvailabilityResult> CheckAvailabilityAsync()
    {
#if WINDOWS
        try
        {
            var availability = await UserConsentVerifier.CheckAvailabilityAsync();

            return availability switch
            {
                UserConsentVerifierAvailability.Available => new BiometricAvailabilityResult(true, true, "Windows Hello", "windows", string.Empty),
                UserConsentVerifierAvailability.DeviceNotPresent => new BiometricAvailabilityResult(true, false, "Windows Hello", "windows", "当前设备没有可用的 Windows Hello 设备。"),
                UserConsentVerifierAvailability.NotConfiguredForUser => new BiometricAvailabilityResult(true, false, "Windows Hello", "windows", "请先在系统中配置 Windows Hello。"),
                UserConsentVerifierAvailability.DisabledByPolicy => new BiometricAvailabilityResult(true, false, "Windows Hello", "windows", "Windows Hello 被系统策略禁用。"),
                UserConsentVerifierAvailability.DeviceBusy => new BiometricAvailabilityResult(true, false, "Windows Hello", "windows", "Windows Hello 当前正忙，请稍后再试。"),
                _ => new BiometricAvailabilityResult(true, false, "Windows Hello", "windows", "Windows Hello 当前不可用。"),
            };
        }
        catch (Exception ex)
        {
            return new BiometricAvailabilityResult(true, false, "Windows Hello", "windows", $"读取 Windows Hello 状态失败：{ex.Message}");
        }
#elif ANDROID
        try
        {
            var activity = Platform.CurrentActivity;
            if (activity is null)
            {
                return new BiometricAvailabilityResult(true, false, "指纹或面容", "android", "当前 Android 宿主尚未就绪，请稍后再试。");
            }

            const int authenticators = (int)BiometricManager.Authenticators.BiometricStrong;
            var status = BiometricManager.From(activity).CanAuthenticate(authenticators);

            return status switch
            {
                BiometricManager.BiometricSuccess => new BiometricAvailabilityResult(true, true, "指纹或面容", "android", string.Empty),
                BiometricManager.BiometricErrorNoHardware => new BiometricAvailabilityResult(true, false, "指纹或面容", "android", "当前设备没有可用的生物识别硬件。"),
                BiometricManager.BiometricErrorHwUnavailable => new BiometricAvailabilityResult(true, false, "指纹或面容", "android", "当前生物识别硬件暂不可用。"),
                BiometricManager.BiometricErrorNoneEnrolled => new BiometricAvailabilityResult(true, false, "指纹或面容", "android", "请先在系统中录入指纹或面容。"),
                BiometricManager.BiometricErrorSecurityUpdateRequired => new BiometricAvailabilityResult(true, false, "指纹或面容", "android", "系统需要安全更新后才能使用生物识别。"),
                _ => new BiometricAvailabilityResult(true, false, "指纹或面容", "android", "当前 Android 生物识别不可用。"),
            };
        }
        catch (Exception ex)
        {
            return new BiometricAvailabilityResult(true, false, "指纹或面容", "android", $"读取 Android 生物识别状态失败：{ex.Message}");
        }
#else
        return await Task.FromResult(new BiometricAvailabilityResult(false, false, "生物识别", "web", "当前平台尚未接入生物识别解锁。"));
#endif
    }

    private async Task<VerificationResult> RequestVerificationAsync(string reason)
    {
#if WINDOWS
        try
        {
            var result = await UserConsentVerifier.RequestVerificationAsync(reason);

            return result switch
            {
                UserConsentVerificationResult.Verified => new VerificationResult(true, string.Empty),
                UserConsentVerificationResult.Canceled => new VerificationResult(false, "你已取消本次设备验证。"),
                UserConsentVerificationResult.DeviceBusy => new VerificationResult(false, "当前验证设备正忙，请稍后重试。"),
                UserConsentVerificationResult.DeviceNotPresent => new VerificationResult(false, "当前设备没有可用的 Windows Hello 设备。"),
                UserConsentVerificationResult.DisabledByPolicy => new VerificationResult(false, "Windows Hello 被系统策略禁用。"),
                UserConsentVerificationResult.NotConfiguredForUser => new VerificationResult(false, "请先在系统中配置 Windows Hello。"),
                UserConsentVerificationResult.RetriesExhausted => new VerificationResult(false, "验证失败次数过多，请稍后再试。"),
                _ => new VerificationResult(false, "Windows Hello 验证失败。"),
            };
        }
        catch (Exception ex)
        {
            return new VerificationResult(false, $"Windows Hello 验证失败：{ex.Message}");
        }
#elif ANDROID
        return await RequestAndroidVerificationAsync(reason);
#else
        return await Task.FromResult(new VerificationResult(false, "当前平台尚未接入生物识别验证。"));
#endif
    }

#if ANDROID
    private static Task<VerificationResult> RequestAndroidVerificationAsync(string reason)
    {
        var tcs = new TaskCompletionSource<VerificationResult>();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var currentActivity = Platform.CurrentActivity;
                if (currentActivity is not AndroidX.Fragment.App.FragmentActivity activity)
                {
                    tcs.TrySetResult(new VerificationResult(false, "当前 Android Activity 尚未准备好，无法发起生物识别。"));
                    return;
                }

                var executor = ContextCompat.GetMainExecutor(activity);
                if (executor is null)
                {
                    tcs.TrySetResult(new VerificationResult(false, "当前 Android 执行器不可用，无法发起生物识别。"));
                    return;
                }

                var callback = new AndroidBiometricCallback(tcs);
                var prompt = new BiometricPrompt(activity, executor, callback);
                var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                    .SetTitle("密码库验证")
                    .SetSubtitle(reason)
                    .SetNegativeButtonText("取消")
                    .Build();

                prompt.Authenticate(promptInfo);
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(new VerificationResult(false, $"Android 生物识别调用失败：{ex.Message}"));
            }
        });

        return tcs.Task;
    }

    private sealed class AndroidBiometricCallback : BiometricPrompt.AuthenticationCallback
    {
        private readonly TaskCompletionSource<VerificationResult> _taskCompletionSource;

        public AndroidBiometricCallback(TaskCompletionSource<VerificationResult> taskCompletionSource)
        {
            _taskCompletionSource = taskCompletionSource;
        }

        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
        {
            base.OnAuthenticationSucceeded(result);
            _taskCompletionSource.TrySetResult(new VerificationResult(true, string.Empty));
        }

        public override void OnAuthenticationError([GeneratedEnum] int errorCode, Java.Lang.ICharSequence? errString)
        {
            base.OnAuthenticationError(errorCode, errString ?? new Java.Lang.String(string.Empty));
            _taskCompletionSource.TrySetResult(new VerificationResult(false, errString?.ToString() ?? "Android 生物识别验证失败。"));
        }
    }
#endif

    private sealed record BiometricAvailabilityResult(
        bool IsSupported,
        bool IsAvailable,
        string BiometricLabel,
        string Platform,
        string Message);

    private sealed record VerificationResult(bool Success, string Message);
}
