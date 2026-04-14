<script setup>
import { computed, nextTick, reactive, ref, watch } from "vue";

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  startWithSetup: { type: Boolean, default: false },
  step: { type: String, default: "appearance" },
  themeMode: { type: String, default: "system" },
  locale: { type: String, default: "zh-CN" },
  setupCompleted: { type: Boolean, default: false },
  setupLoading: { type: Boolean, default: false },
  biometricSupported: { type: Boolean, default: false },
  biometricAvailable: { type: Boolean, default: false },
  biometricEnabled: { type: Boolean, default: false },
  biometricLabel: { type: String, default: "Biometrics" },
  biometricLoading: { type: Boolean, default: false },
  biometricReauthHours: { type: Number, default: 72 },
  secretKeyHint: { type: String, default: "" },
  secretKeyValue: { type: String, default: "" },
  secretKeyLoading: { type: Boolean, default: false },
  supportsWebDavSync: { type: Boolean, default: false },
  supportsLanSync: { type: Boolean, default: false },
  syncSettings: {
    type: Object,
    default: () => ({ deviceName: "" }),
  },
  lanDevices: { type: Array, default: () => [] },
  lanScanning: { type: Boolean, default: false },
  lanSavingDeviceName: { type: Boolean, default: false },
  platform: { type: String, default: "web" },
});

const emit = defineEmits([
  "update:modelValue",
  "update:step",
  "update-theme-mode",
  "update-language",
  "complete",
  "submit-setup",
  "enable-biometric",
  "disable-biometric",
  "update-biometric-reauth-hours",
  "reveal-secret-key",
  "copy-secret-key",
  "save-device-name",
  "scan-lan",
]);

const isZh = computed(() => props.locale === "zh-CN");
const isBrowserPlatform = computed(() => props.platform === "web");
const passphraseRef = ref(null);
const deviceNameDraft = ref("");
const secretKeyVisible = ref(false);

const setupForm = reactive({
  passphrase: "",
  confirmPassphrase: "",
  reveal: false,
});

const setupErrors = reactive({
  passphrase: "",
  confirmPassphrase: "",
});

function normalizeStep(step) {
  return ["appearance", "setup", "security", "sharing"].includes(step) ? step : "appearance";
}

const currentStep = computed({
  get: () => normalizeStep(props.step),
  set: (value) => emit("update:step", normalizeStep(value)),
});

