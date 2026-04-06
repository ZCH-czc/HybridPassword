<script setup>
import { useAppPreferences } from "@/composables/useAppPreferences";

defineProps({
  total: {
    type: Number,
    default: 0,
  },
  filtered: {
    type: Number,
    default: 0,
  },
  notes: {
    type: Number,
    default: 0,
  },
});

const { t } = useAppPreferences();
</script>

<template>
  <v-row class="mb-4" dense>
    <v-col cols="12" sm="4">
      <v-card class="border-sm summary-card">
        <v-card-text>
          <div class="d-flex align-center justify-space-between ga-3">
            <div>
              <div class="text-body-2 text-medium-emphasis">{{ t("home.savedCount", { count: total }) }}</div>
              <div class="text-h5 font-weight-medium mt-1">{{ total }}</div>
            </div>
            <v-avatar color="primary" variant="tonal">
              <v-icon>mdi-key-chain-variant</v-icon>
            </v-avatar>
          </div>
        </v-card-text>
      </v-card>
    </v-col>

    <v-col cols="12" sm="4">
      <v-card class="border-sm summary-card">
        <v-card-text>
          <div class="d-flex align-center justify-space-between ga-3">
            <div>
              <div class="text-body-2 text-medium-emphasis">{{ t("home.filteredCount", { count: filtered }) }}</div>
              <div class="text-h5 font-weight-medium mt-1">{{ filtered }}</div>
            </div>
            <v-avatar color="secondary" variant="tonal">
              <v-icon>mdi-filter-variant</v-icon>
            </v-avatar>
          </div>
        </v-card-text>
      </v-card>
    </v-col>

    <v-col cols="12" sm="4">
      <v-card class="border-sm summary-card">
        <v-card-text>
          <div class="d-flex align-center justify-space-between ga-3">
            <div>
              <div class="text-body-2 text-medium-emphasis">{{ t("home.notesCount", { count: notes }) }}</div>
              <div class="text-h5 font-weight-medium mt-1">{{ notes }}</div>
            </div>
            <v-avatar color="warning" variant="tonal">
              <v-icon>mdi-note-multiple-outline</v-icon>
            </v-avatar>
          </div>
        </v-card-text>
      </v-card>
    </v-col>
  </v-row>
</template>

<style scoped>
.summary-card {
  position: relative;
  overflow: hidden;
  background:
    linear-gradient(
      180deg,
      rgba(var(--v-theme-surface), 1),
      rgba(var(--v-theme-surface), 0.88)
    ),
    radial-gradient(circle at top right, rgba(var(--v-theme-primary), 0.12), transparent 42%);
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  box-shadow:
    0 12px 24px rgba(22, 32, 46, 0.06),
    inset 0 1px 0 rgba(255, 255, 255, 0.46);
}

.summary-card::before {
  content: "";
  position: absolute;
  inset: auto -10% 44% 28%;
  height: 42%;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.26), transparent);
  transform: translateX(-48%) skewX(-20deg);
  filter: blur(8px);
  opacity: 0;
  animation: vault-sheen 12s ease-in-out infinite;
  pointer-events: none;
}
</style>
