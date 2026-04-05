<script setup>
import { computed } from "vue";
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
    default: "light",
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
    default: "生物识别",
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
  "toggle-theme",
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

const biometricDescription = computed(() => {
  if (!props.biometricSupported) {
    return "当前宿主尚未接入生物识别。";
  }

  if (!props.biometricAvailable) {
    return props.biometricMessage || `当前设备暂时无法使用${props.biometricLabel}。`;
  }

  if (props.biometricEnabled) {
    return `已启用${props.biometricLabel}，下次可以直接验证解锁。`;
  }

  return `启用后可直接使用${props.biometricLabel}解锁。`;
});

const biometricActionLabel = computed(() =>
  props.biometricEnabled ? "关闭生物识别" : "启用生物识别"
);

const platformTitle = computed(() => {
  if (props.platform === "windows") {
    return "Windows";
  }

  if (props.platform === "android") {
    return "Android";
  }

  return "宿主平台";
});
</script>

<template>
  <div class="d-flex flex-column ga-4">
    <v-card class="settings-header border-sm overflow-hidden">
      <v-card-text class="pa-6 pa-sm-7 d-flex flex-column flex-lg-row align-lg-center justify-space-between ga-4">
        <div>
          <div class="text-h4 font-weight-medium">设置</div>
          <div class="text-body-2 text-medium-emphasis mt-2">
            已保存 {{ recordCount }}条，最近删除 {{ deletedItems.length }}条
          </div>
        </div>

        <div class="d-flex flex-wrap ga-2">
          <v-chip color="primary" variant="tonal">
            {{ themeMode === "dark" ? "暗黑模式" : "浅色模式" }}
          </v-chip>
          <v-chip
            :color="biometricEnabled ? 'primary' : 'surface-variant'"
            :variant="biometricEnabled ? 'tonal' : 'flat'"
          >
            {{ biometricEnabled ? "生物识别已启用" : "生物识别未启用" }}
          </v-chip>
          <v-btn color="primary" prepend-icon="mdi-lock-outline" @click="emit('lock')">
            立即锁定
          </v-btn>
        </div>
      </v-card-text>
    </v-card>

    <v-row dense>
      <v-col cols="12" lg="6">
        <v-card class="border-sm h-100">
          <v-card-title>外观</v-card-title>
          <v-card-text>
            <v-list class="bg-transparent pa-0">
              <v-list-item rounded="xl">
                <template #prepend>
                  <v-avatar color="primary" variant="tonal" size="40">
                    <v-icon>{{ themeMode === "dark" ? "mdi-weather-night" : "mdi-weather-sunny" }}</v-icon>
                  </v-avatar>
                </template>

                <v-list-item-title>暗黑模式</v-list-item-title>
                <v-list-item-subtitle>
                  {{ themeMode === "dark" ? "当前为深色界面" : "当前为浅色界面" }}
                </v-list-item-subtitle>

                <template #append>
                  <v-switch
                    :model-value="themeMode === 'dark'"
                    color="primary"
                    hide-details
                    inset
                    @update:model-value="emit('toggle-theme', $event)"
                  />
                </template>
              </v-list-item>
            </v-list>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" lg="6">
        <v-card class="border-sm h-100">
          <v-card-title>安全</v-card-title>
          <v-card-text class="d-flex flex-column ga-4">
            <v-sheet class="rounded-xl pa-4 settings-block">
              <div class="text-subtitle-1 font-weight-medium">修改主密码</div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                修改后会用新的主密码重新保护现有数据。
              </div>
              <v-btn
                class="mt-4"
                color="primary"
                prepend-icon="mdi-lock-reset"
                :loading="changingMasterPassword"
                @click="emit('change-master-password')"
              >
                修改主密码
              </v-btn>
            </v-sheet>

            <v-sheet class="rounded-xl pa-4 settings-block">
              <div class="d-flex align-center justify-space-between flex-wrap ga-3">
                <div>
                  <div class="text-subtitle-1 font-weight-medium">生物识别解锁</div>
                  <div class="text-body-2 text-medium-emphasis mt-2">
                    {{ biometricDescription }}
                  </div>
                </div>

                <v-chip
                  :color="biometricEnabled ? 'primary' : 'surface-variant'"
                  :variant="biometricEnabled ? 'tonal' : 'flat'"
                >
                  {{ biometricEnabled ? "已启用" : "未启用" }}
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
              <div class="text-subtitle-1 font-weight-medium">关闭窗口时收纳到托盘</div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                仅在 Windows 上生效，关闭窗口时应用会隐藏到系统托盘。
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
              <div class="text-subtitle-1 font-weight-medium">开机自启动</div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                登录 Windows 后自动启动应用。
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
              <div class="text-subtitle-1 font-weight-medium">不在最近任务中显示</div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                开启后，Android 最近任务卡片中会隐藏当前应用。
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
              <div class="text-subtitle-1 font-weight-medium">自启动与后台运行</div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                Android 不同厂商的自启动入口不统一，这里会优先打开系统相关设置页。
              </div>
            </div>

            <v-btn
              color="primary"
              prepend-icon="mdi-open-in-new"
              :loading="autostartOpening"
              @click="emit('open-autostart-settings')"
            >
              打开系统设置
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
</style>