const copy = computed(() =>
  isZh.value
    ? {
        title: "欢迎使用 Password Vault",
        subtitle: "先完成几步初始设置，就像新手机第一次开机一样，然后再进入你的保险库。",
        appearanceTitle: "先选外观与语言",
        appearanceBody: "先把界面调成你最顺手的样子。这些偏好稍后都可以在设置里再改。",
        appearanceNote: "这些选择只影响显示体验，不会改变或降低你的密码安全性。",
        setupTitle: "创建主密码",
        setupBody: "主密码是这台设备上保险库的第一层保护。请设置一个你能长期记住、同时足够安全的密码。",
        securityTitle: "安全设置",
        securityBody: "这一步会处理生物识别和 Secret Key。你也可以先跳过，稍后再补。",
        sharingTitle: "分享设置",
        sharingBody: isBrowserPlatform.value
          ? "浏览器版不提供局域网设备发现，这里只展示当前平台真正可用的分享与同步方式。"
          : "给当前设备起一个容易识别的名称，方便后续通过局域网或 WebDAV 和其他设备同步。",
        next: "下一步",
        finish: "进入保险库",
        back: "上一步",
        skipSecurity: "稍后设置安全",
        skipSharing: "暂时跳过分享",
        required: "该字段不能为空",
        shortPass: "主密码至少需要 8 位",
        mismatch: "两次输入的主密码不一致",
        languageTitle: "语言",
        languageBody: "选择你希望看到的界面语言。",
        themeTitle: "主题模式",
        themeBody: "选择整体外观。跟随系统会自动匹配设备当前的深浅主题。",
        zh: "简体中文",
        en: "English",
        system: "跟随系统",
        light: "明亮模式",
        dark: "暗黑模式",
        changeLater: "之后也可以在设置里随时修改。",
        biometricTitle: "生物识别解锁",
        biometricBody: "启用后，下次解锁时可以优先使用设备验证，而不是每次都完整输入主密码。",
        biometricUnavailable: "当前设备暂时无法启用生物识别。",
        secretTitle: "Secret Key 是做什么的？",
        secretBody:
          "它是系统为这台设备额外生成的一把随机密钥，会和你的主密码一起参与解密。只拿到主密码，或者只拿到同步数据，都不足以直接解开保险库。",
        secretBullets: [
          "它不是主密码，不能由主密码反推出。",
          "同步、迁移或恢复数据时，主密码和 Secret Key 需要配合使用。",
          "请把它单独保存，不要只留在当前设备里。",
        ],
        revealSecret: "显示 Secret Key",
        hideSecret: "隐藏 Secret Key",
        copySecret: "复制 Secret Key",
        deviceTitle: "设备名称",
        deviceBody: "其他设备会看到这个名称，用它来识别当前设备的同步来源。",
        methodsTitle: isBrowserPlatform.value ? "当前平台支持" : "可用分享方式",
        methodsBody: isBrowserPlatform.value
          ? "浏览器版更适合使用 WebDAV 和文件导入导出。局域网扫描只会在 Windows / Android 宿主里提供。"
          : "局域网同步适合同一网络内快速传输，WebDAV 更适合长期保存加密快照。",
        nearbyTitle: "附近设备",
        nearbyBody: "你可以先扫描一下，看看同一局域网内是否已经有其他设备在线。",
        scan: "扫描设备",
        noDevices: "还没有发现其他设备",
        foundDevices: (count) => `已发现 ${count} 台设备`,
        save: "保存",
        none: "暂无",
        showPassword: "显示密码",
        hidePassword: "隐藏密码",
        masterPassword: "主密码",
        confirmPassword: "确认主密码",
        enableBiometric: "启用生物识别",
        disableBiometric: "关闭生物识别",
        deviceNamePlaceholder: "设备名称",
        noMethods: "当前没有可用方式",
      }
    : {
        title: "Welcome to Password Vault",
        subtitle: "Finish a few first-run steps, just like setting up a new phone, before entering your vault.",
        appearanceTitle: "Pick your look and language",
        appearanceBody: "Set up the interface first so everything feels familiar. You can change these later in Settings.",
        appearanceNote: "These preferences only affect presentation. They do not weaken your vault data.",
        setupTitle: "Create a master password",
        setupBody: "Your master password is the first layer of protection on this device. Choose something secure that you can still remember long-term.",
        securityTitle: "Security setup",
        securityBody: "This step covers biometric unlock and the Secret Key. You can skip it for now and return later.",
        sharingTitle: "Sharing setup",
        sharingBody: isBrowserPlatform.value
          ? "The browser build does not provide LAN discovery. This page only shows sharing options that exist on this platform."
          : "Give this device a friendly name so it is easier to recognize later during LAN or WebDAV sync.",
        next: "Next",
        finish: "Enter vault",
        back: "Back",
        skipSecurity: "Set up security later",
        skipSharing: "Skip sharing for now",
        required: "This field is required",
        shortPass: "Your master password must be at least 8 characters",
        mismatch: "The two master passwords do not match",
        languageTitle: "Language",
        languageBody: "Choose the language you want to use across the interface.",
        themeTitle: "Theme mode",
        themeBody: "Choose the overall look. System mode follows the device theme automatically.",
        zh: "Simplified Chinese",
        en: "English",
        system: "Follow system",
        light: "Light mode",
        dark: "Dark mode",
        changeLater: "You can change these later in Settings.",
        biometricTitle: "Biometric unlock",
        biometricBody: "Enable it now so the next unlock can prefer device authentication instead of typing the full password every time.",
        biometricUnavailable: "Biometrics are not available on this device right now.",
        secretTitle: "What is the Secret Key?",
        secretBody:
          "It is an additional random key generated for this device. It works together with your master password during decryption, so synced data is not protected by the password alone.",
        secretBullets: [
          "It is not your master password and cannot be derived back from it.",
          "You may need it together with your master password when moving or recovering your vault.",
          "Store it somewhere separate from this device.",
        ],
        revealSecret: "Reveal Secret Key",
        hideSecret: "Hide Secret Key",
        copySecret: "Copy Secret Key",
        deviceTitle: "Device name",
        deviceBody: "Other devices will see this name and use it to identify this device during sync.",
        methodsTitle: isBrowserPlatform.value ? "Available on this platform" : "Available sharing methods",
        methodsBody: isBrowserPlatform.value
          ? "The browser build focuses on WebDAV and file-based flows. LAN discovery is only available in the packaged Windows and Android apps."
          : "LAN sync is useful for quick transfers on the same network. WebDAV is better for long-term encrypted snapshots.",
        nearbyTitle: "Nearby devices",
        nearbyBody: "You can scan now to see whether another device is already available on the same network.",
        scan: "Scan devices",
        noDevices: "No other devices found yet",
        foundDevices: (count) => `${count} devices found`,
        save: "Save",
        none: "None",
        showPassword: "Show password",
        hidePassword: "Hide password",
        masterPassword: "Master password",
        confirmPassword: "Confirm master password",
        enableBiometric: "Enable biometrics",
        disableBiometric: "Disable biometrics",
        deviceNamePlaceholder: "Device name",
        noMethods: "No methods available right now",
      }
);

const themeOptions = computed(() => [
  { value: "system", label: copy.value.system },
  { value: "light", label: copy.value.light },
  { value: "dark", label: copy.value.dark },
]);

const languageOptions = computed(() => [
  { value: "zh-CN", label: copy.value.zh },
  { value: "en-US", label: copy.value.en },
]);

