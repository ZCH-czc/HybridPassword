<script setup>
import { computed } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";
import PasswordListItem from "@/components/PasswordListItem.vue";

const props = defineProps({
  items: {
    type: Array,
    default: () => [],
  },
  revealedPasswords: {
    type: Object,
    default: () => ({}),
  },
  revealingIds: {
    type: Object,
    default: () => ({}),
  },
  editingIds: {
    type: Object,
    default: () => ({}),
  },
  favoriteIds: {
    type: Object,
    default: () => ({}),
  },
  selectionMode: {
    type: Boolean,
    default: false,
  },
  selectedIds: {
    type: Array,
    default: () => [],
  },
  bulkLoading: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits([
  "toggle-reveal",
  "toggle-favorite",
  "edit",
  "delete",
  "copy-password",
  "copy-username",
  "toggle-select",
  "toggle-selection-mode",
  "select-all",
  "clear-selection",
  "bulk-favorite",
  "bulk-delete",
]);

const { t } = useAppPreferences();
const selectedCount = computed(() => props.selectedIds.length);
const allSelected = computed(
  () => props.items.length > 0 && selectedCount.value === props.items.length
);
const selectedItems = computed(() => {
  const selectedIdSet = new Set(props.selectedIds);
  return props.items.filter((item) => selectedIdSet.has(item.id));
});
const bulkFavoriteLabel = computed(() => {
  if (!selectedItems.value.length) {
    return t("list.bulkFavorite");
  }

  return selectedItems.value.every((item) => item.isFavorite)
    ? t("list.bulkUnfavorite")
    : t("list.bulkFavorite");
});
</script>

<template>
  <v-card class="border-sm">
    <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-3">
      <div class="d-flex align-center ga-3">
        <span>{{ t("list.savedPasswords") }}</span>
        <v-chip color="primary" variant="tonal">{{ t("common.countItems", { count: items.length }) }}</v-chip>
        <v-chip v-if="selectionMode" variant="tonal" color="secondary">
          {{ t("list.selectedCount", { count: selectedCount }) }}
        </v-chip>
      </div>

      <div class="d-flex flex-wrap ga-2">
        <template v-if="selectionMode">
          <v-btn variant="text" :disabled="!items.length" @click="emit('select-all', !allSelected)">
            {{ allSelected ? t("list.unselectAll") : t("list.selectAll") }}
          </v-btn>
          <v-btn
            variant="text"
            :disabled="!selectedCount || bulkLoading"
            @click="emit('bulk-favorite')"
          >
            {{ bulkFavoriteLabel }}
          </v-btn>
          <v-btn
            color="error"
            variant="tonal"
            :disabled="!selectedCount || bulkLoading"
            @click="emit('bulk-delete')"
          >
            {{ t("list.bulkDelete") }}
          </v-btn>
          <v-btn variant="text" @click="emit('toggle-selection-mode', false)">
            {{ t("list.complete") }}
          </v-btn>
        </template>

        <v-btn
          v-else
          variant="text"
          prepend-icon="mdi-check-circle-outline"
          :disabled="!items.length"
          @click="emit('toggle-selection-mode', true)"
        >
          {{ t("list.select") }}
        </v-btn>
      </div>
    </v-card-title>

    <v-card-text class="pa-4">
      <div v-if="!items.length" class="py-10 text-center">
        <v-icon size="44" color="medium-emphasis">mdi-folder-key-outline</v-icon>
        <div class="text-h6 mt-3">{{ t("list.noResults") }}</div>
        <div class="text-body-2 text-medium-emphasis mt-2">
          {{ t("list.noResultsBody") }}
        </div>
      </div>

      <TransitionGroup v-else name="vault-list" tag="div">
        <PasswordListItem
          v-for="item in items"
          :key="item.id"
          :item="item"
          :revealed-password="revealedPasswords[item.id] || ''"
          :reveal-loading="Boolean(revealingIds[item.id])"
          :edit-loading="Boolean(editingIds[item.id])"
          :favorite-loading="Boolean(favoriteIds[item.id])"
          :selection-mode="selectionMode"
          :selected="selectedIds.includes(item.id)"
          @toggle-reveal="emit('toggle-reveal', $event)"
          @toggle-favorite="emit('toggle-favorite', $event)"
          @edit="emit('edit', $event)"
          @delete="emit('delete', $event)"
          @copy-password="emit('copy-password', $event)"
          @copy-username="emit('copy-username', $event)"
          @toggle-select="emit('toggle-select', $event)"
        />
      </TransitionGroup>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.vault-list-enter-active,
.vault-list-leave-active,
.vault-list-move {
  transition: all 260ms cubic-bezier(0.2, 0.7, 0.2, 1);
}

.vault-list-enter-from,
.vault-list-leave-to {
  opacity: 0;
  transform: translateY(10px) scale(0.98);
}
</style>
