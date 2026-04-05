<script setup>
import { computed, ref, watch } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

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
const { t, formatDateTime } = useAppPreferences();

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
  return timestamp ? formatDateTime(timestamp) : t("common.none");
}

function formatLatestItem(device) {
  if (!device.preview?.latestItem) {
    return t("common.none");
  }

  const siteName = device.preview.latestItem.siteName || t("common.unnamedEntry");
  return `${siteName} / ${device.preview.latestItem.username}`;
}

function saveDeviceName() {
  emit("save-device-name", localDeviceName.value);
}
</script>

<template>
  <v-card class="border-sm">
    <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-3">
      <span>{{ t("settings.lanSync") }}</span>
      <v-btn
        variant="tonal"
        prepend-icon="mdi-radar"
        :loading="scanning"
        :disabled="disabled || savingDeviceName"
        @click="emit('scan')"
      >
        {{ t("settings.scanDevices") }}
      </v-btn>
    </v-card-title>

    <v-card-text class="d-flex flex-column ga-4">
      <v-sheet class="rounded-xl pa-4 settings-block">
        <div class="text-subtitle-1 font-weight-medium">{{ t("settings.deviceIdentity") }}</div>
        <div class="text-body-2 text-medium-emphasis mt-2">
          {{ t("settings.deviceIdentityBody") }}
        </div>

        <div class="d-flex flex-column flex-md-row ga-3 mt-4">
          <v-text-field
            v-model="localDeviceName"
            :label="t('settings.deviceName')"
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
            {{ t("settings.saveName") }}
          </v-btn>
        </div>
      </v-sheet>

      <v-sheet class="rounded-xl px-4 py-3 bg-surface-variant text-body-2 text-medium-emphasis">
        {{ t("settings.lanHint") }}
      </v-sheet>

      <div v-if="!visibleDevices.length" class="py-6 text-center text-medium-emphasis">
        <v-icon size="36">mdi-access-point-network</v-icon>
        <div class="mt-3">{{ t("settings.noLanDevices") }}</div>
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
                  {{ device.deviceName || t("common.unnamedDevice") }}
                </div>
                <v-chip
                  :color="device.snapshotAvailable ? 'primary' : 'surface-variant'"
                  :variant="device.snapshotAvailable ? 'tonal' : 'flat'"
                >
                  {{ device.snapshotAvailable ? t("settings.syncAvailable") : t("settings.syncUnavailable") }}
                </v-chip>
              </div>

              <div class="text-body-2 text-medium-emphasis mt-2">
                {{ device.host }}:{{ device.port }}
              </div>
              <div class="text-body-2 text-medium-emphasis mt-1">
                {{ t("common.countItems", { count: device.preview.totalCount }) }},
                {{ t("list.statusDeleted", { count: device.preview.deletedCount }) }}
              </div>
              <div class="text-body-2 text-medium-emphasis mt-1">
                {{ t("settings.latestAdded") }}: {{ formatLatestItem(device) }}
              </div>
              <div class="text-caption text-medium-emphasis mt-1">
                {{ t("settings.lastPublished", { time: formatTime(device.exportedAt) }) }}
              </div>
            </div>

            <v-btn
              color="primary"
              prepend-icon="mdi-sync"
              :disabled="disabled || !device.snapshotAvailable"
              @click="emit('sync-device', device)"
            >
              {{ t("settings.useDeviceData") }}
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
