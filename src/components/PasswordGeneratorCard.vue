<script setup>
import { reactive, ref, watch } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";
import {
  DEFAULT_GENERATOR_OPTIONS,
  generateRandomPassword,
} from "@/utils/password-generator";

const emit = defineEmits(["apply", "copy"]);
const { t } = useAppPreferences();

const options = reactive({ ...DEFAULT_GENERATOR_OPTIONS });
const preview = ref("");
const errorText = ref("");

function refreshPreview() {
  try {
    preview.value = generateRandomPassword(options);
    errorText.value = "";
  } catch {
    preview.value = "";
    errorText.value = t("generator.error");
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
      <span>{{ t("generator.title") }}</span>
      <v-btn icon variant="text" size="small" @click="refreshPreview">
        <v-icon>mdi-refresh</v-icon>
      </v-btn>
    </v-card-title>

    <v-card-text>
      <div class="d-flex align-center justify-space-between mb-1">
        <span class="text-body-2 text-medium-emphasis">{{ t("generator.length") }}</span>
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
        <v-checkbox v-model="options.includeUppercase" hide-details density="comfortable" :label="t('generator.uppercase')" />
        <v-checkbox v-model="options.includeLowercase" hide-details density="comfortable" :label="t('generator.lowercase')" />
        <v-checkbox v-model="options.includeNumbers" hide-details density="comfortable" :label="t('generator.numbers')" />
        <v-checkbox v-model="options.includeSymbols" hide-details density="comfortable" :label="t('generator.symbols')" />
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
        <div class="text-caption text-medium-emphasis mb-1">{{ t("generator.preview") }}</div>
        <div class="text-body-2 password-preview">
          {{ preview || t("generator.emptyPreview") }}
        </div>
      </v-sheet>

      <div class="d-flex flex-wrap ga-2 mt-4">
        <v-btn variant="text" prepend-icon="mdi-content-copy" :disabled="!preview" @click="copyPreview">
          {{ t("generator.copyPreview") }}
        </v-btn>
        <v-btn
          color="primary"
          prepend-icon="mdi-arrow-down-bold-circle-outline"
          :disabled="!preview"
          @click="applyToForm"
        >
          {{ t("generator.fillForm") }}
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
