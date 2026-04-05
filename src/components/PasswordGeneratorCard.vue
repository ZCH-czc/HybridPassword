<script setup>
import { reactive, ref, watch } from "vue";
import {
  DEFAULT_GENERATOR_OPTIONS,
  generateRandomPassword,
} from "@/utils/password-generator";

const emit = defineEmits(["apply", "copy"]);

const options = reactive({ ...DEFAULT_GENERATOR_OPTIONS });
const preview = ref("");
const errorText = ref("");

function refreshPreview() {
  try {
    preview.value = generateRandomPassword(options);
    errorText.value = "";
  } catch (error) {
    preview.value = "";
    errorText.value = error.message || "无法生成密码。";
  }
}

function applyToForm() {
  if (preview.value) {
    emit("apply", preview.value);
  }
}

function copyPreview() {
  if (preview.value) {
    emit("copy", preview.value);
  }
}

watch(options, refreshPreview, { immediate: true, deep: true });
</script>

<template>
  <v-card class="border-sm">
    <v-card-title class="d-flex align-center justify-space-between">
      <span>随机密码生成器</span>
      <v-btn icon variant="text" size="small" @click="refreshPreview">
        <v-icon>mdi-refresh</v-icon>
      </v-btn>
    </v-card-title>

    <v-card-text>
      <div class="d-flex align-center justify-space-between mb-1">
        <span class="text-body-2 text-medium-emphasis">密码长度</span>
        <span class="text-subtitle-2">{{ options.length }}</span>
      </div>

      <v-slider
        v-model="options.length"
        :min="6"
        :max="64"
        :step="1"
        color="primary"
        hide-details
      />

      <div class="d-flex flex-column ga-1 mt-2">
        <v-checkbox v-model="options.includeUppercase" hide-details density="comfortable" label="大写字母 A-Z" />
        <v-checkbox v-model="options.includeLowercase" hide-details density="comfortable" label="小写字母 a-z" />
        <v-checkbox v-model="options.includeNumbers" hide-details density="comfortable" label="数字 0-9" />
        <v-checkbox v-model="options.includeSymbols" hide-details density="comfortable" label="特殊字符" />
      </div>

      <v-alert
        v-if="errorText"
        type="warning"
        variant="tonal"
        density="comfortable"
        class="mt-4"
      >
        {{ errorText }}
      </v-alert>

      <v-sheet class="mt-4 pa-4 rounded-lg border-sm bg-surface-variant">
        <div class="text-caption text-medium-emphasis mb-1">实时预览</div>
        <div class="text-body-2 password-preview">
          {{ preview || "请至少选择一种字符类型" }}
        </div>
      </v-sheet>

      <div class="d-flex flex-wrap ga-2 mt-4">
        <v-btn variant="text" prepend-icon="mdi-content-copy" :disabled="!preview" @click="copyPreview">
          复制预览
        </v-btn>
        <v-btn color="primary" prepend-icon="mdi-arrow-down-bold-circle-outline" :disabled="!preview" @click="applyToForm">
          填入表单
        </v-btn>
      </div>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.password-preview {
  word-break: break-all;
  font-family: "Cascadia Code", "Consolas", monospace;
}
</style>
