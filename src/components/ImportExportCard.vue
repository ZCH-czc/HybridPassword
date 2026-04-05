<script setup>
import { ref, watch } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

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
const { t } = useAppPreferences();
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
    <v-card-title>{{ t("settings.importExport") }}</v-card-title>

    <v-card-text>
      <div class="text-body-2 text-medium-emphasis mb-2">{{ t("settings.importStrategy") }}</div>

      <v-radio-group v-model="localStrategy" hide-details density="comfortable">
        <v-radio :label="t('settings.importOverwrite')" value="overwrite" />
        <v-radio :label="t('settings.importSkip')" value="skip" />
      </v-radio-group>

      <v-sheet class="mt-4 rounded-xl px-4 py-3 bg-surface-variant text-body-2 text-medium-emphasis">
        <div>{{ t("settings.importExportHint") }}</div>
        <div class="mt-1">
          {{ nativeFileDialogsAvailable ? t("settings.importExportNativeHint") : t("settings.importExportBrowserHint") }}
        </div>
      </v-sheet>

      <div class="d-flex flex-column ga-2 mt-4">
        <v-btn
          variant="tonal"
          prepend-icon="mdi-file-delimited-outline"
          :disabled="busy"
          @click="emit('export', 'csv')"
        >
          {{ t("settings.exportCsv") }}
        </v-btn>
        <v-btn
          variant="tonal"
          prepend-icon="mdi-file-document-outline"
          :disabled="busy"
          @click="emit('export', 'txt')"
        >
          {{ t("settings.exportTxt") }}
        </v-btn>
        <v-btn
          color="primary"
          prepend-icon="mdi-file-import-outline"
          :loading="busy"
          @click="emit('import', localStrategy)"
        >
          {{ t("settings.importCsv") }}
        </v-btn>
      </div>
    </v-card-text>
  </v-card>
</template>
