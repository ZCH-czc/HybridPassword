<script setup>
import { computed, ref } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";
import DeletedList from "@/components/DeletedList.vue";
import ImportExportCard from "@/components/ImportExportCard.vue";
import LanSyncCard from "@/components/LanSyncCard.vue";
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
  "restore",
  "permanent-delete",
]);

const activeSection = ref("root");
const { t, themeModeOptions, localeOptions, navAlignmentOptions } = useAppPreferences();

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

  return items;
});

const currentSection = computed(
  () =>
    rootItems.value.find((item) => item.key === activeSection.value) || {
      key: "root",
      title: t("common.settings"),
      subtitle: t("settings.sectionListHint"),
      icon: "mdi-cog-outline",
      tag: "",
    }
);

function openSection(key) {
  activeSection.value = key;
}

function goBack() {
  activeSection.value = "root";
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

          <v-card class="border-sm mt-4">
            <v-list class="bg-transparent py-2 px-2" lines="two">
              <v-list-item
                v-for="item in rootItems"
                :key="item.key"
                class="settings-nav-item my-1"
                rounded="xl"
                @click="openSection(item.key)"
              >
                <template #prepend>
                  <v-avatar size="42" color="surface-variant" variant="flat">
                    <v-icon>{{ item.icon }}</v-icon>
                  </v-avatar>
                </template>

                <v-list-item-title class="font-weight-medium">
                  {{ item.title }}
                </v-list-item-title>
                <v-list-item-subtitle class="text-wrap mt-1">
                  {{ item.subtitle }}
                </v-list-item-subtitle>

                <template #append>
                  <div class="d-flex align-center ga-2">
                    <v-chip v-if="item.tag" size="small" variant="flat" class="settings-nav-chip">
                      {{ item.tag }}
                    </v-chip>
                    <v-icon size="20">mdi-chevron-right</v-icon>
                  </div>
                </template>
              </v-list-item>
            </v-list>
          </v-card>
        </template>

        <template v-else>
          <v-card class="border-sm settings-detail-card">
            <v-card-text class="pa-4 pa-sm-5 d-flex align-center justify-space-between ga-3">
              <div class="d-flex align-center ga-3 min-w-0">
                <v-btn icon variant="text" @click="goBack">
                  <v-icon>mdi-chevron-left</v-icon>
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
                      {{ secretKeyValue || secretKeyHint || t("common.none") }}
                    </div>
                  </v-sheet>

                  <div class="d-flex flex-wrap ga-2 mt-4">
                    <v-btn
                      color="primary"
                      prepend-icon="mdi-key-outline"
                      :loading="secretKeyLoading"
                      @click="emit('reveal-secret-key')"
                    >
                      {{ t("settings.revealSecretKey") }}
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
            <DeletedList
              :items="deletedItems"
              :busy-ids="deletedBusyIds"
              @restore="emit('restore', $event)"
              @permanent-delete="emit('permanent-delete', $event)"
            />

            <ImportExportCard
              :import-strategy="importStrategy"
              :busy="busy"
              :native-file-dialogs-available="nativeFileDialogsAvailable"
              @update:import-strategy="emit('update:importStrategy', $event)"
              @export="emit('export', $event)"
              @import="emit('import', $event)"
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
        </template>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.settings-root-card,
.settings-detail-card {
  background:
    linear-gradient(
      180deg,
      rgba(var(--v-theme-surface), 0.92),
      rgba(var(--v-theme-surface), 0.78)
    ),
    radial-gradient(circle at top right, rgba(var(--v-theme-primary), 0.08), transparent 34%);
}

.settings-nav-item {
  border-radius: var(--vault-radius) !important;
  cursor: pointer;
  transition:
    background-color 220ms ease,
    transform 220ms ease,
    box-shadow 220ms ease;
}

.settings-nav-item:hover {
  background: rgba(var(--v-theme-surface-variant), 0.82);
  transform: translateY(-1px);
}

.settings-nav-chip {
  background: rgba(var(--v-theme-surface-variant), 0.9);
}

.settings-block {
  background: rgba(var(--v-theme-surface), 0.62);
}

.settings-choice-shell {
  background: rgba(var(--v-theme-surface), 0.6);
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
  background: rgba(var(--v-theme-surface), 0.76);
  box-shadow: none;
}

.settings-toggle :deep(.settings-toggle__active) {
  background: rgba(var(--v-theme-primary), 0.14);
  color: rgb(var(--v-theme-primary));
  box-shadow: inset 0 0 0 1px rgba(var(--v-theme-primary), 0.18);
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
</style>
