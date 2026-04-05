<script setup>
import { computed, ref, watch } from "vue";

const props = defineProps({
  deviceName: {
    type: String,
    default: "",
  },
  devices: {
    type: Array,
    default: () => [],
  },
  disabled: {
    type: Boolean,
    default: false,
  },
  scanning: {
    type: Boolean,
    default: false,
  },
  savingDeviceName: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["save-device-name", "scan", "sync-device"]);

const localDeviceName = ref("");

watch(
  () => props.deviceName,
  (value) => {
    localDeviceName.value = value || "";
  },
  { immediate: true }
);

const visibleDevices = computed(() =>
  props.devices.filter((device) => !device.isCurrentDevice)
);

function formatTime(timestamp) {
  if (!timestamp) {
    return "暂无";
  }

  return new Date(timestamp).toLocaleString();
}

function saveDeviceName() {
  emit("save-device-name", localDeviceName.value);
}
</script>

<template>
  <v-card class="border-sm">
    <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-3">
      <span>局域网同步</span>
      <v-btn
        variant="tonal"
        prepend-icon="mdi-radar"
        :loading="scanning"
        :disabled="disabled || savingDeviceName"
        @click="emit('scan')"
      >
        扫描设备
      </v-btn>
    </v-card-title>

    <v-card-text class="d-flex flex-column ga-4">
      <v-sheet class="rounded-xl pa-4 settings-block">
        <div class="text-subtitle-1 font-weight-medium">本机标识</div>
        <div class="text-body-2 text-medium-emphasis mt-2">
          其他设备会看到这个名称，并用它来区分同步来源。
        </div>

        <div class="d-flex flex-column flex-md-row ga-3 mt-4">
          <v-text-field
            v-model="localDeviceName"
            label="设备名称"
            variant="solo-filled"
            hide-details
            class="flex-grow-1"
            :disabled="disabled"
          />
          <v-btn
            color="primary"
            prepend-icon="mdi-content-save-outline"
            :loading="savingDeviceName"
            :disabled="disabled"
            @click="saveDeviceName"
          >
            保存名称
          </v-btn>
        </div>
      </v-sheet>

      <v-sheet class="rounded-xl px-4 py-3 bg-surface-variant text-body-2 text-medium-emphasis">
        同步前会展示“本机”和“目标设备”最新添加的项目，用来帮你确认哪一侧数据更新。
      </v-sheet>

      <div v-if="!visibleDevices.length" class="py-6 text-center text-medium-emphasis">
        <v-icon size="36">mdi-access-point-network</v-icon>
        <div class="mt-3">还没有扫描到其他可用设备</div>
      </div>

      <TransitionGroup
        v-else
        name="lan-device"
        tag="div"
        class="d-flex flex-column ga-3"
      >
        <v-sheet
          v-for="device in visibleDevices"
          :key="`${device.deviceId}-${device.host}-${device.port}`"
          class="rounded-xl pa-4 settings-block"
        >
          <div class="d-flex flex-column flex-lg-row align-lg-center justify-space-between ga-4">
            <div class="min-w-0">
              <div class="d-flex align-center flex-wrap ga-2">
                <div class="text-subtitle-1 font-weight-medium text-truncate">
                  {{ device.deviceName || "未命名设备" }}
                </div>
                <v-chip
                  :color="device.snapshotAvailable ? 'primary' : 'surface-variant'"
                  :variant="device.snapshotAvailable ? 'tonal' : 'flat'"
                >
                  {{ device.snapshotAvailable ? "可同步" : "暂无数据" }}
                </v-chip>
              </div>

              <div class="text-body-2 text-medium-emphasis mt-2">
                {{ device.host }}:{{ device.port }}
              </div>
              <div class="text-body-2 text-medium-emphasis mt-1">
                已保存 {{ device.preview.totalCount }}条，最近删除 {{ device.preview.deletedCount }}条
              </div>
              <div class="text-body-2 text-medium-emphasis mt-1">
                最新添加：
                {{
                  device.preview.latestItem
                    ? `${device.preview.latestItem.siteName} / ${device.preview.latestItem.username}`
                    : "暂无"
                }}
              </div>
              <div class="text-caption text-medium-emphasis mt-1">
                最近发布 {{ formatTime(device.exportedAt) }}
              </div>
            </div>

            <v-btn
              color="primary"
              prepend-icon="mdi-sync"
              :disabled="disabled || !device.snapshotAvailable"
              @click="emit('sync-device', device)"
            >
              使用这台设备的数据
            </v-btn>
          </div>
        </v-sheet>
      </TransitionGroup>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.settings-block {
  background: rgba(var(--v-theme-surface), 0.62);
}

.min-w-0 {
  min-width: 0;
}

.lan-device-enter-active,
.lan-device-leave-active,
.lan-device-move {
  transition: all 240ms cubic-bezier(0.2, 0.7, 0.2, 1);
}

.lan-device-enter-from,
.lan-device-leave-to {
  opacity: 0;
  transform: translateY(10px);
}
</style>
