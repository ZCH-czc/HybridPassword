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
  <v-row class="mb-4" dense data-tour-target="home-summary">
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
  background: var(--vault-panel-bg);
  box-shadow: none;
}

.summary-card::before {
  display: none;
}
</style>
