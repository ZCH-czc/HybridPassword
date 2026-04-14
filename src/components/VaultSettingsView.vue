<script setup>
import { computed, ref } from "vue";
import packageInfo from "../../package.json";
import InlineSvgIcon from "@/components/InlineSvgIcon.vue";
import AppLogStatusCard from "@/components/AppLogStatusCard.vue";
import PendingImportReviewCard from "@/components/PendingImportReviewCard.vue";
import PasskeyAppStatusCard from "@/components/PasskeyAppStatusCard.vue";
import PasskeyMetadataCard from "@/components/PasskeyMetadataCard.vue";
import { useAppPreferences } from "@/composables/useAppPreferences";
import DeletedList from "@/components/DeletedList.vue";
import ImportExportCard from "@/components/ImportExportCard.vue";
import LanSyncCard from "@/components/LanSyncCard.vue";
import { openExternalUrl } from "@/utils/native-bridge";
import WebDavSyncCard from "@/components/WebDavSyncCard.vue";

const props = defineProps({
  importStrategy: {
    type: String,
    default: "overwrite",
  },
  recordCount: {
    type: Number,
    default: 0,
  },
  deletedItems: {
    type: Array,
    default: () => [],
  },
  deletedBusyIds: {
    type: Object,
    default: () => ({}),
  },
  deletedBatchLoading: {
    type: Boolean,
    default: false,
  },
  pendingImportReviewItems: {
    type: Array,
    default: () => [],
  },
  pendingImportReviewUpdatedAt: {
    type: Number,
    default: 0,
  },
  pendingImportReviewBusy: {
    type: Boolean,
    default: false,
  },
  appLogs: {
    type: Array,
    default: () => [],
  },
  appLogsUpdatedAt: {
    type: Number,
    default: 0,
  },
  appLogExporting: {
    type: Boolean,
    default: false,
  },
  nativeFileDialogsAvailable: {
    type: Boolean,
    default: false,
  },
  busy: {
    type: Boolean,
    default: false,
  },
  themeMode: {
    type: String,
    default: "system",
  },
  resolvedTheme: {
    type: String,
    default: "light",
  },
  navAlignment: {
    type: String,
    default: "center",
  },
  locale: {
    type: String,
    default: "zh-CN",
  },
  changingMasterPassword: {
    type: Boolean,
    default: false,
  },
  clearingData: {
    type: Boolean,
    default: false,
  },
  biometricSupported: {
    type: Boolean,
    default: false,
  },
  biometricAvailable: {
    type: Boolean,
    default: false,
  },
  biometricEnabled: {
    type: Boolean,
    default: false,
  },
  biometricLabel: {
    type: String,
    default: "Biometrics",
  },
  biometricMessage: {
    type: String,
    default: "",
  },
  biometricLoading: {
    type: Boolean,
    default: false,
  },
  biometricReauthHours: {
    type: Number,
    default: 72,
  },
  secretKeyHint: {
    type: String,
    default: "",
  },
  secretKeyValue: {
    type: String,
    default: "",
  },
  secretKeyLoading: {
    type: Boolean,
    default: false,
  },
  platform: {
    type: String,
    default: "web",
  },
  supportsMinimizeToTray: {
    type: Boolean,
    default: false,
  },
  minimizeToTrayEnabled: {
    type: Boolean,
    default: false,
  },
  supportsLaunchAtStartup: {
    type: Boolean,
    default: false,
  },
  launchAtStartupEnabled: {
    type: Boolean,
    default: false,
  },
  trayAutoLockMinutes: {
    type: Number,
    default: 0,
  },
  supportsExcludeFromRecents: {
    type: Boolean,
    default: false,
  },
  excludeFromRecentsEnabled: {
    type: Boolean,
    default: false,
  },
  backgroundAutoLockMinutes: {
    type: Number,
    default: 0,
  },
  supportsAutostartSettingsShortcut: {
    type: Boolean,
    default: false,
  },
  platformSettingsLoading: {
    type: Boolean,
    default: false,
  },
  autostartOpening: {
    type: Boolean,
    default: false,
  },
  supportsWebDavSync: {
    type: Boolean,
    default: false,
  },
  supportsLanSync: {
    type: Boolean,
    default: false,
  },
  supportsPasskeys: {
    type: Boolean,
    default: false,
  },
  passkeyState: {
    type: Object,
    default: () => ({
      isSupported: false,
      supportsMetadataManagement: false,
      supportsPluginManager: false,
      requiresCompanionApp: true,
      companionAppIntegrated: false,
      canLaunchCompanionApp: false,
      supportsCompanionAutoLaunch: false,
      companionAutoLaunchEnabled: false,
      apiVersion: 0,
      hasPlatformAuthenticator: false,
      platform: "web",
      message: "",
      pluginStatus: "",
      companionCheckedAtUnixTimeMs: 0,
      companionBuildNumber: 0,
      companionUbr: 0,
      companionMeetsPluginBuildRequirement: false,
      companionWebAuthnLibraryAvailable: false,
      companionPluginExportsAvailable: false,
      companionIsPackagedProcess: false,
      companionStatusSummary: "",
      companionDetailMessage: "",
      companionWorkflowMode: "skeleton",
      companionRegistrationAttempted: false,
      companionRegistrationPrepared: false,
      companionRegistrationEnvironmentReady: false,
      companionRegistrationCompleted: false,
      companionLastRegistrationAttemptUnixTimeMs: 0,
      companionRegistrationStatus: "",
      companionLastRegistrationMessage: "",
      companionLastRegistrationHResultHex: "",
      companionAuthenticatorStateCode: 0,
      companionAuthenticatorStateLabel: "unknown",
      companionHasOperationSigningPublicKey: false,
      companionOperationSigningPublicKeyStoredAtUnixTimeMs: 0,
      companionComSkeletonReady: false,
      companionComClassIdMatchesManifest: false,
      companionComFactoryReady: false,
      companionComAuthenticatorReady: false,
    companionComLastProbeUnixTimeMs: 0,
    companionComLastProbeMessage: "",
    companionComAuthenticatorTypeName: "",
    companionComClassFactoryRegistered: false,
    companionComClassFactoryRegistrationCookie: 0,
    companionComClassFactoryLastRegistrationUnixTimeMs: 0,
    companionComClassFactoryLastMessage: "",
    companionComClassFactoryLastHResultHex: "",
    companionCallbackTotalCount: 0,
    companionCallbackMakeCredentialCount: 0,
    companionCallbackGetAssertionCount: 0,
    companionCallbackCancelOperationCount: 0,
    companionCallbackGetLockStatusCount: 0,
    companionCallbackLastUnixTimeMs: 0,
    companionCallbackLastKind: "",
    companionCallbackLastMessage: "",
    companionCallbackLastHResultHex: "",
    companionLatestOperationId: "",
    companionLatestOperationKind: "",
    companionLatestOperationState: "idle",
    companionLatestOperationSource: "",
    companionLatestOperationCreatedAtUnixTimeMs: 0,
    companionLatestOperationUpdatedAtUnixTimeMs: 0,
    companionLatestOperationRequestPointerPresent: false,
    companionLatestOperationResponsePointerPresent: false,
    companionLatestOperationCancelPointerPresent: false,
    companionLatestOperationMessage: "",
    companionLatestOperationHResultHex: "",
    companionActivationCount: 0,
      companionLastActivationUnixTimeMs: 0,
      companionLastActivationSource: "",
      companionStartedFromPluginActivation: false,
      companionCreateRequestCount: 0,
      companionLastCreateRequestUnixTimeMs: 0,
      companionLastCreateRequestRpId: "",
      companionLastCreateRequestUsername: "",
      companionLastCreateRequestMessage: "",
      recentLogs: [],
    }),
  },
  passkeyItems: {
    type: Array,
    default: () => [],
  },
  deletedPasskeyItems: {
    type: Array,
    default: () => [],
  },
  passkeyRefreshing: {
    type: Boolean,
    default: false,
  },
  passkeyLaunching: {
    type: Boolean,
    default: false,
  },
  passkeyAutoLaunchSaving: {
    type: Boolean,
    default: false,
  },
  passkeyOperationResolving: {
    type: Boolean,
    default: false,
  },
  passkeyBusyIds: {
    type: Object,
    default: () => ({}),
  },
  syncSettings: {
    type: Object,
    default: () => ({
      deviceId: "",
      deviceName: "",
      webDav: {
        baseUrl: "",
        remotePath: "",
        username: "",
        hasPassword: false,
      },
    }),
  },
  lanDevices: {
    type: Array,
    default: () => [],
  },
  webDavSaving: {
    type: Boolean,
    default: false,
  },
  webDavTransfering: {
    type: Boolean,
    default: false,
  },
  lanScanning: {
    type: Boolean,
    default: false,
  },
  lanSavingDeviceName: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits([
  "update:importStrategy",
  "export",
  "import",
  "lock",
  "update-theme-mode",
  "update-nav-alignment",
  "update-language",
  "change-master-password",
  "enable-biometric",
  "disable-biometric",
  "update-biometric-reauth-hours",
  "reveal-secret-key",
  "copy-secret-key",
  "toggle-minimize-to-tray",
  "toggle-launch-at-startup",
  "update-tray-auto-lock-minutes",
  "toggle-exclude-from-recents",
  "update-background-auto-lock-minutes",
  "open-autostart-settings",
  "save-webdav-settings",
  "upload-webdav",
  "download-webdav",
  "save-device-name",
  "scan-lan",
  "sync-lan-device",
  "refresh-passkeys",
  "launch-passkey-companion",
  "restart-passkey-companion",
  "toggle-passkey-companion-auto-launch",
  "resolve-passkey-operation",
  "remove-passkey",
  "restore-passkey",
  "permanent-delete-passkey",
  "clear-all-data",
  "restore",
  "permanent-delete",
  "restore-many",
  "permanent-delete-many",
  "manual-add-pending-import",
  "manual-add-many-pending-import",
  "dismiss-pending-import",
  "dismiss-many-pending-import",
  "export-app-logs",
]);

const activeSection = ref("root");
const secretKeyVisible = ref(false);
const { t, themeModeOptions, localeOptions, navAlignmentOptions } = useAppPreferences();
const repositoryUrl = "https://github.com/ZCH-czc/HybridPassword";
const appVersion = packageInfo.version || "0.0.0";

const isZh = computed(() => String(props.locale || "").toLowerCase().startsWith("zh"));
const aboutCopy = computed(() =>
  isZh.value
    ? {
        title: "关于",
        subtitle: "项目来源、技术栈与版本信息",
        heroTitle: "HybridPassword",
        heroBody: "一个面向 Windows、Android 与 Web 的本地优先密码管理器",
        versionTitle: "当前版本",
        sourceTitle: "Source Code",
        sourceBody: "GitHub 仓库地址",
        stackTitle: "技术栈",
        stackBody: "核心实现",
        openLink: "打开链接",
      }
    : {
        title: "About",
        subtitle: "Project source, stack, and version details",
        heroTitle: "HybridPassword",
        heroBody: "A local-first password manager for Windows, Android, and the web",
        versionTitle: "Current version",
        sourceTitle: "Source Code",
        sourceBody: "GitHub repository",
        stackTitle: "Tech stack",
        stackBody: "Core stack",
        openLink: "Open link",
      }
);

const aboutStack = computed(() =>
  isZh.value
    ? [
        { title: "Vue 3" },
        { title: ".NET MAUI Hybrid" },
      ]
    : [
        { title: "Vue 3" },
        { title: ".NET MAUI Hybrid" },
      ]
);

const passkeyNavCopy = computed(() =>
  isZh.value
    ? {
        title: "Passkeys",
        subtitle: "Windows 原生 passkey 元数据与能力状态",
        tagReady: "Windows",
        tagUnavailable: "未就绪",
      }
    : {
        title: "Passkeys",
        subtitle: "Windows-native passkey metadata and capability state",
        tagReady: "Windows",
        tagUnavailable: "Unavailable",
      }
);

const aboutSectionMeta = computed(() => ({
  "about-logs": {
    key: "about-logs",
    title: isZh.value ? "应用日志" : "Application logs",
    subtitle: isZh.value
      ? "查看应用运行、同步与错误日志，并支持导出"
      : "Review app events, sync activity, and errors, then export them",
    icon: "mdi-text-box-search-outline",
    tag: t("common.countItems", { count: props.appLogs.length }),
  },
}));

const languageChoiceOptions = computed(() => [
  {
    value: "zh-CN",
    title:
      localeOptions.value.find((item) => item.value === "zh-CN")?.title ||
      "简体中文",
    icon: "mdi-ideogram-cjk-variant",
  },
  {
    value: "en-US",
    title:
      localeOptions.value.find((item) => item.value === "en-US")?.title || "English",
    icon: "mdi-alphabetical-variant",
  },
]);

const themeChoiceOptions = computed(() => [
  {
    value: "system",
    title:
      themeModeOptions.value.find((item) => item.value === "system")?.title ||
      t("settings.appearance.theme.system"),
    icon: "mdi-theme-light-dark",
  },
  {
    value: "light",
    title:
      themeModeOptions.value.find((item) => item.value === "light")?.title ||
      t("settings.appearance.theme.light"),
    icon: "mdi-white-balance-sunny",
  },
  {
    value: "dark",
    title:
      themeModeOptions.value.find((item) => item.value === "dark")?.title ||
      t("settings.appearance.theme.dark"),
    icon: "mdi-weather-night",
  },
]);

const navAlignmentChoiceOptions = computed(() => [
  {
    value: "center",
    title:
      navAlignmentOptions.value.find((item) => item.value === "center")?.title ||
      t("settings.appearance.navAlignment.center"),
    icon: "mdi-dock-window",
  },
  {
    value: "left",
    title:
      navAlignmentOptions.value.find((item) => item.value === "left")?.title ||
      t("settings.appearance.navAlignment.left"),
    icon: "mdi-format-horizontal-align-left",
  },
  {
    value: "right",
    title:
      navAlignmentOptions.value.find((item) => item.value === "right")?.title ||
      t("settings.appearance.navAlignment.right"),
    icon: "mdi-format-horizontal-align-right",
  },
]);

const biometricReauthOptions = computed(() => [
  {
    value: 24,
    title: t("settings.biometricReauth.24h"),
    icon: "mdi-clock-time-eight-outline",
  },
  {
    value: 72,
    title: t("settings.biometricReauth.72h"),
    icon: "mdi-clock-outline",
  },
  {
    value: 168,
    title: t("settings.biometricReauth.1w"),
    icon: "mdi-calendar-week-outline",
  },
  {
    value: 720,
    title: t("settings.biometricReauth.1m"),
    icon: "mdi-calendar-month-outline",
  },
  {
    value: 0,
    title: t("settings.biometricReauth.never"),
    icon: "mdi-infinity",
  },
]);

const autoLockOptions = computed(() => [
  { value: 1, title: t("settings.autoLock.1m"), icon: "mdi-timer-outline" },
  { value: 5, title: t("settings.autoLock.5m"), icon: "mdi-timer-sand" },
  { value: 15, title: t("settings.autoLock.15m"), icon: "mdi-timer-cog-outline" },
  { value: 30, title: t("settings.autoLock.30m"), icon: "mdi-clock-outline" },
  { value: 60, title: t("settings.autoLock.1h"), icon: "mdi-clock-time-four-outline" },
  { value: 0, title: t("settings.autoLock.never"), icon: "mdi-infinity" },
]);

const biometricDescription = computed(() => {
  if (!props.biometricSupported) {
    return t("settings.biometricNotIntegrated");
  }

  if (!props.biometricAvailable) {
    return (
      props.biometricMessage ||
      t("settings.biometricUnavailable", { label: props.biometricLabel })
    );
  }

  if (props.biometricEnabled) {
    return t("settings.biometricEnabledBody", { label: props.biometricLabel });
  }

  return t("settings.biometricDisabledBody", { label: props.biometricLabel });
});

const biometricActionLabel = computed(() =>
  props.biometricEnabled ? t("settings.disableBiometric") : t("settings.enableBiometric")
);

const platformTitle = computed(() => {
  if (props.platform === "windows") {
    return "Windows";
  }

  if (props.platform === "android") {
    return "Android";
  }

  return t("settings.platformIntegration");
});

const resolvedThemeLabel = computed(() =>
  props.resolvedTheme === "dark"
    ? t("settings.appearance.resolved.dark")
    : t("settings.appearance.resolved.light")
);

const showSyncSection = computed(() => props.supportsWebDavSync || props.supportsLanSync);
const showPlatformSection = computed(
  () =>
    props.supportsMinimizeToTray ||
    props.supportsLaunchAtStartup ||
    props.supportsExcludeFromRecents ||
    props.supportsAutostartSettingsShortcut
);

const secretKeyToggleLabel = computed(() =>
  secretKeyVisible.value
    ? isZh.value
      ? "隐藏 Secret Key"
      : "Hide Secret Key"
    : t("settings.revealSecretKey")
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

const passkeySectionMeta = computed(() => ({
  passkeys: {
    key: "passkeys",
    title: "Passkeys",
    subtitle: isZh.value
      ? "查看 Windows 中已保存的 passkey 与账户信息"
      : "View saved Windows passkeys and account metadata",
    icon: "mdi-key-chain-variant",
    tag: props.passkeyState.isSupported ? "Windows" : isZh.value ? "未就绪" : "Unavailable",
  },
  "passkey-status": {
    key: "passkey-status",
    title: isZh.value ? "应用状态" : "App status",
    subtitle: isZh.value
      ? "日志、Companion 与能力诊断"
      : "Logs, companion state, and diagnostics",
    icon: "mdi-chart-box-outline",
    tag: "",
  },
}));

const rootItems = computed(() => {
  const items = [
    {
      key: "appearance",
      title: t("settings.appearance"),
      subtitle: t("settings.sectionAppearanceBody"),
      icon: "mdi-palette-outline",
      tag: resolvedThemeLabel.value,
    },
    {
      key: "security",
      title: t("settings.security"),
      subtitle: t("settings.sectionSecurityBody"),
      icon: "mdi-shield-lock-outline",
      tag: props.biometricEnabled ? t("common.enabled") : t("common.disabled"),
    },
  ];

  if (showSyncSection.value) {
    items.push({
      key: "sync",
      title: t("common.sync"),
      subtitle: t("settings.sectionSyncBody"),
      icon: "mdi-sync",
      tag: props.supportsWebDavSync && props.supportsLanSync ? "WebDAV · LAN" : "LAN / WebDAV",
    });
  }

  if (props.supportsPasskeys) {
    items.push(passkeySectionMeta.value.passkeys);
  }

  items.push({
    key: "data",
    title: t("settings.dataManagement"),
    subtitle: t("settings.sectionDataBody"),
    icon: "mdi-database-outline",
    tag: t("common.countItems", { count: props.recordCount }),
  });

  if (showPlatformSection.value) {
    items.push({
      key: "platform",
      title: platformTitle.value,
      subtitle: t("settings.sectionPlatformBody"),
      icon: props.platform === "windows" ? "mdi-microsoft-windows" : "mdi-cellphone-cog",
      tag: props.platform === "windows" ? "Windows" : props.platform === "android" ? "Android" : "",
    });
  }

  items.push({
    key: "about",
    title: aboutCopy.value.title,
    subtitle: aboutCopy.value.subtitle,
    icon: "mdi-information-outline",
    tag: `v${appVersion}`,
  });

  return items;
});

const currentSection = computed(
  () =>
    rootItems.value.find((item) => item.key === activeSection.value) ||
    passkeySectionMeta.value[activeSection.value] ||
    aboutSectionMeta.value[activeSection.value] || {
      key: "root",
      title: t("common.settings"),
      subtitle: t("settings.sectionListHint"),
      icon: "mdi-cog-outline",
      tag: "",
    }
);

function openSection(key) {
  secretKeyVisible.value = false;
  activeSection.value = key;
}

function goBack() {
  secretKeyVisible.value = false;
  if (activeSection.value === "passkey-status") {
    activeSection.value = "passkeys";
    return;
  }

  if (activeSection.value === "about-logs") {
    activeSection.value = "about";
    return;
  }

  activeSection.value = "root";
}

async function openRepositoryUrl() {
  await openExternalUrl(repositoryUrl);
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
</script>

<template>
  <div class="d-flex flex-column ga-4">
    <Transition name="settings-page" mode="out-in">
      <div :key="activeSection">
        <template v-if="activeSection === 'root'">
          <v-card class="border-sm settings-root-card">
            <v-card-text class="pa-6 pa-sm-7 d-flex align-center justify-space-between ga-4">
              <div>
                <div class="text-h4 font-weight-medium">{{ t("common.settings") }}</div>
                <div class="text-body-2 text-medium-emphasis mt-2">
                  {{ t("settings.sectionListHint") }}
                </div>
              </div>

              <v-btn color="primary" prepend-icon="mdi-lock-outline" @click="emit('lock')">
                {{ t("common.lock") }}
              </v-btn>
            </v-card-text>
          </v-card>

          <div class="settings-nav-stack mt-4" data-tour-target="settings-sections">
            <button
              v-for="item in rootItems"
              :key="item.key"
              type="button"
              class="settings-nav-card"
              @click="openSection(item.key)"
            >
              <div class="settings-nav-card__main">
                <v-avatar size="42" class="settings-nav-avatar">
                  <InlineSvgIcon :icon="item.icon" :size="21" />
                </v-avatar>

                <div class="min-w-0">
                  <div class="font-weight-medium text-subtitle-1">
                    {{ item.title }}
                  </div>
                  <div class="text-body-2 text-medium-emphasis mt-1 text-wrap">
                    {{ item.subtitle }}
                  </div>
                </div>
              </div>

              <div class="settings-nav-card__aside">
                <span v-if="item.tag" class="settings-nav-tag">
                  {{ item.tag }}
                </span>
                <InlineSvgIcon icon="mdi-chevron-right" :size="18" />
              </div>
            </button>
          </div>
        </template>

        <template v-else>
          <v-card class="border-sm settings-detail-card">
            <v-card-text class="pa-4 pa-sm-5 d-flex align-center justify-space-between ga-3">
              <div class="d-flex align-center ga-3 min-w-0">
                <v-btn icon variant="text" @click="goBack">
                  <InlineSvgIcon icon="mdi-chevron-left" :size="20" />
                </v-btn>
                <div class="min-w-0">
                  <div class="text-h5 font-weight-medium text-truncate">
                    {{ currentSection.title }}
                  </div>
                  <div class="text-body-2 text-medium-emphasis mt-1 text-wrap">
                    {{ currentSection.subtitle }}
                  </div>
                </div>
              </div>

              <v-btn variant="text" prepend-icon="mdi-lock-outline" @click="emit('lock')">
                {{ t("common.lock") }}
              </v-btn>
            </v-card-text>
          </v-card>

          <div v-if="activeSection === 'appearance'" class="d-flex flex-column ga-4 mt-4">
            <v-card class="border-sm">
              <v-card-title>{{ t("settings.appearance") }}</v-card-title>
              <v-card-text class="d-flex flex-column ga-4">
                <v-sheet class="pa-3 settings-choice-shell">
                  <div class="text-subtitle-2 font-weight-medium px-2 pt-1 settings-choice-label">
                    {{ t("settings.language") }}
                  </div>
                  <v-btn-toggle
                    :model-value="locale"
                    class="settings-toggle mt-3"
                    mandatory
                    selected-class="settings-toggle__active"
                    @update:model-value="emit('update-language', $event)"
                  >
                    <v-btn
                      v-for="item in languageChoiceOptions"
                      :key="item.value"
                      :value="item.value"
                      class="settings-toggle__item"
                      variant="text"
                    >
                      <v-icon :icon="item.icon" size="18" />
                      <span>{{ item.title }}</span>
                    </v-btn>
                  </v-btn-toggle>
                </v-sheet>

                <v-sheet class="pa-3 settings-choice-shell">
                  <div class="text-subtitle-2 font-weight-medium px-2 pt-1 settings-choice-label">
                    {{ t("settings.appearance.themeMode") }}
                  </div>
                  <v-btn-toggle
                    :model-value="themeMode"
                    class="settings-toggle mt-3"
                    mandatory
                    selected-class="settings-toggle__active"
                    @update:model-value="emit('update-theme-mode', $event)"
                  >
                    <v-btn
                      v-for="item in themeChoiceOptions"
                      :key="item.value"
                      :value="item.value"
                      class="settings-toggle__item"
                      variant="text"
                    >
                      <v-icon :icon="item.icon" size="18" />
                      <span>{{ item.title }}</span>
                    </v-btn>
                  </v-btn-toggle>
                </v-sheet>

                <v-sheet class="pa-3 settings-choice-shell">
                  <div class="text-subtitle-2 font-weight-medium px-2 pt-1 settings-choice-label">
                    {{ t("settings.appearance.navAlignment") }}
                  </div>
                  <div class="text-body-2 text-medium-emphasis px-2 mt-2">
                    {{ t("settings.appearance.navAlignmentBody") }}
                  </div>
                  <v-btn-toggle
                    :model-value="navAlignment"
                    class="settings-toggle mt-3"
                    mandatory
                    selected-class="settings-toggle__active"
                    @update:model-value="emit('update-nav-alignment', $event)"
                  >
                    <v-btn
                      v-for="item in navAlignmentChoiceOptions"
                      :key="item.value"
                      :value="item.value"
                      class="settings-toggle__item"
                      variant="text"
                    >
                      <v-icon :icon="item.icon" size="18" />
                      <span>{{ item.title }}</span>
                    </v-btn>
                  </v-btn-toggle>
                </v-sheet>

                <v-sheet class="px-4 py-3 bg-surface-variant text-body-2 text-medium-emphasis">
                  {{ t("settings.appearance.themeModeHint", { theme: resolvedThemeLabel }) }}
                </v-sheet>
              </v-card-text>
            </v-card>
          </div>

          <div v-else-if="activeSection === 'security'" class="d-flex flex-column ga-4 mt-4">
            <v-card class="border-sm">
              <v-card-title>{{ t("settings.security") }}</v-card-title>
              <v-card-text class="d-flex flex-column ga-4">
                <v-sheet class="pa-4 settings-block">
                  <div class="text-subtitle-1 font-weight-medium">{{ t("settings.changeMaster") }}</div>
                  <div class="text-body-2 text-medium-emphasis mt-2">
                    {{ t("settings.changeMasterBody") }}
                  </div>
                  <v-btn
                    class="mt-4"
                    color="primary"
                    prepend-icon="mdi-lock-reset"
                    :loading="changingMasterPassword"
                    @click="emit('change-master-password')"
                  >
                    {{ t("settings.changeMaster") }}
                  </v-btn>
                </v-sheet>

                <v-sheet class="pa-4 settings-block">
                  <div class="d-flex align-center justify-space-between flex-wrap ga-3">
                    <div>
                      <div class="text-subtitle-1 font-weight-medium">
                        {{ t("settings.biometricUnlock") }}
                      </div>
                      <div class="text-body-2 text-medium-emphasis mt-2">
                        {{ biometricDescription }}
                      </div>
                    </div>

                    <v-chip
                      :color="biometricEnabled ? 'secondary' : 'surface-variant'"
                      :variant="biometricEnabled ? 'tonal' : 'flat'"
                    >
                      {{ biometricEnabled ? t("common.enabled") : t("common.disabled") }}
                    </v-chip>
                  </div>

                  <v-btn
                    v-if="biometricSupported"
                    class="mt-4"
                    :color="biometricEnabled ? undefined : 'primary'"
                    :variant="biometricEnabled ? 'text' : 'elevated'"
                    prepend-icon="mdi-fingerprint"
                    :disabled="!biometricEnabled && !biometricAvailable"
                    :loading="biometricLoading"
                    @click="emit(biometricEnabled ? 'disable-biometric' : 'enable-biometric')"
                  >
                    {{ biometricActionLabel }}
                  </v-btn>

                  <div v-if="biometricSupported" class="mt-4">
                    <div class="text-subtitle-2 font-weight-medium">
                      {{ t("settings.biometricReauth.title") }}
                    </div>
                    <div class="text-body-2 text-medium-emphasis mt-2">
                      {{ t("settings.biometricReauth.body") }}
                    </div>

                    <v-btn-toggle
                      :model-value="biometricReauthHours"
                      class="settings-toggle mt-3"
                      mandatory
                      selected-class="settings-toggle__active"
                      :disabled="biometricLoading"
                      @update:model-value="emit('update-biometric-reauth-hours', $event)"
                    >
                      <v-btn
                        v-for="item in biometricReauthOptions"
                        :key="item.value"
                        :value="item.value"
                        class="settings-toggle__item"
                        variant="text"
                      >
                        <v-icon :icon="item.icon" size="18" />
                        <span>{{ item.title }}</span>
                      </v-btn>
                    </v-btn-toggle>
                  </div>
                </v-sheet>

                <v-sheet class="pa-4 settings-block">
                  <div class="text-subtitle-1 font-weight-medium">{{ t("settings.secretKey") }}</div>
                  <div class="text-body-2 text-medium-emphasis mt-2">
                    {{ t("settings.secretKeyBody") }}
                  </div>

                  <v-sheet class="px-4 py-3 bg-surface-variant text-body-2 mt-4">
                    <div class="font-weight-medium">{{ t("settings.secretKeyHintTitle") }}</div>
                    <div class="text-medium-emphasis mt-1">
                      {{ secretKeyDisplay }}
                    </div>
                  </v-sheet>

                  <div class="d-flex flex-wrap ga-2 mt-4">
                    <v-btn
                      color="primary"
                      prepend-icon="mdi-key-outline"
                      :loading="secretKeyLoading"
                      @click="handleToggleSecretKey"
                    >
                      {{ secretKeyToggleLabel }}
                    </v-btn>
                    <v-btn
                      variant="tonal"
                      prepend-icon="mdi-content-copy"
                      :disabled="!secretKeyValue"
                      @click="emit('copy-secret-key')"
                    >
                      {{ t("settings.copySecretKey") }}
                    </v-btn>
                  </div>
                </v-sheet>
              </v-card-text>
            </v-card>
          </div>

          <div v-else-if="activeSection === 'passkeys'" class="d-flex flex-column ga-4 mt-4">
            <PasskeyMetadataCard
              :locale="locale"
              :platform="platform"
              :supported="passkeyState.isSupported"
              :supports-metadata-management="passkeyState.supportsMetadataManagement"
              :has-platform-authenticator="passkeyState.hasPlatformAuthenticator"
              :companion-app-integrated="passkeyState.companionAppIntegrated"
              :items="passkeyItems"
              :deleted-items="deletedPasskeyItems"
              :refreshing="passkeyRefreshing"
              :busy-ids="passkeyBusyIds"
              @refresh="emit('refresh-passkeys')"
              @open-status="openSection('passkey-status')"
              @remove="emit('remove-passkey', $event)"
              @restore="emit('restore-passkey', $event)"
              @permanent-delete="emit('permanent-delete-passkey', $event)"
            />
          </div>

          <div v-else-if="activeSection === 'passkey-status'" class="d-flex flex-column ga-4 mt-4">
            <PasskeyAppStatusCard
              :locale="locale"
              :platform="platform"
              :supports-plugin-manager="passkeyState.supportsPluginManager"
              :requires-companion-app="passkeyState.requiresCompanionApp"
              :companion-app-integrated="passkeyState.companionAppIntegrated"
              :can-launch-companion-app="passkeyState.canLaunchCompanionApp"
              :supports-companion-auto-launch="passkeyState.supportsCompanionAutoLaunch"
              :companion-auto-launch-enabled="passkeyState.companionAutoLaunchEnabled"
            :launching-companion="passkeyLaunching"
            :auto-launch-saving="passkeyAutoLaunchSaving"
            :operation-resolving="passkeyOperationResolving"
            :companion-checked-at-unix-time-ms="passkeyState.companionCheckedAtUnixTimeMs"
              :companion-build-number="passkeyState.companionBuildNumber"
              :companion-ubr="passkeyState.companionUbr"
              :companion-meets-plugin-build-requirement="passkeyState.companionMeetsPluginBuildRequirement"
              :companion-web-authn-library-available="passkeyState.companionWebAuthnLibraryAvailable"
            :companion-plugin-exports-available="passkeyState.companionPluginExportsAvailable"
            :companion-is-packaged-process="passkeyState.companionIsPackagedProcess"
            :companion-status-summary="passkeyState.companionStatusSummary"
            :companion-detail-message="passkeyState.companionDetailMessage"
            :companion-workflow-mode="passkeyState.companionWorkflowMode"
            :companion-registration-attempted="passkeyState.companionRegistrationAttempted"
            :companion-registration-prepared="passkeyState.companionRegistrationPrepared"
            :companion-registration-environment-ready="passkeyState.companionRegistrationEnvironmentReady"
            :companion-registration-completed="passkeyState.companionRegistrationCompleted"
            :companion-last-registration-attempt-unix-time-ms="passkeyState.companionLastRegistrationAttemptUnixTimeMs"
            :companion-registration-status="passkeyState.companionRegistrationStatus"
            :companion-last-registration-message="passkeyState.companionLastRegistrationMessage"
            :companion-last-registration-h-result-hex="passkeyState.companionLastRegistrationHResultHex"
            :companion-authenticator-state-code="passkeyState.companionAuthenticatorStateCode"
            :companion-authenticator-state-label="passkeyState.companionAuthenticatorStateLabel"
            :companion-has-operation-signing-public-key="passkeyState.companionHasOperationSigningPublicKey"
            :companion-operation-signing-public-key-stored-at-unix-time-ms="passkeyState.companionOperationSigningPublicKeyStoredAtUnixTimeMs"
            :companion-com-skeleton-ready="passkeyState.companionComSkeletonReady"
            :companion-com-class-id-matches-manifest="passkeyState.companionComClassIdMatchesManifest"
            :companion-com-factory-ready="passkeyState.companionComFactoryReady"
            :companion-com-authenticator-ready="passkeyState.companionComAuthenticatorReady"
            :companion-com-last-probe-unix-time-ms="passkeyState.companionComLastProbeUnixTimeMs"
            :companion-com-last-probe-message="passkeyState.companionComLastProbeMessage"
            :companion-com-authenticator-type-name="passkeyState.companionComAuthenticatorTypeName"
            :companion-com-class-factory-registered="passkeyState.companionComClassFactoryRegistered"
            :companion-com-class-factory-registration-cookie="passkeyState.companionComClassFactoryRegistrationCookie"
            :companion-com-class-factory-last-registration-unix-time-ms="passkeyState.companionComClassFactoryLastRegistrationUnixTimeMs"
            :companion-com-class-factory-last-message="passkeyState.companionComClassFactoryLastMessage"
            :companion-com-class-factory-last-h-result-hex="passkeyState.companionComClassFactoryLastHResultHex"
            :companion-callback-total-count="passkeyState.companionCallbackTotalCount"
            :companion-callback-make-credential-count="passkeyState.companionCallbackMakeCredentialCount"
            :companion-callback-get-assertion-count="passkeyState.companionCallbackGetAssertionCount"
            :companion-callback-cancel-operation-count="passkeyState.companionCallbackCancelOperationCount"
            :companion-callback-get-lock-status-count="passkeyState.companionCallbackGetLockStatusCount"
            :companion-callback-last-unix-time-ms="passkeyState.companionCallbackLastUnixTimeMs"
            :companion-callback-last-kind="passkeyState.companionCallbackLastKind"
            :companion-callback-last-message="passkeyState.companionCallbackLastMessage"
            :companion-callback-last-h-result-hex="passkeyState.companionCallbackLastHResultHex"
            :companion-latest-operation-id="passkeyState.companionLatestOperationId"
            :companion-latest-operation-kind="passkeyState.companionLatestOperationKind"
            :companion-latest-operation-state="passkeyState.companionLatestOperationState"
            :companion-latest-operation-source="passkeyState.companionLatestOperationSource"
            :companion-latest-operation-created-at-unix-time-ms="passkeyState.companionLatestOperationCreatedAtUnixTimeMs"
            :companion-latest-operation-updated-at-unix-time-ms="passkeyState.companionLatestOperationUpdatedAtUnixTimeMs"
            :companion-latest-operation-request-pointer-present="passkeyState.companionLatestOperationRequestPointerPresent"
            :companion-latest-operation-response-pointer-present="passkeyState.companionLatestOperationResponsePointerPresent"
            :companion-latest-operation-cancel-pointer-present="passkeyState.companionLatestOperationCancelPointerPresent"
            :companion-latest-operation-message="passkeyState.companionLatestOperationMessage"
            :companion-latest-operation-h-result-hex="passkeyState.companionLatestOperationHResultHex"
            :companion-activation-count="passkeyState.companionActivationCount"
            :companion-last-activation-unix-time-ms="passkeyState.companionLastActivationUnixTimeMs"
            :companion-last-activation-source="passkeyState.companionLastActivationSource"
            :companion-started-from-plugin-activation="passkeyState.companionStartedFromPluginActivation"
            :companion-create-request-count="passkeyState.companionCreateRequestCount"
            :companion-last-create-request-unix-time-ms="passkeyState.companionLastCreateRequestUnixTimeMs"
            :companion-last-create-request-rp-id="passkeyState.companionLastCreateRequestRpId"
            :companion-last-create-request-username="passkeyState.companionLastCreateRequestUsername"
            :companion-last-create-request-message="passkeyState.companionLastCreateRequestMessage"
            :recent-logs="passkeyState.recentLogs"
            @launch-companion="emit('launch-passkey-companion')"
            @restart-companion="emit('restart-passkey-companion')"
            @toggle-auto-launch="emit('toggle-passkey-companion-auto-launch', $event)"
            @approve-operation="emit('resolve-passkey-operation', 'approve')"
            @reject-operation="emit('resolve-passkey-operation', 'reject')"
            @clear-operation="emit('resolve-passkey-operation', 'clear')"
          />
          </div>

          <div v-else-if="activeSection === 'sync'" class="d-flex flex-column ga-4 mt-4">
            <WebDavSyncCard
              v-if="supportsWebDavSync"
              :settings="syncSettings.webDav"
              :disabled="busy"
              :saving="webDavSaving"
              :transferring="webDavTransfering"
              @save="emit('save-webdav-settings', $event)"
              @upload="emit('upload-webdav')"
              @download="emit('download-webdav')"
            />

            <LanSyncCard
              v-if="supportsLanSync"
              :device-name="syncSettings.deviceName"
              :devices="lanDevices"
              :disabled="busy"
              :scanning="lanScanning"
              :saving-device-name="lanSavingDeviceName"
              @save-device-name="emit('save-device-name', $event)"
              @scan="emit('scan-lan')"
              @sync-device="emit('sync-lan-device', $event)"
            />
          </div>

          <div v-else-if="activeSection === 'data'" class="d-flex flex-column ga-4 mt-4">
            <v-card class="border-sm">
              <v-card-title>{{ t("settings.clearAllData") }}</v-card-title>
              <v-card-text>
                <v-sheet class="pa-4 settings-block">
                  <div class="text-subtitle-1 font-weight-medium">
                    {{ t("settings.clearAllData") }}
                  </div>
                  <div class="text-body-2 text-medium-emphasis mt-2">
                    {{ t("settings.clearAllDataBody") }}
                  </div>

                  <v-btn
                    class="mt-4"
                    color="error"
                    prepend-icon="mdi-trash-can-outline"
                    :loading="clearingData"
                    @click="emit('clear-all-data')"
                  >
                    {{ t("settings.clearAllDataAction") }}
                  </v-btn>
                </v-sheet>
              </v-card-text>
            </v-card>

            <DeletedList
              :items="deletedItems"
              :busy-ids="deletedBusyIds"
              :batch-loading="deletedBatchLoading"
              @restore="emit('restore', $event)"
              @permanent-delete="emit('permanent-delete', $event)"
              @restore-many="emit('restore-many', $event)"
              @permanent-delete-many="emit('permanent-delete-many', $event)"
            />

            <ImportExportCard
              :import-strategy="importStrategy"
              :busy="busy"
              :native-file-dialogs-available="nativeFileDialogsAvailable"
              @update:import-strategy="emit('update:importStrategy', $event)"
              @export="emit('export', $event)"
              @import="emit('import', $event)"
            />

            <PendingImportReviewCard
              :items="pendingImportReviewItems"
              :updated-at="pendingImportReviewUpdatedAt"
              :busy="pendingImportReviewBusy"
              @manual-add="emit('manual-add-pending-import', $event)"
              @manual-add-many="emit('manual-add-many-pending-import', $event)"
              @dismiss="emit('dismiss-pending-import', $event)"
              @dismiss-many="emit('dismiss-many-pending-import', $event)"
            />
          </div>

          <div v-else-if="activeSection === 'platform'" class="d-flex flex-column ga-4 mt-4">
            <v-card class="border-sm">
              <v-card-title>{{ platformTitle }}</v-card-title>
              <v-card-text class="d-flex flex-column ga-4">
                <v-sheet v-if="supportsMinimizeToTray" class="pa-4 settings-block">
                  <div class="d-flex align-center justify-space-between ga-4">
                    <div>
                      <div class="text-subtitle-1 font-weight-medium">
                        {{ t("settings.windowsTray") }}
                      </div>
                      <div class="text-body-2 text-medium-emphasis mt-2">
                        {{ t("settings.windowsTrayBody") }}
                      </div>
                    </div>

                    <v-switch
                      :model-value="minimizeToTrayEnabled"
                      color="primary"
                      hide-details
                      inset
                      :loading="platformSettingsLoading"
                      @update:model-value="emit('toggle-minimize-to-tray', $event)"
                    />
                  </div>
                </v-sheet>

                <v-sheet v-if="supportsMinimizeToTray" class="pa-4 settings-block">
                  <div class="text-subtitle-1 font-weight-medium">
                    {{ t("settings.trayAutoLock") }}
                  </div>
                  <div class="text-body-2 text-medium-emphasis mt-2">
                    {{ t("settings.trayAutoLockBody") }}
                  </div>

                  <v-btn-toggle
                    :model-value="trayAutoLockMinutes"
                    class="settings-toggle mt-4"
                    mandatory
                    selected-class="settings-toggle__active"
                    :disabled="platformSettingsLoading"
                    @update:model-value="emit('update-tray-auto-lock-minutes', $event)"
                  >
                    <v-btn
                      v-for="item in autoLockOptions"
                      :key="`tray-${item.value}`"
                      :value="item.value"
                      class="settings-toggle__item"
                      variant="text"
                    >
                      <v-icon :icon="item.icon" size="18" />
                      <span>{{ item.title }}</span>
                    </v-btn>
                  </v-btn-toggle>
                </v-sheet>

                <v-sheet v-if="supportsLaunchAtStartup" class="pa-4 settings-block">
                  <div class="d-flex align-center justify-space-between ga-4">
                    <div>
                      <div class="text-subtitle-1 font-weight-medium">
                        {{ t("settings.launchAtStartup") }}
                      </div>
                      <div class="text-body-2 text-medium-emphasis mt-2">
                        {{ t("settings.launchAtStartupBody") }}
                      </div>
                    </div>

                    <v-switch
                      :model-value="launchAtStartupEnabled"
                      color="primary"
                      hide-details
                      inset
                      :loading="platformSettingsLoading"
                      @update:model-value="emit('toggle-launch-at-startup', $event)"
                    />
                  </div>
                </v-sheet>

                <v-sheet v-if="supportsExcludeFromRecents" class="pa-4 settings-block">
                  <div class="d-flex align-center justify-space-between ga-4">
                    <div>
                      <div class="text-subtitle-1 font-weight-medium">
                        {{ t("settings.excludeFromRecents") }}
                      </div>
                      <div class="text-body-2 text-medium-emphasis mt-2">
                        {{ t("settings.excludeFromRecentsBody") }}
                      </div>
                    </div>

                    <v-switch
                      :model-value="excludeFromRecentsEnabled"
                      color="primary"
                      hide-details
                      inset
                      :loading="platformSettingsLoading"
                      @update:model-value="emit('toggle-exclude-from-recents', $event)"
                    />
                  </div>
                </v-sheet>

                <v-sheet v-if="supportsExcludeFromRecents" class="pa-4 settings-block">
                  <div class="text-subtitle-1 font-weight-medium">
                    {{ t("settings.backgroundAutoLock") }}
                  </div>
                  <div class="text-body-2 text-medium-emphasis mt-2">
                    {{ t("settings.backgroundAutoLockBody") }}
                  </div>

                  <v-btn-toggle
                    :model-value="backgroundAutoLockMinutes"
                    class="settings-toggle mt-4"
                    mandatory
                    selected-class="settings-toggle__active"
                    :disabled="platformSettingsLoading"
                    @update:model-value="emit('update-background-auto-lock-minutes', $event)"
                  >
                    <v-btn
                      v-for="item in autoLockOptions"
                      :key="`background-${item.value}`"
                      :value="item.value"
                      class="settings-toggle__item"
                      variant="text"
                    >
                      <v-icon :icon="item.icon" size="18" />
                      <span>{{ item.title }}</span>
                    </v-btn>
                  </v-btn-toggle>
                </v-sheet>

                <v-sheet v-if="supportsAutostartSettingsShortcut" class="pa-4 settings-block">
                  <div
                    class="d-flex flex-column flex-md-row align-md-center justify-space-between ga-4"
                  >
                    <div>
                      <div class="text-subtitle-1 font-weight-medium">
                        {{ t("settings.autostartShortcut") }}
                      </div>
                      <div class="text-body-2 text-medium-emphasis mt-2">
                        {{ t("settings.autostartShortcutBody") }}
                      </div>
                    </div>

                    <v-btn
                      color="primary"
                      prepend-icon="mdi-open-in-new"
                      :loading="autostartOpening"
                      @click="emit('open-autostart-settings')"
                    >
                      {{ t("settings.openSystemSettings") }}
                    </v-btn>
                  </div>
                </v-sheet>
              </v-card-text>
            </v-card>
          </div>

          <div v-else-if="activeSection === 'about'" class="d-flex flex-column ga-4 mt-4">
            <v-card class="border-sm about-card">
              <v-card-text class="pa-6 pa-sm-8">
                <div class="about-hero">
                  <v-avatar size="96" class="about-hero__avatar">
                    <InlineSvgIcon icon="mdi-shield-lock-outline" :size="48" />
                  </v-avatar>

                  <div class="text-h4 font-weight-bold mt-5">
                    {{ aboutCopy.heroTitle }}
                  </div>
                  <div class="text-body-1 text-medium-emphasis mt-2 about-hero__body">
                    {{ aboutCopy.heroBody }}
                  </div>
                </div>

                <div class="d-flex flex-column ga-3 mt-8">
                  <div class="about-panel">
                    <div class="about-panel__row">
                      <div class="about-panel__head">
                        <InlineSvgIcon icon="mdi-information-outline" :size="18" />
                        <span>{{ aboutCopy.versionTitle }}</span>
                      </div>
                      <span class="about-panel__value">v{{ appVersion }}</span>
                    </div>
                  </div>

                  <div class="about-panel">
                    <div class="about-panel__row about-panel__row--start">
                      <div>
                        <div class="about-panel__title">{{ aboutCopy.sourceTitle }}</div>
                        <div class="text-body-2 text-medium-emphasis mt-1">
                          {{ aboutCopy.sourceBody }}
                        </div>
                      </div>

                      <v-btn variant="text" class="about-panel__action" @click="openRepositoryUrl">
                        <template #prepend>
                          <InlineSvgIcon icon="mdi-open-in-new" :size="18" />
                        </template>
                        {{ aboutCopy.openLink }}
                      </v-btn>
                    </div>

                    <a
                      class="about-link mt-3"
                      :href="repositoryUrl"
                      target="_blank"
                      rel="noreferrer"
                    >
                      <InlineSvgIcon icon="mdi-github" :size="18" />
                      <span>{{ repositoryUrl }}</span>
                    </a>
                  </div>

                  <div class="about-panel">
                    <div class="about-panel__title">{{ aboutCopy.stackTitle }}</div>
                    <div class="text-body-2 text-medium-emphasis mt-1">
                      {{ aboutCopy.stackBody }}
                    </div>

                    <div class="d-flex flex-column ga-3 mt-4">
                      <div v-for="item in aboutStack" :key="item.title" class="about-stack-item">
                        <div class="about-stack-item__title">
                          <InlineSvgIcon icon="mdi-code-tags" :size="18" />
                          <span>{{ item.title }}</span>
                        </div>
                      </div>
                    </div>
                  </div>

                  <button
                    type="button"
                    class="settings-nav-card about-nav-card"
                    @click="openSection('about-logs')"
                  >
                    <div class="settings-nav-card__main">
                      <v-avatar size="44" class="settings-nav-avatar">
                        <InlineSvgIcon icon="mdi-text-box-search-outline" :size="22" />
                      </v-avatar>

                      <div class="min-w-0">
                        <div class="text-subtitle-1 font-weight-medium">
                          {{ aboutSectionMeta["about-logs"].title }}
                        </div>
                        <div class="text-body-2 text-medium-emphasis mt-1">
                          {{ aboutSectionMeta["about-logs"].subtitle }}
                        </div>
                      </div>
                    </div>

                    <div class="settings-nav-card__aside">
                      <span class="settings-nav-tag">{{ aboutSectionMeta["about-logs"].tag }}</span>
                      <InlineSvgIcon icon="mdi-chevron-right" :size="20" />
                    </div>
                  </button>
                </div>
              </v-card-text>
            </v-card>
          </div>

          <div v-else-if="activeSection === 'about-logs'" class="d-flex flex-column ga-4 mt-4">
            <AppLogStatusCard
              :locale="locale"
              :platform="platform"
              :app-version="appVersion"
              :logs="appLogs"
              :updated-at="appLogsUpdatedAt"
              :exporting="appLogExporting"
              @export="emit('export-app-logs')"
            />
          </div>
        </template>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.settings-root-card,
.settings-detail-card {
  background: var(--vault-panel-bg);
}

.settings-nav-item {
  border-radius: var(--vault-radius) !important;
  cursor: pointer;
  background: transparent !important;
  box-shadow: none !important;
  transition:
    background-color 220ms ease,
    transform 220ms ease,
    box-shadow 220ms ease;
}

.settings-nav-item:hover {
  background: rgba(var(--v-theme-surface-variant), 0.42) !important;
  transform: translateY(-1px);
}

.settings-nav-stack {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.settings-nav-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 18px;
  width: 100%;
  padding: 18px 20px;
  border: none;
  border-radius: calc(var(--vault-radius) + 2px);
  background: rgba(var(--v-theme-surface), 0.48);
  box-shadow: none;
  text-align: left;
  color: inherit;
  cursor: pointer;
  transition:
    transform 220ms ease,
    background-color 220ms ease,
    box-shadow 220ms ease;
}

.settings-nav-card:hover {
  transform: translateY(-1px);
  background: rgba(var(--v-theme-surface), 0.6);
}

.settings-nav-card:focus-visible {
  outline: none;
  background: rgba(var(--v-theme-surface), 0.62);
}

.settings-nav-card__main,
.settings-nav-card__aside {
  display: flex;
  align-items: center;
  gap: 14px;
}

.settings-nav-card__main {
  min-width: 0;
  flex: 1 1 auto;
}

.settings-nav-card__aside {
  color: rgba(var(--v-theme-on-surface), 0.64);
}

.settings-nav-avatar {
  background: rgba(var(--v-theme-on-surface), 0.05);
  color: rgba(var(--v-theme-on-surface), 0.74);
}

.settings-nav-tag {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 28px;
  padding: 0 12px;
  border-radius: 999px;
  background: rgba(var(--v-theme-on-surface), 0.05);
  color: rgba(var(--v-theme-on-surface), 0.76);
  font-size: 0.82rem;
  font-weight: 600;
}

.settings-nav-chip {
  background: rgba(var(--v-theme-surface-variant), 0.9);
}

.settings-block {
  background: var(--vault-block-bg);
}

.about-card {
  background: var(--vault-panel-bg);
}

.about-hero {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
}

.about-hero__avatar {
  background:
    radial-gradient(circle at 32% 28%, rgba(var(--v-theme-secondary), 0.22), transparent 56%),
    rgba(var(--v-theme-on-surface), 0.06);
  color: rgba(var(--v-theme-on-surface), 0.92);
}

.about-hero__body {
  max-width: 520px;
}

.about-panel {
  padding: 18px 20px;
  border-radius: calc(var(--vault-radius) + 4px);
  background: var(--vault-block-bg);
}

.about-panel__row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.about-panel__row--start {
  align-items: flex-start;
}

.about-panel__head,
.about-stack-item__title {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  font-weight: 600;
}

.about-panel__title {
  font-size: 1rem;
  font-weight: 700;
}

.about-panel__value {
  color: rgba(var(--v-theme-on-surface), 0.74);
  font-weight: 600;
}

.about-panel__action {
  align-self: center;
}

.about-link {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  color: inherit;
  text-decoration: none;
  word-break: break-all;
}

.about-link:hover {
  opacity: 0.88;
}

.about-stack-item {
  padding: 14px 16px;
  border-radius: calc(var(--vault-radius) - 2px);
  background: var(--vault-block-bg-subtle);
}

.settings-choice-shell {
  background: var(--vault-block-bg);
}

.settings-choice-label {
  color: rgba(var(--v-theme-on-surface), 0.74);
}

.settings-toggle {
  display: grid !important;
  grid-template-columns: repeat(auto-fit, minmax(132px, 1fr));
  gap: 10px;
  width: 100%;
  height: auto !important;
  overflow: visible !important;
  align-items: stretch;
  background: transparent !important;
}

.settings-toggle :deep(.v-btn) {
  min-height: 64px;
  height: auto !important;
  justify-content: flex-start;
  gap: 10px;
  padding-inline: 16px;
  color: rgb(var(--v-theme-on-surface));
  background: var(--vault-block-bg-subtle);
  box-shadow: none;
}

.settings-toggle :deep(.settings-toggle__active) {
  background: rgba(var(--v-theme-primary), 0.12);
  color: rgb(var(--v-theme-primary));
  box-shadow: none;
}

.settings-toggle :deep(.v-btn__content) {
  justify-content: flex-start;
  width: 100%;
  font-weight: 500;
  white-space: normal;
  line-height: 1.3;
  text-align: left;
}

.settings-page-enter-active,
.settings-page-leave-active {
  transition:
    opacity 240ms cubic-bezier(0.2, 0.7, 0.2, 1),
    transform 240ms cubic-bezier(0.2, 0.7, 0.2, 1);
}

.settings-page-enter-from,
.settings-page-leave-to {
  opacity: 0;
  transform: translateY(10px);
}

.min-w-0 {
  min-width: 0;
}

:deep(.settings-nav-item .v-list-item__overlay),
:deep(.settings-nav-item .v-ripple__container) {
  display: none !important;
}

:global(.v-theme--dark) .settings-nav-avatar {
  background: rgba(255, 255, 255, 0.04);
  color: rgba(var(--v-theme-on-surface), 0.8);
}

:global(.v-theme--dark) .settings-nav-card {
  background: rgba(var(--v-theme-surface), 0.34);
}

:global(.v-theme--dark) .settings-nav-card:hover,
:global(.v-theme--dark) .settings-nav-card:focus-visible {
  background: rgba(var(--v-theme-surface), 0.46);
}

:global(.v-theme--dark) .settings-nav-tag {
  background: rgba(255, 255, 255, 0.04);
  color: rgba(var(--v-theme-on-surface), 0.82);
}

:global(.v-theme--dark) .about-hero__avatar {
  background:
    radial-gradient(circle at 32% 28%, rgba(var(--v-theme-secondary), 0.18), transparent 56%),
    rgba(255, 255, 255, 0.04);
}

@media (max-width: 640px) {
  .about-panel {
    padding: 16px 16px;
  }

  .about-panel__row {
    flex-direction: column;
    align-items: flex-start;
  }

  .about-panel__action {
    align-self: stretch;
  }
}
</style>