const reauthOptions = computed(() => [
  { value: 24, title: isZh.value ? "24 小时" : "24 hours" },
  { value: 72, title: isZh.value ? "72 小时" : "72 hours" },
  { value: 168, title: isZh.value ? "1 周" : "1 week" },
  { value: 720, title: isZh.value ? "1 个月" : "1 month" },
  { value: 0, title: isZh.value ? "永不" : "Never" },
]);

const steps = computed(() =>
  props.startWithSetup ? ["appearance", "setup", "security", "sharing"] : ["security", "sharing"]
);
const currentIndex = computed(() => Math.max(0, steps.value.indexOf(currentStep.value)));
const canSkip = computed(() => currentStep.value === "security" || currentStep.value === "sharing");
const canGoBack = computed(() => {
  if (currentIndex.value <= 0) return false;
  if (currentStep.value === "security" && props.startWithSetup && props.setupCompleted) return false;
  return true;
});

const currentTitle = computed(() => {
  if (currentStep.value === "appearance") return copy.value.appearanceTitle;
  if (currentStep.value === "setup") return copy.value.setupTitle;
  if (currentStep.value === "security") return copy.value.securityTitle;
  return copy.value.sharingTitle;
});

const currentBody = computed(() => {
  if (currentStep.value === "appearance") return copy.value.appearanceBody;
  if (currentStep.value === "setup") return copy.value.setupBody;
  if (currentStep.value === "security") return copy.value.securityBody;
  return copy.value.sharingBody;
});

const methods = computed(() => {
  const list = [];
  if (props.supportsLanSync) list.push(isZh.value ? "局域网同步" : "LAN sync");
  if (props.supportsWebDavSync) list.push("WebDAV");
  return list;
});

const showShareDeviceName = computed(
  () => !isBrowserPlatform.value && (props.supportsLanSync || props.supportsWebDavSync)
);

const secretKeyToggleLabel = computed(() =>
  secretKeyVisible.value ? copy.value.hideSecret : copy.value.revealSecret
);

const secretKeyDisplay = computed(() => {
  if (secretKeyVisible.value && props.secretKeyValue) {
    return props.secretKeyValue;
  }

  if (props.secretKeyHint) {
    return props.secretKeyHint;
  }

  return isZh.value ? "默认隐藏，点击按钮后显示" : "Hidden until you choose to reveal it";
});

const ambientBadges = computed(() => {
  if (currentStep.value === "appearance") {
    return ["Aa", "🎨", "✨", "•"];
  }

  if (currentStep.value === "setup") {
    return ["🔐", "✦", "••", "🫧"];
  }

  if (currentStep.value === "security") {
    return ["🛡️", "🔑", "✨", "•"];
  }

  return ["📶", "🤝", "✨", "•"];
});

function resetWizard() {
  setupForm.passphrase = "";
  setupForm.confirmPassphrase = "";
  setupForm.reveal = false;
  setupErrors.passphrase = "";
  setupErrors.confirmPassphrase = "";
  deviceNameDraft.value = props.syncSettings?.deviceName || "";
  secretKeyVisible.value = false;
}

async function focusSetupPassphrase() {
  await nextTick();
  window.setTimeout(() => {
    passphraseRef.value?.focus?.();
    passphraseRef.value?.select?.();
  }, 100);
}

function goToNextStep() {
  const nextIndex = currentIndex.value + 1;
  if (nextIndex >= steps.value.length) {
    emit("complete");
    emit("update:modelValue", false);
    return;
  }

  currentStep.value = steps.value[nextIndex];
}

function goBack() {
  if (!canGoBack.value) return;
  const prevIndex = currentIndex.value - 1;
  if (prevIndex >= 0) currentStep.value = steps.value[prevIndex];
}

function skipCurrentStep() {
  if (canSkip.value) goToNextStep();
}

function validateSetupStep() {
  setupErrors.passphrase = "";
  setupErrors.confirmPassphrase = "";
  const passphrase = String(setupForm.passphrase || "");
  const confirm = String(setupForm.confirmPassphrase || "");

  if (!passphrase.trim()) setupErrors.passphrase = copy.value.required;
  else if (passphrase.length < 8) setupErrors.passphrase = copy.value.shortPass;

  if (!confirm.trim()) setupErrors.confirmPassphrase = copy.value.required;
  else if (confirm !== passphrase) setupErrors.confirmPassphrase = copy.value.mismatch;

  return !setupErrors.passphrase && !setupErrors.confirmPassphrase;
}

function submitSetupStep() {
  if (!validateSetupStep()) return;
  emit("submit-setup", { passphrase: setupForm.passphrase });
}

function saveDeviceName() {
  emit("save-device-name", deviceNameDraft.value.trim());
}

function handleToggleSecretKey() {
  if (secretKeyVisible.value) {
    secretKeyVisible.value = false;
    return;
  }

  secretKeyVisible.value = true;
  if (!props.secretKeyValue) {
    emit("reveal-secret-key");
  }
}

