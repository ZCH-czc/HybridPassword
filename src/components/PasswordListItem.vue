<script setup>
import { computed, ref } from "vue";
import InlineSvgIcon from "@/components/InlineSvgIcon.vue";
import { useAppPreferences } from "@/composables/useAppPreferences";
import { maskPassword } from "@/utils/password-generator";

const props = defineProps({
  item: {
    type: Object,
    required: true,
  },
  revealedPassword: {
    type: String,
    default: "",
  },
  revealLoading: {
    type: Boolean,
    default: false,
  },
  editLoading: {
    type: Boolean,
    default: false,
  },
  favoriteLoading: {
    type: Boolean,
    default: false,
  },
  selectionMode: {
    type: Boolean,
    default: false,
  },
  selected: {
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
]);

const notesExpanded = ref(false);
const { t, formatDateTime } = useAppPreferences();

const displayTitle = computed(() => props.item.siteName || t("common.unnamedEntry"));
const avatarLabel = computed(() => (displayTitle.value?.slice(0, 1) || "P").toUpperCase());
const displayPassword = computed(() =>
  props.revealedPassword ? props.revealedPassword : maskPassword("placeholder")
);
const notesLabel = computed(() =>
  props.item.notes.length ? t("common.countNotes", { count: props.item.notes.length }) : t("item.noNotes")
);
</script>

<template>
  <v-card
    class="border-sm mb-3 vault-list-card"
    :class="{ 'vault-list-card--selected': selected }"
  >
    <v-card-text class="pa-4">
      <div class="d-flex align-start justify-space-between ga-3 flex-wrap">
        <div class="d-flex ga-3 min-w-0 flex-grow-1">
          <v-checkbox-btn
            v-if="selectionMode"
            :model-value="selected"
            color="primary"
            class="mt-1"
            @update:model-value="emit('toggle-select', item.id)"
          />

          <v-avatar color="secondary" variant="tonal" size="42">
            {{ avatarLabel }}
          </v-avatar>

          <div class="min-w-0">
            <div class="d-flex align-center flex-wrap ga-2">
              <div class="text-subtitle-1 font-weight-medium text-truncate">
                {{ displayTitle }}
              </div>
              <v-chip
                v-if="item.isFavorite"
                size="x-small"
                color="warning"
                variant="tonal"
              >
                {{ t("item.favorite") }}
              </v-chip>
            </div>
            <div class="text-body-2 text-medium-emphasis text-truncate">
              {{ item.username }}
            </div>
            <div class="text-caption text-medium-emphasis mt-1">
              {{ t("item.updatedAt", { time: formatDateTime(item.updatedAt) }) }}
            </div>
          </div>
        </div>

        <div v-if="!selectionMode" class="d-flex ga-1">
          <button
            type="button"
            class="vault-icon-action"
            :class="{ 'vault-icon-action--warning': item.isFavorite }"
            :disabled="favoriteLoading"
            @click="emit('toggle-favorite', item.id)"
          >
            <v-progress-circular
              v-if="favoriteLoading"
              indeterminate
              size="16"
              width="2"
              color="warning"
            />
            <InlineSvgIcon
              v-else
              :icon="item.isFavorite ? 'mdi-star' : 'mdi-star-outline'"
              :size="20"
            />
          </button>
          <button
            type="button"
            class="vault-icon-action"
            :disabled="editLoading"
            @click="emit('edit', item.id)"
          >
            <v-progress-circular
              v-if="editLoading"
              indeterminate
              size="16"
              width="2"
              color="primary"
            />
            <InlineSvgIcon v-else icon="mdi-pencil-outline" :size="20" />
          </button>
          <button
            type="button"
            class="vault-icon-action vault-icon-action--danger"
            @click="emit('delete', item.id)"
          >
            <InlineSvgIcon icon="mdi-delete-outline" :size="20" />
          </button>
        </div>
      </div>

      <v-sheet class="mt-4 pa-3 rounded-lg border-sm bg-surface-variant vault-inner-sheet">
        <div class="text-caption text-medium-emphasis mb-1">{{ t("item.password") }}</div>
        <div class="password-line text-body-2">
          {{ displayPassword }}
        </div>

        <div class="d-flex flex-wrap ga-2 mt-3">
          <v-btn
            size="small"
            variant="text"
            :loading="revealLoading"
            @click="emit('toggle-reveal', item.id)"
          >
            {{ revealedPassword ? t("item.hidePlain") : t("item.showPlain") }}
          </v-btn>

          <v-btn size="small" variant="text" @click="emit('copy-password', item.id)">
            {{ t("item.copyPassword") }}
          </v-btn>

          <v-btn size="small" variant="text" @click="emit('copy-username', item.username)">
            {{ t("item.copyUsername") }}
          </v-btn>
        </div>
      </v-sheet>

      <div class="d-flex align-center justify-space-between mt-3">
        <div class="text-body-2 text-medium-emphasis">
          {{ notesLabel }}
        </div>

        <v-btn
          variant="text"
          size="small"
          :disabled="!item.notes.length"
          @click="notesExpanded = !notesExpanded"
        >
          {{ notesExpanded ? t("item.collapseNotes") : t("item.expandNotes") }}
        </v-btn>
      </div>

      <v-expand-transition>
        <div v-show="notesExpanded && item.notes.length" class="mt-2">
          <v-list density="compact" class="bg-transparent pa-0">
            <v-list-item
              v-for="(note, index) in item.notes"
              :key="`${item.id}-${index}`"
              class="px-0"
            >
              <template #prepend>
                <v-icon size="18">mdi-note-text-outline</v-icon>
              </template>
              <v-list-item-title class="text-body-2 text-wrap">
                {{ note }}
              </v-list-item-title>
            </v-list-item>
          </v-list>
        </div>
      </v-expand-transition>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.min-w-0 {
  min-width: 0;
}

.password-line {
  word-break: break-all;
  font-family: "Cascadia Code", "Consolas", monospace;
}

.vault-list-card,
.vault-inner-sheet {
  position: relative;
  overflow: hidden;
  transition:
    transform 260ms cubic-bezier(0.22, 1, 0.36, 1),
    box-shadow 260ms cubic-bezier(0.22, 1, 0.36, 1),
    background-color 220ms ease,
    border-color 220ms ease;
}

.vault-list-card::before,
.vault-inner-sheet::before {
  content: "";
  position: absolute;
  inset: auto -12% 44% 24%;
  height: 42%;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.24), transparent);
  transform: translateX(-44%) skewX(-20deg);
  filter: blur(10px);
  opacity: 0;
  animation: vault-sheen 14s ease-in-out infinite;
  pointer-events: none;
}

