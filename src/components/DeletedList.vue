<script setup>
import { useAppPreferences } from "@/composables/useAppPreferences";

defineProps({
  items: {
    type: Array,
    default: () => [],
  },
  busyIds: {
    type: Object,
    default: () => ({}),
  },
});

const emit = defineEmits(["restore", "permanent-delete"]);
const { t, formatDateTime } = useAppPreferences();
</script>

<template>
  <v-card class="border-sm">
    <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-3">
      <span>{{ t("deleted.title") }}</span>
      <v-chip variant="tonal" color="error">{{ t("common.countItems", { count: items.length }) }}</v-chip>
    </v-card-title>

    <v-card-text class="pa-4">
      <div v-if="!items.length" class="py-8 text-center">
        <v-icon size="40" color="medium-emphasis">mdi-delete-clock-outline</v-icon>
        <div class="text-subtitle-1 mt-3">{{ t("deleted.emptyTitle") }}</div>
        <div class="text-body-2 text-medium-emphasis mt-2">
          {{ t("deleted.emptyBody") }}
        </div>
      </div>

      <TransitionGroup v-else name="deleted-list" tag="div" class="d-flex flex-column ga-3">
        <v-sheet
          v-for="item in items"
          :key="item.id"
          class="pa-4 rounded-xl bg-surface deleted-tile"
        >
          <div class="d-flex flex-column flex-md-row align-md-center justify-space-between ga-4">
            <div class="min-w-0">
              <div class="text-subtitle-1 font-weight-medium text-truncate">
                {{ item.siteName || t("common.unnamedEntry") }}
              </div>
              <div class="text-body-2 text-medium-emphasis text-truncate mt-1">
                {{ item.username }}
              </div>
              <div class="text-caption text-medium-emphasis mt-2">
                {{ t("deleted.deletedAt", { time: formatDateTime(item.deletedAt) }) }}
              </div>
            </div>

            <div class="d-flex flex-wrap ga-2">
              <v-btn
                variant="text"
                prepend-icon="mdi-restore"
                :loading="Boolean(busyIds[item.id])"
                @click="emit('restore', item.id)"
              >
                {{ t("deleted.restore") }}
              </v-btn>
              <v-btn
                color="error"
                variant="tonal"
                prepend-icon="mdi-delete-forever-outline"
                :loading="Boolean(busyIds[item.id])"
                @click="emit('permanent-delete', item.id)"
              >
                {{ t("deleted.permanentDelete") }}
              </v-btn>
            </div>
          </div>
        </v-sheet>
      </TransitionGroup>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.deleted-tile {
  transition:
    transform 240ms ease,
    box-shadow 240ms ease,
    background-color 240ms ease;
}

.deleted-tile:hover {
  transform: translateY(-2px);
  box-shadow: 0 14px 30px rgba(28, 38, 63, 0.08);
}

.min-w-0 {
  min-width: 0;
}

.deleted-list-enter-active,
.deleted-list-leave-active,
.deleted-list-move {
  transition: all 260ms cubic-bezier(0.2, 0.7, 0.2, 1);
}

.deleted-list-enter-from,
.deleted-list-leave-to {
  opacity: 0;
  transform: translateY(12px);
}
</style>
