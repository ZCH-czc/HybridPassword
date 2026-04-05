<script setup>
import { computed } from "vue";
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
  supportsExcludeFromRecents: {
    type: Boolean,
    default: false,
  },
  excludeFromRecentsEnabled: {
    type: Boolean,
    default: false,
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
  "update-language",
  "change-master-password",
  "enable-biometric",
  "disable-biometric",
  "toggle-minimize-to-tray",
  "toggle-launch-at-startup",
  "toggle-exclude-from-recents",
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

const { t, themeModeOptions, localeOptions } = useAppPreferences();

const languageChoiceOptions = computed(() => [
  {
    value: "zh-CN",
    title: localeOptions.value.find((item) => item.value === "zh-CN")?.title || "简体中文",
    icon: "mdi-ideogram-cjk-variant",
  },
  {
    value: "en-US",
    title: localeOptions.value.find((item) => item.value === "en-US")?.title || "English",
    icon: "mdi-alphabetical-variant",
  },
]);

const themeChoiceOptions = computed(() => [
  {
    value: "system",
    title: themeModeOptions.value.find((item) => item.value === "system")?.title || t("settings.appearance.theme.system"),
    icon: "mdi-theme-light-dark",
  },
  {
    value: "light",
    title: themeModeOptions.value.find((item) => item.value === "light")?.title || t("settings.appearance.theme.light"),
    icon: "mdi-white-balance-sunny",
  },
  {
    value: "dark",
    title: themeModeOptions.value.find((item) => item.value === "dark")?.title || t("settings.appearance.theme.dark"),
    icon: "mdi-weather-night",
  },
]);

const biometricDescription = computed(() => {
  if (!props.biometricSupported) {
    return t("settings.biometricNotIntegrated");
  }

  if (!props.biometricAvailable) {
    return props.biometricMessage || t("settings.biometricUnavailable", { label: props.biometricLabel });
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

  return "Host";
});

const resolvedThemeLabel = computed(() =>
  props.resolvedTheme === "dark"
    ? t("settings.appearance.resolved.dark")
    : t("settings.appearance.resolved.light")
);
</script>

<template>
  <div class="d-flex flex-column ga-4">
    <v-card class="settings-header border-sm overflow-hidden">
      <v-card-text class="pa-6 pa-sm-7 d-flex flex-column flex-lg-row align-lg-center justify-space-between ga-4">
        <div>
          <div class="text-h4 font-weight-medium">{{ t("common.settings") }}</div>
          <div class="text-body-2 text-medium-emphasis mt-2">
            {{ t("settings.headerDescription", { count: recordCount, deletedCount: deletedItems.length }) }}
          </div>
        </div>

        <div class="d-flex flex-wrap ga-2">
          <v-chip color="primary" variant="tonal">
            {{ localeOptions.find((item) => item.value === locale)?.title || locale }}
          </v-chip>
          <v-chip color="secondary" variant="tonal">
            {{ resolvedThemeLabel }}
          </v-chip>
          <v-chip
            :color="biometricEnabled ? 'primary' : 'surface-variant'"
            :variant="biometricEnabled ? 'tonal' : 'flat'"
          >
            {{ biometricEnabled ? t("common.enabled") : t("common.disabled") }}
          </v-chip>
          <v-btn color="primary" prepend-icon="mdi-lock-outline" @click="emit('lock')">
            {{ t("common.lock") }}
          </v-btn>
        </div>
      </v-card-text>
    </v-card>

    <v-row dense>
      <v-col cols="12" lg="6">
        <v-card class="border-sm h-100">
          <v-card-title>{{ t("settings.appearance") }}</v-card-title>
          <v-card-text class="d-flex flex-column ga-4">
            <v-sheet class="rounded-xl pa-3 settings-choice-shell">
              <div class="text-subtitle-2 font-weight-medium px-2 pt-1 settings-choice-label">
                {{ t("settings.language") }}
              </div>
              <v-btn-toggle
                :model-value="locale"
                class="settings-toggle mt-3"
                mandatory
                rounded="xl"
                selected-class="settings-toggle__active"
                @update:model-value="emit('update-language', $event)"
              >
                <v-btn
                  v-for="item in languageChoiceOptions"
                  :key="item.value"
                  :value="item.value"
                  class="settings-toggle__item"
                  rounded="xl"
                  variant="text"
                >
                  <v-icon :icon="item.icon" size="18" />
                  <span>{{ item.title }}</span>
                </v-btn>
              </v-btn-toggle>
            </v-sheet>

            <v-sheet class="rounded-xl pa-3 settings-choice-shell">
              <div class="text-subtitle-2 font-weight-medium px-2 pt-1 settings-choice-label">
                {{ t("settings.appearance.themeMode") }}
              </div>
              <v-btn-toggle
                :model-value="themeMode"
                class="settings-toggle mt-3"
                mandatory
                rounded="xl"
                selected-class="settings-toggle__active"
                @update:model-value="emit('update-theme-mode', $event)"
              >
                <v-btn
                  v-for="item in themeChoiceOptions"
                  :key="item.value"
                  :value="item.value"
                  class="settings-toggle__item"
                  rounded="xl"
                  variant="text"
                >
                  <v-icon :icon="item.icon" size="18" />
                  <span>{{ item.title }}</span>
                </v-btn>
              </v-btn-toggle>
            </v-sheet>

            <v-sheet class="rounded-xl px-4 py-3 bg-surface-variant text-body-2 text-medium-emphasis">
              {{ t("settings.appearance.themeModeHint", { theme: resolvedThemeLabel }) }}
            </v-sheet>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" lg="6">
        <v-card class="border-sm h-100">
          <v-card-title>{{ t("settings.security") }}</v-card-title>
          <v-card-text class="d-flex flex-column ga-4">
            <v-sheet class="rounded-xl pa-4 settings-block">
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

            <v-sheet class="rounded-xl pa-4 settings-block">
              <div class="d-flex align-center justify-space-between flex-wrap ga-3">
                <div>
                  <div class="text-subtitle-1 font-weight-medium">{{ t("settings.biometricUnlock") }}</div>
                  <div class="text-body-2 text-medium-emphasis mt-2">
                    {{ biometricDescription }}
                  </div>
                </div>

                <v-chip
                  :color="biometricEnabled ? 'primary' : 'surface-variant'"
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
            </v-sheet>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-card
      v-if="supportsMinimizeToTray || supportsLaunchAtStartup || supportsExcludeFromRecents || supportsAutostartSettingsShortcut"
      class="border-sm"
    >
      <v-card-title>{{ platformTitle }}</v-card-title>
      <v-card-text class="d-flex flex-column ga-4">
        <v-sheet v-if="supportsMinimizeToTray" class="rounded-xl pa-4 settings-block">
          <div class="d-flex align-center justify-space-between ga-4">
            <div>
              <div class="text-subtitle-1 font-weight-medium">{{ t("settings.windowsTray") }}</div>
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

        <v-sheet v-if="supportsLaunchAtStartup" class="rounded-xl pa-4 settings-block">
          <div class="d-flex align-center justify-space-between ga-4">
            <div>
              <div class="text-subtitle-1 font-weight-medium">{{ t("settings.launchAtStartup") }}</div>
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

        <v-sheet v-if="supportsExcludeFromRecents" class="rounded-xl pa-4 settings-block">
          <div class="d-flex align-center justify-space-between ga-4">
            <div>
              <div class="text-subtitle-1 font-weight-medium">{{ t("settings.excludeFromRecents") }}</div>
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

        <v-sheet v-if="supportsAutostartSettingsShortcut" class="rounded-xl pa-4 settings-block">
          <div class="d-flex flex-column flex-md-row align-md-center justify-space-between ga-4">
            <div>
              <div class="text-subtitle-1 font-weight-medium">{{ t("settings.autostartShortcut") }}</div>
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
</template>

<style scoped>
.settings-header {
  background:
    radial-gradient(circle at top right, rgba(var(--v-theme-primary), 0.12), transparent 30%),
    linear-gradient(
      135deg,
      rgba(var(--v-theme-surface), 0.98),
      rgba(var(--v-theme-surface), 0.88)
    );
}

.settings-block {
  background: rgba(var(--v-theme-surface), 0.62);
}

.settings-choice-shell {
  background: rgba(var(--v-theme-surface), 0.58);
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
  text-transform: none;
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
</style>
