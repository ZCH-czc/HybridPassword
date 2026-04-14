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
  <v-card class="border-sm import-export-card">
    <v-card-title>{{ t("settings.importExport") }}</v-card-title>

    <v-card-text>
      <div class="text-body-2 text-medium-emphasis mb-2">{{ t("settings.importStrategy") }}</div>

      <v-sheet class="import-strategy-group vault-surface-block rounded-xl pa-2">
        <v-radio-group v-model="localStrategy" hide-details density="comfortable">
          <v-sheet class="import-strategy-option vault-surface-block vault-surface-block--subtle rounded-xl px-4 py-3">
            <v-radio :label="t('settings.importOverwrite')" value="overwrite" />
          </v-sheet>
          <v-sheet class="import-strategy-option vault-surface-block vault-surface-block--subtle rounded-xl px-4 py-3 mt-2">
            <v-radio :label="t('settings.importSkip')" value="skip" />
          </v-sheet>
        </v-radio-group>
      </v-sheet>

      <v-sheet class="mt-4 rounded-xl px-4 py-3 import-hint vault-surface-block vault-surface-block--subtle text-body-2 text-medium-emphasis">
        <div>{{ t("settings.importExportHint") }}</div>
        <div class="mt-1">
          {{ nativeFileDialogsAvailable ? t("settings.importExportNativeHint") : t("settings.importExportBrowserHint") }}
        </div>
        <div class="d-flex flex-wrap ga-2 mt-3">
          <span class="import-format-pill">1Password 1PUX</span>
          <span class="import-format-pill">1Password CSV</span>
          <span class="import-format-pill">CSV</span>
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
          {{ t("settings.importFile") }}
        </v-btn>
      </div>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.import-export-card {
  background: var(--vault-panel-bg);
}

.import-strategy-group,
.import-hint,
.import-strategy-option {
  box-shadow: none;
}

.import-format-pill {
  display: inline-flex;
  align-items: center;
  min-height: 30px;
  padding: 0 12px;
  border-radius: 999px;
  background: rgba(var(--v-theme-on-surface), 0.05);
  color: rgba(var(--v-theme-on-surface), 0.72);
  font-size: 0.8rem;
  font-weight: 600;
}

:deep(.import-strategy-option .v-selection-control) {
  margin: 0;
}

:deep(.import-strategy-option .v-selection-control__input),
:deep(.import-strategy-option .v-icon) {
  box-shadow: none !important;
}
</style>
