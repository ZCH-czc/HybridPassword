<script setup>
import { ref, watch } from "vue";

const props = defineProps({
  importStrategy: {
    type: String,
    default: "overwrite",
  },
  busy: {
    type: Boolean,
    default: false,
  },
  nativeFileDialogsAvailable: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["update:importStrategy", "export", "import"]);
const localStrategy = ref(props.importStrategy);

watch(
  () => props.importStrategy,
  (value) => {
    localStrategy.value = value;
  }
);

watch(localStrategy, (value) => {
  emit("update:importStrategy", value);
});
</script>

<template>
  <v-card class="border-sm">
    <v-card-title>导入与导出</v-card-title>

    <v-card-text>
      <div class="text-body-2 text-medium-emphasis mb-2">CSV 冲突策略</div>

      <v-radio-group v-model="localStrategy" hide-details density="comfortable">
        <v-radio label="按用户名覆盖已有记录" value="overwrite" />
        <v-radio label="按用户名跳过重复记录" value="skip" />
      </v-radio-group>

      <v-sheet class="mt-4 rounded-xl px-4 py-3 bg-surface-variant text-body-2 text-medium-emphasis">
        <div>多条备注会使用 <code>|</code> 连接保存。</div>
        <div class="mt-1">
          {{ nativeFileDialogsAvailable ? "当前会调用系统文件管理器。" : "浏览器调试环境会使用网页文件选择与下载。" }}
        </div>
      </v-sheet>

      <div class="d-flex flex-column ga-2 mt-4">
        <v-btn
          variant="tonal"
          prepend-icon="mdi-file-delimited-outline"
          :disabled="busy"
          @click="emit('export', 'csv')"
        >
          导出 CSV
        </v-btn>
        <v-btn
          variant="tonal"
          prepend-icon="mdi-file-document-outline"
          :disabled="busy"
          @click="emit('export', 'txt')"
        >
          导出 TXT
        </v-btn>
        <v-btn
          color="primary"
          prepend-icon="mdi-file-import-outline"
          :loading="busy"
          @click="emit('import', localStrategy)"
        >
          导入 CSV
        </v-btn>
      </div>
    </v-card-text>
  </v-card>
</template>