watch(
  () => props.modelValue,
  (visible) => {
    if (!visible) return;
    resetWizard();
    if (currentStep.value === "setup" && !props.setupCompleted) void focusSetupPassphrase();
  },
  { immediate: true }
);

watch(
  () => props.setupCompleted,
  (completed) => {
    if (completed && currentStep.value === "setup") currentStep.value = "security";
  }
);

watch(currentStep, (step) => {
  if (props.modelValue && step === "setup" && !props.setupCompleted) void focusSetupPassphrase();
  if (step !== "security") {
    secretKeyVisible.value = false;
  }
});

watch(
  () => props.syncSettings?.deviceName,
  (value) => {
    deviceNameDraft.value = value || "";
  },
  { immediate: true }
);

watch(
  () => props.secretKeyValue,
  (value) => {
    if (!value) {
      secretKeyVisible.value = false;
    }
  }
);
</script>

<template>
  <Teleport to="body">
    <Transition name="first-run-guide" appear>
      <section v-if="modelValue" class="first-run-guide" role="dialog" aria-modal="true">
        <div class="first-run-guide__ambient" aria-hidden="true">
          <div class="first-run-guide__mesh first-run-guide__mesh--a"></div>
          <div class="first-run-guide__mesh first-run-guide__mesh--b"></div>
          <div class="first-run-guide__beam first-run-guide__beam--a"></div>
          <div class="first-run-guide__beam first-run-guide__beam--b"></div>
          <div class="first-run-guide__orb first-run-guide__orb--a"></div>
          <div class="first-run-guide__orb first-run-guide__orb--b"></div>
          <div class="first-run-guide__orb first-run-guide__orb--c"></div>
          <span
            v-for="(badge, index) in ambientBadges"
            :key="`${currentStep}-${index}`"
            class="first-run-guide__badge"
            :class="`first-run-guide__badge--${index + 1}`"
          >
            {{ badge }}
          </span>
          <span class="first-run-guide__spark first-run-guide__spark--1"></span>
          <span class="first-run-guide__spark first-run-guide__spark--2"></span>
          <span class="first-run-guide__spark first-run-guide__spark--3"></span>
          <span class="first-run-guide__spark first-run-guide__spark--4"></span>
        </div>
        <div class="first-run-guide__shell">
          <div class="first-run-guide__hero">
            <div class="first-run-guide__hero-chip">{{ currentIndex + 1 }}/{{ steps.length }}</div>
            <h1 class="first-run-guide__title">{{ copy.title }}</h1>
            <p class="first-run-guide__subtitle">{{ copy.subtitle }}</p>
          </div>

          <div class="first-run-guide__panel">
            <Transition name="first-run-step" mode="out-in">
              <div :key="currentStep">
                <div class="first-run-guide__step-intro">
                  <div class="first-run-guide__step-title">{{ currentTitle }}</div>
                  <div class="first-run-guide__step-body">{{ currentBody }}</div>
                </div>

                <div v-if="currentStep === 'appearance'" class="first-run-guide__stack">
                  <div class="first-run-guide__card">
                    <div class="first-run-guide__card-title">{{ copy.languageTitle }}</div>
                    <div class="first-run-guide__card-body">{{ copy.languageBody }}</div>
                    <div class="first-run-guide__choice-grid">
                      <button
                        v-for="item in languageOptions"
                        :key="item.value"
                        type="button"
                        class="first-run-guide__option-card"
                        :class="{ 'first-run-guide__option-card--active': locale === item.value }"
                        @click="emit('update-language', item.value)"
                      >
                        {{ item.label }}
                      </button>
                    </div>
                  </div>

                  <div class="first-run-guide__card">
                    <div class="first-run-guide__card-title">{{ copy.themeTitle }}</div>
                    <div class="first-run-guide__card-body">{{ copy.themeBody }}</div>
                    <div class="first-run-guide__choice-grid first-run-guide__choice-grid--three">
                      <button
                        v-for="item in themeOptions"
                        :key="item.value"
                        type="button"
                        class="first-run-guide__option-card"
                        :class="{ 'first-run-guide__option-card--active': themeMode === item.value }"
                        @click="emit('update-theme-mode', item.value)"
                      >
                        {{ item.label }}
                      </button>
                    </div>
                    <div class="first-run-guide__note">{{ copy.changeLater }}</div>
                    <div class="first-run-guide__note first-run-guide__note--muted">{{ copy.appearanceNote }}</div>
                  </div>
                </div>

                <form v-else-if="currentStep === 'setup'" class="first-run-guide__stack" @submit.prevent="submitSetupStep">
                  <div class="first-run-guide__field" :class="{ 'first-run-guide__field--invalid': Boolean(setupErrors.passphrase) }">
                    <label class="first-run-guide__field-label">{{ copy.masterPassword }}</label>
                    <input
                      ref="passphraseRef"
                      v-model="setupForm.passphrase"
                      type="text"
                      class="first-run-guide__field-input"
                      :class="{ 'first-run-guide__field-input--masked': !setupForm.reveal }"
                      autocomplete="off"
                      autocorrect="off"
                      autocapitalize="none"
                      spellcheck="false"
                    />
                    <div v-if="setupErrors.passphrase" class="first-run-guide__field-error">{{ setupErrors.passphrase }}</div>
                  </div>

                  <div class="first-run-guide__field" :class="{ 'first-run-guide__field--invalid': Boolean(setupErrors.confirmPassphrase) }">
                    <label class="first-run-guide__field-label">{{ copy.confirmPassword }}</label>
                    <input
                      v-model="setupForm.confirmPassphrase"
                      type="text"
                      class="first-run-guide__field-input"
                      :class="{ 'first-run-guide__field-input--masked': !setupForm.reveal }"
                      autocomplete="off"
                      autocorrect="off"
                      autocapitalize="none"
                      spellcheck="false"
                    />
                    <div v-if="setupErrors.confirmPassphrase" class="first-run-guide__field-error">{{ setupErrors.confirmPassphrase }}</div>
                  </div>

                  <button type="button" class="first-run-guide__link" @click="setupForm.reveal = !setupForm.reveal">
                    {{ setupForm.reveal ? copy.hidePassword : copy.showPassword }}
                  </button>

                  <v-btn block size="x-large" color="primary" class="first-run-guide__primary" :loading="setupLoading" @click="submitSetupStep">
                    {{ copy.next }}
                  </v-btn>
                </form>

                <div v-else-if="currentStep === 'security'" class="first-run-guide__stack">
                  <div class="first-run-guide__card">
                    <div class="first-run-guide__card-title">{{ copy.biometricTitle }}</div>
                    <div class="first-run-guide__card-body">{{ biometricAvailable ? copy.biometricBody : copy.biometricUnavailable }}</div>
                    <div class="d-flex flex-wrap ga-2 mt-4">
                      <v-btn
                        v-if="biometricSupported"
                        :color="biometricEnabled ? undefined : 'primary'"
                        :variant="biometricEnabled ? 'text' : 'elevated'"
                        prepend-icon="mdi-fingerprint"
                        :disabled="!biometricEnabled && !biometricAvailable"
                        :loading="biometricLoading"
                        @click="emit(biometricEnabled ? 'disable-biometric' : 'enable-biometric')"
                      >
                        {{ biometricEnabled ? copy.disableBiometric : copy.enableBiometric }}
                      </v-btn>
                    </div>
                    <div v-if="biometricSupported" class="first-run-guide__chip-group">
                      <button
                        v-for="item in reauthOptions"
                        :key="item.value"
                        type="button"
                        class="first-run-guide__choice-chip"
                        :class="{ 'first-run-guide__choice-chip--active': Number(biometricReauthHours) === Number(item.value) }"
                        @click="emit('update-biometric-reauth-hours', item.value)"
                      >
                        {{ item.title }}
                      </button>
                    </div>
                  </div>

                  <div class="first-run-guide__card">
                    <div class="first-run-guide__card-title">{{ copy.secretTitle }}</div>
                    <div class="first-run-guide__card-body">{{ copy.secretBody }}</div>
                    <ul class="first-run-guide__bullets">
                      <li v-for="item in copy.secretBullets" :key="item">{{ item }}</li>
                    </ul>
                    <div
                      class="first-run-guide__secret"
                      :class="{ 'first-run-guide__secret--revealed': secretKeyVisible }"
                    >
                      {{ secretKeyDisplay }}
                    </div>
                    <div class="d-flex flex-wrap ga-2 mt-4">
                      <v-btn color="primary" prepend-icon="mdi-key-outline" :loading="secretKeyLoading" @click="handleToggleSecretKey">
                        {{ secretKeyToggleLabel }}
                      </v-btn>
                      <v-btn variant="tonal" prepend-icon="mdi-content-copy" :disabled="!secretKeyValue" @click="emit('copy-secret-key')">
                        {{ copy.copySecret }}
                      </v-btn>
                    </div>
                  </div>
                </div>

                <div v-else class="first-run-guide__stack">
                  <div v-if="showShareDeviceName" class="first-run-guide__card">
                    <div class="first-run-guide__card-title">{{ copy.deviceTitle }}</div>
                    <div class="first-run-guide__card-body">{{ copy.deviceBody }}</div>
                    <div class="first-run-guide__device-row">
                      <input v-model="deviceNameDraft" type="text" class="first-run-guide__device-input" :placeholder="copy.deviceNamePlaceholder" />
                      <v-btn color="primary" prepend-icon="mdi-content-save-outline" :loading="lanSavingDeviceName" @click="saveDeviceName">
                        {{ copy.save }}
                      </v-btn>
                    </div>
                  </div>

                  <div class="first-run-guide__card">
                    <div class="first-run-guide__card-title">{{ copy.methodsTitle }}</div>
                    <div class="first-run-guide__card-body">{{ copy.methodsBody }}</div>
                    <div class="first-run-guide__chip-group">
                      <span v-for="method in methods" :key="method" class="first-run-guide__method-chip">{{ method }}</span>
                      <span v-if="!methods.length" class="first-run-guide__method-chip first-run-guide__method-chip--muted">{{ copy.noMethods }}</span>
                    </div>
                  </div>

                  <div v-if="supportsLanSync" class="first-run-guide__card">
                    <div class="first-run-guide__card-title">{{ copy.nearbyTitle }}</div>
                    <div class="first-run-guide__card-body">{{ copy.nearbyBody }}</div>
                    <div class="d-flex flex-wrap align-center ga-3 mt-4">
                      <v-btn color="primary" prepend-icon="mdi-radar" :loading="lanScanning" @click="emit('scan-lan')">
                        {{ copy.scan }}
                      </v-btn>
                      <div class="text-body-2 text-medium-emphasis">
                        {{ lanDevices.length ? copy.foundDevices(lanDevices.length) : copy.noDevices }}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </Transition>
          </div>

          <div v-if="currentStep !== 'setup'" class="first-run-guide__footer">
            <v-btn v-if="canGoBack" variant="text" @click="goBack">{{ copy.back }}</v-btn>
            <span v-else></span>
            <div class="d-flex align-center ga-2">
              <v-btn v-if="canSkip" variant="text" @click="skipCurrentStep">
                {{ currentStep === "security" ? copy.skipSecurity : copy.skipSharing }}
              </v-btn>
              <v-btn color="primary" @click="goToNextStep">
                {{ currentStep === "sharing" ? copy.finish : copy.next }}
              </v-btn>
            </div>
          </div>
        </div>
      </section>
    </Transition>
  </Teleport>