.vault-inner-sheet {
  background: var(--vault-block-bg);
  box-shadow: none;
}

.vault-list-card:hover {
  transform: translateY(-4px) scale(1.003);
  box-shadow: none;
}

.vault-list-card--selected {
  box-shadow: 0 0 0 2px rgba(var(--v-theme-primary), 0.22), var(--vault-shadow-soft);
}

.vault-icon-action {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  height: 34px;
  padding: 0;
  border: none;
  border-radius: 999px;
  background: transparent;
  box-shadow: none;
  color: rgba(var(--v-theme-on-surface), 0.72);
  cursor: pointer;
  transition:
    background-color 180ms ease,
    color 180ms ease,
    transform 180ms ease;
}

.vault-icon-action:hover {
  background: rgba(var(--v-theme-on-surface), 0.06);
  color: rgba(var(--v-theme-on-surface), 0.96);
}

.vault-icon-action:focus-visible {
  outline: none;
  background: rgba(var(--v-theme-on-surface), 0.08);
}

.vault-icon-action:disabled {
  opacity: 0.56;
  cursor: default;
}

.vault-icon-action--danger {
  color: rgb(var(--v-theme-error));
}

.vault-icon-action--warning {
  color: rgb(var(--v-theme-warning));
}

:global(.v-theme--dark) .vault-icon-action:hover {
  background: rgba(255, 255, 255, 0.05);
}
</style>
