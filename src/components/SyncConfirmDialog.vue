<script setup>
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

function formatPreview(preview) {
  if (!preview?.latestItem) {
    return "暂无";
  }

  return `${preview.latestItem.siteName} / ${preview.latestItem.username}`;
}

function formatTime(preview) {
  if (!preview?.latestItem?.createdAt) {
    return "暂无";
  }

  return new Date(preview.latestItem.createdAt).toLocaleString();
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
      <v-card-title class="pt-6 px-6 text-h5">确认同步</v-card-title>
      <v-card-text class="px-6 pb-2">
        <div class="text-body-1">
          你将使用 <strong>{{ sourceLabel }}</strong> 的数据覆盖当前设备。
        </div>
        <div class="text-body-2 text-medium-emphasis mt-2">
          为了避免误同步，下面会同时显示两边最近新增的项目。确认后，本机会替换为来源设备的整库加密快照。
        </div>

        <v-row dense class="mt-4">
          <v-col cols="12" md="6">
            <v-sheet class="rounded-xl pa-4 sync-preview-card">
              <div class="text-subtitle-1 font-weight-medium">当前设备</div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                已保存 {{ localPreview.totalCount }}条，最近删除 {{ localPreview.deletedCount }}条
              </div>
              <div class="text-body-1 mt-4">{{ formatPreview(localPreview) }}</div>
              <div class="text-caption text-medium-emphasis mt-2">
                添加时间 {{ formatTime(localPreview) }}
              </div>
            </v-sheet>
          </v-col>

          <v-col cols="12" md="6">
            <v-sheet class="rounded-xl pa-4 sync-preview-card">
              <div class="text-subtitle-1 font-weight-medium">{{ sourceLabel }}</div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                已保存 {{ remotePreview.totalCount }}条，最近删除 {{ remotePreview.deletedCount }}条
              </div>
              <div class="text-body-1 mt-4">{{ formatPreview(remotePreview) }}</div>
              <div class="text-caption text-medium-emphasis mt-2">
                添加时间 {{ formatTime(remotePreview) }}
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
          取消
        </v-btn>
        <v-btn
          color="primary"
          prepend-icon="mdi-sync"
          :loading="loading"
          @click="emit('confirm')"
        >
          确认同步
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