</template>

<style scoped>
.first-run-guide{position:fixed;inset:0;z-index:2150;display:flex;justify-content:center;padding:calc(var(--host-safe-top-effective) + 20px) max(18px,3.4vw) calc(var(--host-safe-bottom-effective) + 22px);overflow:hidden;background:radial-gradient(circle at top left,rgba(var(--v-theme-primary),.1),transparent 28%),radial-gradient(circle at top right,rgba(var(--v-theme-secondary),.08),transparent 24%),linear-gradient(180deg,rgba(var(--v-theme-background),.98),rgba(var(--v-theme-background),1))}
.first-run-guide__ambient{position:absolute;inset:0;background:radial-gradient(circle at 50% 0%,rgba(255,255,255,.12),transparent 26%),linear-gradient(180deg,transparent 0%,rgba(255,255,255,.03) 100%);pointer-events:none}
.first-run-guide__mesh,.first-run-guide__beam,.first-run-guide__orb,.first-run-guide__badge,.first-run-guide__spark{position:absolute}
.first-run-guide__mesh{inset:auto;filter:blur(18px);opacity:.68;mix-blend-mode:screen;animation:first-run-mesh-drift 18s ease-in-out infinite}
.first-run-guide__mesh--a{top:-8%;left:12%;width:42vw;max-width:360px;height:42vw;max-height:360px;background:radial-gradient(circle,rgba(var(--v-theme-primary),.24) 0%,rgba(var(--v-theme-primary),.08) 42%,transparent 72%)}
.first-run-guide__mesh--b{right:-4%;bottom:6%;width:34vw;max-width:300px;height:34vw;max-height:300px;background:radial-gradient(circle,rgba(var(--v-theme-secondary),.22) 0%,rgba(var(--v-theme-secondary),.08) 46%,transparent 72%);animation-delay:-8s}
.first-run-guide__beam{height:180px;width:68vw;max-width:520px;border-radius:999px;filter:blur(24px);opacity:.42;transform:rotate(-14deg);animation:first-run-beam-sweep 14s ease-in-out infinite}
.first-run-guide__beam--a{top:12%;left:-12%;background:linear-gradient(90deg,transparent,rgba(255,255,255,.18),transparent)}
.first-run-guide__beam--b{right:-12%;bottom:18%;background:linear-gradient(90deg,transparent,rgba(var(--v-theme-primary),.22),transparent);animation-delay:-5s}
.first-run-guide__orb{border-radius:999px;filter:blur(8px);opacity:.7;animation:first-run-orb-float 12s ease-in-out infinite}
.first-run-guide__orb--a{top:8%;left:6%;width:180px;height:180px;background:rgba(var(--v-theme-primary),.12)}
.first-run-guide__orb--b{top:18%;right:8%;width:136px;height:136px;background:rgba(var(--v-theme-secondary),.12);animation-delay:-3s}
.first-run-guide__orb--c{bottom:16%;left:18%;width:120px;height:120px;background:rgba(255,255,255,.08);animation-delay:-6s}
.first-run-guide__badge{display:flex;align-items:center;justify-content:center;min-width:44px;min-height:44px;padding:0 14px;border-radius:999px;background:rgba(255,255,255,.14);backdrop-filter:blur(12px);-webkit-backdrop-filter:blur(12px);font-size:1rem;font-weight:700;animation:first-run-badge-drift 10s ease-in-out infinite}
.first-run-guide__badge--1{top:12%;right:18%}
.first-run-guide__badge--2{top:30%;left:8%;animation-delay:-2s}
.first-run-guide__badge--3{bottom:22%;right:12%;animation-delay:-4s}
.first-run-guide__badge--4{bottom:10%;left:26%;animation-delay:-6s}
.first-run-guide__spark{width:8px;height:8px;border-radius:999px;background:rgba(255,255,255,.82);box-shadow:0 0 18px rgba(255,255,255,.52);animation:first-run-spark-twinkle 4.6s ease-in-out infinite}
.first-run-guide__spark--1{top:18%;left:22%}
.first-run-guide__spark--2{top:24%;right:22%;animation-delay:-1.2s}
.first-run-guide__spark--3{bottom:24%;left:16%;animation-delay:-2.4s}
.first-run-guide__spark--4{bottom:14%;right:28%;animation-delay:-3.1s}
.first-run-guide__shell{position:relative;z-index:1;width:min(100%,760px);display:flex;flex-direction:column;gap:20px}
.first-run-guide__hero{padding:18px 6px 0}
.first-run-guide__hero-chip,.first-run-guide__choice-chip,.first-run-guide__method-chip{display:inline-flex;align-items:center;justify-content:center;border-radius:999px}
.first-run-guide__hero-chip{min-height:30px;padding:0 14px;background:rgba(var(--v-theme-surface),.78);color:rgba(var(--v-theme-on-surface),.72);font-size:.84rem;font-weight:700}
.first-run-guide__title{margin:16px 0 0;font-size:clamp(2rem,4.2vw,3.3rem);line-height:1.04;letter-spacing:-.05em;font-weight:700}
.first-run-guide__subtitle{margin:12px 0 0;font-size:1rem;line-height:1.7;color:rgba(var(--v-theme-on-surface),.72)}
.first-run-guide__panel{flex:1 1 auto;padding:clamp(20px,4vw,28px);border-radius:32px;background:rgba(var(--v-theme-surface),.8);box-shadow:0 30px 60px rgba(18,24,34,.14);backdrop-filter:blur(26px);-webkit-backdrop-filter:blur(26px)}
.first-run-guide__step-intro{margin-bottom:18px}
.first-run-guide__step-title{font-size:1.3rem;font-weight:700;letter-spacing:-.02em}
.first-run-guide__step-body,.first-run-guide__card-body,.first-run-guide__note{margin-top:10px;color:rgba(var(--v-theme-on-surface),.68);line-height:1.7}
.first-run-guide__stack{display:flex;flex-direction:column;gap:16px}
.first-run-guide__card,.first-run-guide__field{padding:18px 18px 20px;border-radius:26px;background:rgba(var(--v-theme-surface-variant),.66)}
.first-run-guide__field{display:flex;flex-direction:column;gap:10px}
.first-run-guide__field--invalid{box-shadow:0 0 0 1px rgba(var(--v-theme-error),.28)}
.first-run-guide__field-label,.first-run-guide__card-title{font-size:.98rem;font-weight:700}
.first-run-guide__field-input,.first-run-guide__device-input{width:100%;min-height:30px;padding:0;border:none;outline:none;background:transparent;color:rgb(var(--v-theme-on-surface));font:inherit;font-size:1.06rem}
.first-run-guide__field-input--masked{-webkit-text-security:disc}
.first-run-guide__field-error{color:rgb(var(--v-theme-error));font-size:.9rem}
.first-run-guide__link{align-self:flex-start;border:none;background:transparent;color:rgba(var(--v-theme-on-surface),.68);font:inherit;font-weight:600;cursor:pointer}
.first-run-guide__primary{min-height:58px}
.first-run-guide__choice-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px;margin-top:16px}
.first-run-guide__choice-grid--three{grid-template-columns:repeat(3,minmax(0,1fr))}
.first-run-guide__option-card{display:flex;align-items:center;justify-content:center;min-height:56px;padding:0 14px;border:none;border-radius:20px;background:rgba(var(--v-theme-on-surface),.05);color:rgba(var(--v-theme-on-surface),.8);font:inherit;font-weight:700;cursor:pointer;transition:transform .18s ease,background-color .18s ease,color .18s ease}
.first-run-guide__option-card:hover{transform:translateY(-1px)}
.first-run-guide__option-card--active,.first-run-guide__choice-chip--active{background:rgba(var(--v-theme-primary),.12);color:rgb(var(--v-theme-primary))}
.first-run-guide__note--muted{color:rgba(var(--v-theme-on-surface),.56)}
.first-run-guide__chip-group{display:flex;flex-wrap:wrap;gap:10px;margin-top:16px}
.first-run-guide__choice-chip,.first-run-guide__method-chip{min-height:40px;padding:0 14px;border:none;background:rgba(var(--v-theme-on-surface),.06);color:rgba(var(--v-theme-on-surface),.74);font:inherit;font-size:.92rem;font-weight:600}
.first-run-guide__choice-chip{cursor:pointer}
.first-run-guide__bullets{margin:16px 0 0;padding-left:18px;color:rgba(var(--v-theme-on-surface),.78);line-height:1.7}
.first-run-guide__bullets li+li{margin-top:6px}
.first-run-guide__secret{margin-top:16px;padding:14px 16px;border-radius:20px;background:rgba(var(--v-theme-on-surface),.04);color:rgba(var(--v-theme-on-surface),.82);word-break:break-word}
.first-run-guide__secret--revealed{font-weight:600;letter-spacing:.01em}
.first-run-guide__device-row{display:grid;grid-template-columns:minmax(0,1fr) auto;gap:12px;margin-top:16px;align-items:center}
.first-run-guide__device-input{min-height:52px;padding:0 16px;border-radius:20px;background:rgba(var(--v-theme-on-surface),.04)}
.first-run-guide__method-chip--muted{opacity:.7}
.first-run-guide__footer{display:flex;align-items:center;justify-content:space-between;gap:16px;padding:0 6px 2px}
.first-run-guide-enter-active,.first-run-guide-leave-active{transition:opacity .32s ease}
.first-run-guide-enter-from,.first-run-guide-leave-to{opacity:0}
.first-run-step-enter-active,.first-run-step-leave-active{transition:opacity .26s ease,transform .26s cubic-bezier(.22,1,.36,1)}
.first-run-step-enter-from,.first-run-step-leave-to{opacity:0;transform:translateY(12px)}
@keyframes first-run-orb-float{0%,100%{transform:translate3d(0,0,0) scale(1)}50%{transform:translate3d(0,-12px,0) scale(1.06)}}
@keyframes first-run-badge-drift{0%,100%{transform:translate3d(0,0,0)}50%{transform:translate3d(0,-10px,0)}}
@keyframes first-run-mesh-drift{0%,100%{transform:translate3d(0,0,0) scale(1)}50%{transform:translate3d(12px,-10px,0) scale(1.08)}}
@keyframes first-run-beam-sweep{0%,100%{transform:translate3d(0,0,0) rotate(-14deg) scaleX(1)}50%{transform:translate3d(18px,-12px,0) rotate(-9deg) scaleX(1.06)}}
@keyframes first-run-spark-twinkle{0%,100%{opacity:.32;transform:scale(.72)}50%{opacity:1;transform:scale(1.18)}}
:global(html[data-theme-mode="dark"]) .first-run-guide{background:radial-gradient(circle at top left,rgba(var(--v-theme-primary),.16),transparent 28%),radial-gradient(circle at top right,rgba(var(--v-theme-secondary),.08),transparent 24%),linear-gradient(180deg,rgba(10,14,20,.98),rgba(8,11,16,1))}
:global(html[data-theme-mode="dark"]) .first-run-guide__panel{background:rgba(var(--v-theme-surface),.8);box-shadow:0 30px 64px rgba(0,0,0,.32)}
:global(html[data-theme-mode="dark"]) .first-run-guide__card,:global(html[data-theme-mode="dark"]) .first-run-guide__field{background:rgba(var(--v-theme-surface-variant),.72)}
:global(html[data-theme-mode="dark"]) .first-run-guide__device-input,:global(html[data-theme-mode="dark"]) .first-run-guide__secret,:global(html[data-theme-mode="dark"]) .first-run-guide__choice-chip,:global(html[data-theme-mode="dark"]) .first-run-guide__method-chip,:global(html[data-theme-mode="dark"]) .first-run-guide__hero-chip,:global(html[data-theme-mode="dark"]) .first-run-guide__option-card{background:rgba(255,255,255,.05)}
:global(html[data-theme-mode="dark"]) .first-run-guide__option-card--active,:global(html[data-theme-mode="dark"]) .first-run-guide__choice-chip--active{background:rgba(var(--v-theme-primary),.14)}
:global(html[data-theme-mode="dark"]) .first-run-guide__badge{background:rgba(255,255,255,.08)}
:global(html[data-theme-mode="dark"]) .first-run-guide__beam--a{background:linear-gradient(90deg,transparent,rgba(255,255,255,.12),transparent)}
:global(html[data-theme-mode="dark"]) .first-run-guide__beam--b{background:linear-gradient(90deg,transparent,rgba(var(--v-theme-primary),.18),transparent)}
@media (max-width:640px){
  .first-run-guide{padding:calc(var(--host-safe-top-effective) + 14px) 14px calc(var(--host-safe-bottom-effective) + 18px)}
  .first-run-guide__panel{padding:18px;border-radius:28px}
  .first-run-guide__choice-grid,.first-run-guide__choice-grid--three{grid-template-columns:1fr}
  .first-run-guide__device-row{grid-template-columns:1fr}
  .first-run-guide__footer{flex-direction:column;align-items:stretch}
  .first-run-guide__footer>:last-child{display:grid;grid-template-columns:1fr}
}
</style>
