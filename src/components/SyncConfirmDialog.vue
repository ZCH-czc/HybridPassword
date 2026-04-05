<script setup>
import { useAppPreferences } from "@/composables/useAppPreferences";

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  loading: {
    type: Boolean,
    default: false,
  },
  sourceLabel: {
    type: String,
    default: "",
  },
  localPreview: {
    type: Object,
    default: () => ({
      totalCount: 0,
      deletedCount: 0,
      latestItem: null,
    }),
  },
  remotePreview: {
    type: Object,
    default: () => ({
      totalCount: 0,
      deletedCount: 0,
      latestItem: null,
    }),
  },
});

const emit = defineEmits(["update:modelValue", "confirm"]);
const { t, formatDateTime } = useAppPreferences();

function formatPreview(preview) {
  if (!preview?.latestItem) {
    return t("common.none");
  }

  const siteName = preview.latestItem.siteName || t("common.unnamedEntry");
  return `${siteName} / ${preview.latestItem.username}`;
}

function formatTime(preview) {
  return preview?.latestItem?.createdAt ? formatDateTime(preview.latestItem.createdAt) : t("common.none");
}
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="760"
    persistent
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="border-sm">
      <v-card-title class="pt-6 px-6 text-h5">{{ t("syncConfirm.title") }}</v-card-title>
      <v-card-text class="px-6 pb-2">
        <div class="text-body-1">
          {{ t("syncConfirm.description", { source: sourceLabel }) }}
        </div>
        <div class="text-body-2 text-medium-emphasis mt-2">
          {{ t("syncConfirm.warning") }}
        </div>

        <v-row dense class="mt-4">
          <v-col cols="12" md="6">
            <v-sheet class="rounded-xl pa-4 sync-preview-card">
              <div class="text-subtitle-1 font-weight-medium">{{ t("common.currentDevice") }}</div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                {{ t("common.countItems", { count: localPreview.totalCount }) }},
                {{ t("list.statusDeleted", { count: localPreview.deletedCount }) }}
              </div>
              <div class="text-body-1 mt-4">{{ formatPreview(localPreview) }}</div>
              <div class="text-caption text-medium-emphasis mt-2">
                {{ t("syncConfirm.addedAt", { time: formatTime(localPreview) }) }}
              </div>
            </v-sheet>
          </v-col>

          <v-col cols="12" md="6">
            <v-sheet class="rounded-xl pa-4 sync-preview-card">
              <div class="text-subtitle-1 font-weight-medium">{{ sourceLabel }}</div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                {{ t("common.countItems", { count: remotePreview.totalCount }) }},
                {{ t("list.statusDeleted", { count: remotePreview.deletedCount }) }}
              </div>
              <div class="text-body-1 mt-4">{{ formatPreview(remotePreview) }}</div>
              <div class="text-caption text-medium-emphasis mt-2">
                {{ t("syncConfirm.addedAt", { time: formatTime(remotePreview) }) }}
              </div>
            </v-sheet>
          </v-col>
        </v-row>
      </v-card-text>

      <v-card-actions class="px-6 pb-6 pt-2 justify-end ga-2">
        <v-btn
          variant="text"
          :disabled="loading"
          @click="emit('update:modelValue', false)"
        >
          {{ t("common.cancel") }}
        </v-btn>
        <v-btn
          color="primary"
          prepend-icon="mdi-sync"
          :loading="loading"
          @click="emit('confirm')"
        >
          {{ t("common.confirm") }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.sync-preview-card {
  background: rgba(var(--v-theme-surface), 0.62);
}
</style>
