<script setup>
import { computed, ref, watch } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

const props = defineProps({
  items: {
    type: Array,
    default: () => [],
  },
  busy: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits([
  "manual-add",
  "manual-add-many",
  "dismiss",
  "dismiss-many",
]);

const { t } = useAppPreferences();
const selectionMode = ref(false);
const selectedIds = ref([]);

const hasItems = computed(() => props.items.length > 0);
const selectedItems = computed(() => {
  const idSet = new Set(selectedIds.value);
  return props.items.filter((item) => idSet.has(item.id));
});

watch(
  () => props.items,
  (items) => {
    const visibleIds = new Set(items.map((item) => item.id));
    selectedIds.value = selectedIds.value.filter((id) => visibleIds.has(id));
    if (!items.length) {
      selectionMode.value = false;
    }
  },
  { deep: true }
);

function getSourceLabel(sourceType) {
  if (sourceType === "1password-1pux") {
    return "1Password 1PUX";
  }

  if (sourceType === "1password-csv") {
    return "1Password CSV";
  }

  return "CSV";
}

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

function handleRowClick(itemId) {
  if (!selectionMode.value) {
    return;
  }

  toggleSelected(itemId);
}

function handleManualAddMany() {
  if (!selectedItems.value.length) {
    return;
  }

  emit("manual-add-many", selectedItems.value);
  selectionMode.value = false;
  selectedIds.value = [];
}

function handleDismissMany() {
  if (!selectedIds.value.length) {
    return;
  }

  emit("dismiss-many", [...selectedIds.value]);
  selectionMode.value = false;
  selectedIds.value = [];
}
</script>

<template>
  <div class="d-flex flex-column ga-4">
    <div class="d-flex flex-wrap align-center justify-space-between ga-3">
      <div v-if="selectionMode" class="text-caption text-medium-emphasis">
        {{ t("importReview.selectionCount", { count: selectedIds.length }) }}
      </div>
      <div v-else class="text-caption text-medium-emphasis">
        {{ t("importReview.savedHint") }}
      </div>

      <div class="d-flex flex-wrap justify-end ga-2">
        <template v-if="selectionMode">
          <v-btn size="small" variant="text" :disabled="!hasItems || busy" @click="toggleSelectAll">
            {{
              selectedIds.length === items.length && items.length
                ? t("list.unselectAll")
                : t("list.selectAll")
            }}
          </v-btn>
          <v-btn
            size="small"
            variant="text"
            :disabled="!selectedIds.length || busy"
            :loading="busy"
            @click="handleManualAddMany"
          >
            {{ t("importReview.addSelectedManually") }}
          </v-btn>
          <v-btn
            size="small"
            color="error"
            variant="tonal"
            :disabled="!selectedIds.length || busy"
            :loading="busy"
            @click="handleDismissMany"
          >
            {{ t("importReview.dismissSelected") }}
          </v-btn>
          <v-btn size="small" variant="text" :disabled="busy" @click="toggleSelectionMode">
            {{ t("common.done") }}
          </v-btn>
        </template>

        <v-btn
          v-else
          size="small"
          variant="text"
          :disabled="!hasItems || busy"
          @click="toggleSelectionMode"
        >
          {{ t("list.select") }}
        </v-btn>
      </div>
    </div>

    <div v-if="!hasItems" class="py-8 text-center">
      <v-icon size="40" color="medium-emphasis">mdi-file-alert-outline</v-icon>
      <div class="text-subtitle-1 mt-3">{{ t("importReview.emptyTitle") }}</div>
      <div class="text-body-2 text-medium-emphasis mt-2">
        {{ t("importReview.emptyBody") }}
      </div>
    </div>

    <div v-else class="import-review-scroll-shell vault-surface-block rounded-xl pa-2">
      <TransitionGroup name="import-review-list" tag="div" class="d-flex flex-column ga-3">
        <component
          :is="selectionMode ? 'button' : 'div'"
          v-for="item in items"
          :key="item.id"
          :type="selectionMode ? 'button' : undefined"
          :tabindex="selectionMode ? 0 : undefined"
          class="import-review-row"
          :class="{
            'import-review-row--selected': selectedIds.includes(item.id),
            'import-review-row--selectable': selectionMode,
          }"
          @click="handleRowClick(item.id)"
          @keydown.enter.prevent="handleRowClick(item.id)"
          @keydown.space.prevent="handleRowClick(item.id)"
        >
          <div class="import-review-row__main">
            <div v-if="selectionMode" class="import-review-row__check">
              <v-icon
                :icon="selectedIds.includes(item.id) ? 'mdi-check-circle' : 'mdi-circle-outline'"
                size="20"
              />
            </div>

            <div class="min-w-0">
              <div class="d-flex flex-wrap align-center ga-2">
                <div class="text-subtitle-2 font-weight-bold text-truncate">
                  {{ item.siteName || t("common.unnamedEntry") }}
                </div>
                <span class="import-review-tag">
                  {{ getSourceLabel(item.sourceType) }}
                </span>
              </div>

              <div v-if="item.username" class="text-body-2 text-medium-emphasis mt-1">
                {{ item.username }}
              </div>

              <div class="text-body-2 mt-3 import-review-reason">
                {{ item.reason }}
              </div>

              <div v-if="item.notes?.length" class="d-flex flex-column ga-2 mt-4">
                <div class="text-caption text-medium-emphasis">
                  {{ t("importReview.capturedNotes") }}
                </div>
                <div
                  v-for="(note, index) in item.notes.slice(0, 4)"
                  :key="`${item.id}-${index}`"
                  class="text-body-2 import-review-note"
                >
                  {{ note }}
                </div>
                <div
                  v-if="item.notes.length > 4"
                  class="text-caption text-medium-emphasis"
                >
                  {{ t("importReview.moreNotes", { count: item.notes.length - 4 }) }}
                </div>
              </div>
            </div>
          </div>

          <div
            v-if="!selectionMode"
            class="d-flex align-center flex-wrap justify-end ga-2 import-review-row__actions"
          >
            <v-btn
              size="small"
              color="primary"
              prepend-icon="mdi-plus"
              :disabled="busy"
              @click.stop="emit('manual-add', item)"
            >
              {{ t("importReview.addManually") }}
            </v-btn>
            <v-btn
              size="small"
              variant="text"
              color="error"
              prepend-icon="mdi-close"
              :disabled="busy"
              @click.stop="emit('dismiss', item.id)"
            >
              {{ t("importReview.dismiss") }}
            </v-btn>
          </div>
        </component>
      </TransitionGroup>
    </div>
  </div>
</template>

<style scoped>
.import-review-scroll-shell {
  max-height: 460px;
  overflow-y: auto;
  box-shadow: none;
}

.import-review-row {
  display: flex;
  align-items: flex-start;
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

.import-review-row--selectable {
  cursor: pointer;
}

.import-review-row--selectable:hover {
  transform: translateY(-1px);
}

.import-review-row--selected {
  background: rgba(var(--v-theme-primary), 0.12);
}

.import-review-row__main {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  min-width: 0;
  flex: 1 1 auto;
}

.import-review-row__check {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: rgb(var(--v-theme-primary));
  margin-top: 2px;
}

.import-review-row__actions {
  flex: 0 0 auto;
}

.import-review-tag {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 28px;
  padding: 0 10px;
  border-radius: 999px;
  background: rgba(var(--v-theme-on-surface), 0.05);
  color: rgba(var(--v-theme-on-surface), 0.72);
  font-size: 0.8rem;
  font-weight: 600;
}

.import-review-reason {
  color: rgba(var(--v-theme-on-surface), 0.86);
}

.import-review-note {
  color: rgba(var(--v-theme-on-surface), 0.72);
  word-break: break-word;
}

.min-w-0 {
  min-width: 0;
}

.import-review-list-enter-active,
.import-review-list-leave-active,
.import-review-list-move {
  transition: all 260ms cubic-bezier(0.2, 0.7, 0.2, 1);
}

.import-review-list-enter-from,
.import-review-list-leave-to {
  opacity: 0;
  transform: translateY(12px);
}

@media (max-width: 680px) {
  .import-review-row {
    flex-direction: column;
    align-items: stretch;
  }

  .import-review-row__actions {
    width: 100%;
  }
}
</style>
