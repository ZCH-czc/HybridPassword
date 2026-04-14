<script setup>
import { computed, ref, watch } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

const props = defineProps({
  items: {
    type: Array,
    default: () => [],
  },
  busyIds: {
    type: Object,
    default: () => ({}),
  },
  batchLoading: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["restore", "permanent-delete", "restore-many", "permanent-delete-many"]);
const { t, formatDateTime } = useAppPreferences();
const PAGE_SIZE = 40;

const selectionMode = ref(false);
const selectedIds = ref([]);
const visibleCount = ref(PAGE_SIZE);

const selectedCount = computed(() => selectedIds.value.length);
const hasItems = computed(() => props.items.length > 0);
const visibleItems = computed(() => props.items.slice(0, visibleCount.value));

watch(
  () => props.items,
  (items) => {
    const visibleIds = new Set(items.map((item) => item.id));
    selectedIds.value = selectedIds.value.filter((id) => visibleIds.has(id));
    if (!items.length) {
      selectionMode.value = false;
    }
    visibleCount.value = Math.min(PAGE_SIZE, items.length);
  },
  { deep: true }
);

function toggleSelectionMode() {
  selectionMode.value = !selectionMode.value;
  if (!selectionMode.value) {
    selectedIds.value = [];
  }
}

function toggleSelected(id) {
  const nextSelected = new Set(selectedIds.value);
  if (nextSelected.has(id)) {
    nextSelected.delete(id);
  } else {
    nextSelected.add(id);
  }

  selectedIds.value = [...nextSelected];
}

function toggleSelectAll() {
  if (selectedIds.value.length === props.items.length) {
    selectedIds.value = [];
    return;
  }

  selectedIds.value = props.items.map((item) => item.id);
}

function emitRestoreMany() {
  if (!selectedIds.value.length) {
    return;
  }

  emit("restore-many", [...selectedIds.value]);
  selectionMode.value = false;
  selectedIds.value = [];
}

function emitPermanentDeleteMany() {
  if (!selectedIds.value.length) {
    return;
  }

  emit("permanent-delete-many", [...selectedIds.value]);
  selectionMode.value = false;
  selectedIds.value = [];
}

function handleRowClick(itemId) {
  if (!selectionMode.value) {
    return;
  }

  toggleSelected(itemId);
}

function handleScroll(event) {
  const element = event.target;
  if (!element || visibleCount.value >= props.items.length) {
    return;
  }

  const remaining = element.scrollHeight - element.scrollTop - element.clientHeight;
  if (remaining <= 120) {
    visibleCount.value = Math.min(props.items.length, visibleCount.value + PAGE_SIZE);
  }
}
</script>

<template>
  <v-card class="border-sm deleted-card">
    <v-card-title class="d-flex flex-column align-stretch ga-3 pa-4 pa-sm-5">
      <div class="d-flex align-center justify-space-between flex-wrap ga-3">
        <div class="d-flex align-center ga-3">
          <span>{{ t("deleted.title") }}</span>
          <v-chip variant="tonal" color="error">
            {{ t("common.countItems", { count: items.length }) }}
          </v-chip>
        </div>

        <div class="d-flex flex-wrap justify-end ga-2">
          <template v-if="selectionMode">
            <v-btn
              size="small"
              variant="text"
              :disabled="!hasItems || batchLoading"
              @click="toggleSelectAll"
            >
              {{
                selectedCount === items.length && items.length
                  ? t("list.unselectAll")
                  : t("list.selectAll")
              }}
            </v-btn>
            <v-btn
              size="small"
              variant="text"
              :disabled="!selectedCount || batchLoading"
              :loading="batchLoading"
              @click="emitRestoreMany"
            >
              {{ t("deleted.restoreSelected") }}
            </v-btn>
            <v-btn
              size="small"
              color="error"
              variant="tonal"
              :disabled="!selectedCount || batchLoading"
              :loading="batchLoading"
              @click="emitPermanentDeleteMany"
            >
              {{ t("deleted.deleteSelected") }}
            </v-btn>
            <v-btn size="small" variant="text" :disabled="batchLoading" @click="toggleSelectionMode">
              {{ t("common.done") }}
            </v-btn>
          </template>
          <v-btn
            v-else
            size="small"
            variant="text"
            :disabled="!hasItems"
            @click="toggleSelectionMode"
          >
            {{ t("list.select") }}
          </v-btn>
        </div>
      </div>

      <div v-if="selectionMode" class="text-caption text-medium-emphasis">
        {{ t("deleted.selectionCount", { count: selectedCount }) }}
      </div>
    </v-card-title>

    <v-card-text class="pa-4 pt-0 pa-sm-5 pt-sm-0">
      <div v-if="!items.length" class="py-8 text-center">
        <v-icon size="40" color="medium-emphasis">mdi-delete-clock-outline</v-icon>
        <div class="text-subtitle-1 mt-3">{{ t("deleted.emptyTitle") }}</div>
        <div class="text-body-2 text-medium-emphasis mt-2">
          {{ t("deleted.emptyBody") }}
        </div>
      </div>

      <div
        v-else
        class="deleted-scroll-shell vault-surface-block rounded-xl pa-2"
        @scroll.passive="handleScroll"
      >
        <TransitionGroup name="deleted-list" tag="div" class="deleted-scroll-list">
          <component
            v-for="item in visibleItems"
            :key="item.id"
            :is="selectionMode ? 'button' : 'div'"
            :type="selectionMode ? 'button' : undefined"
            :tabindex="selectionMode ? 0 : undefined"
            class="deleted-row"
            :class="{ 'deleted-row--selected': selectedIds.includes(item.id), 'deleted-row--selectable': selectionMode }"
            @click="handleRowClick(item.id)"
            @keydown.enter.prevent="handleRowClick(item.id)"
            @keydown.space.prevent="handleRowClick(item.id)"
          >
            <div class="deleted-row__main">
              <div v-if="selectionMode" class="deleted-row__check">
                <v-icon :icon="selectedIds.includes(item.id) ? 'mdi-check-circle' : 'mdi-circle-outline'" size="20" />
              </div>

              <div class="min-w-0">
                <div class="text-subtitle-2 font-weight-medium text-truncate">
                  {{ item.siteName || t("common.unnamedEntry") }}
                </div>
                <div class="text-body-2 text-medium-emphasis text-truncate mt-1">
                  {{ item.username || t("common.none") }}
                </div>
                <div class="text-caption text-medium-emphasis mt-1">
                  {{ t("deleted.deletedAt", { time: formatDateTime(item.deletedAt) }) }}
                </div>
              </div>
            </div>

            <div v-if="!selectionMode" class="d-flex align-center flex-wrap justify-end ga-2 deleted-row__actions">
              <v-btn
                size="small"
                variant="text"
                prepend-icon="mdi-restore"
                :loading="Boolean(busyIds[item.id])"
                @click.stop="emit('restore', item.id)"
              >
                {{ t("deleted.restore") }}
              </v-btn>
              <v-btn
                size="small"
                color="error"
                variant="tonal"
                prepend-icon="mdi-delete-forever-outline"
                :loading="Boolean(busyIds[item.id])"
                @click.stop="emit('permanent-delete', item.id)"
              >
                {{ t("deleted.permanentDelete") }}
              </v-btn>
            </div>
          </component>
        </TransitionGroup>

        <div v-if="visibleItems.length < items.length" class="d-flex justify-center py-3">
          <v-progress-circular indeterminate size="22" width="2" color="primary" />
        </div>
      </div>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.deleted-card {
  background: var(--vault-panel-bg);
}

.deleted-scroll-shell {
  max-height: 420px;
  overflow-y: auto;
  box-shadow: none;
}

.deleted-scroll-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.deleted-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 18px;
  width: 100%;
  padding: 14px 16px;
  border: none;
  border-radius: calc(var(--vault-radius) - 4px);
  background: var(--vault-block-bg-subtle);
  color: inherit;
  text-align: left;
  box-shadow: none;
  transition:
    transform 220ms ease,
    background-color 220ms ease,
    opacity 220ms ease;
}

.deleted-row--selectable {
  cursor: pointer;
}

.deleted-row--selectable:hover {
  transform: translateY(-1px);
}

.deleted-row--selected {
  background: rgba(var(--v-theme-primary), 0.12);
}

.deleted-row__main {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
  flex: 1 1 auto;
}

.deleted-row__check {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: rgb(var(--v-theme-primary));
}

.deleted-row__actions {
  flex: 0 0 auto;
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

@media (max-width: 680px) {
  .deleted-row {
    flex-direction: column;
    align-items: stretch;
  }

  .deleted-row__actions {
    width: 100%;
  }
}
</style>
