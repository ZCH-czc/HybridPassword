<script setup>
import { computed } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  loading: {
    type: Boolean,
    default: false,
  },
  sourceLabel: {
    type: String,
    default: "",
  },
  localPreview: {
    type: Object,
    default: () => ({
      totalCount: 0,
      deletedCount: 0,
      latestItem: null,
    }),
  },
  remotePreview: {
    type: Object,
    default: () => ({
      totalCount: 0,
      deletedCount: 0,
      latestItem: null,
    }),
  },
  localOnlyItems: {
    type: Array,
    default: () => [],
  },
  remoteOnlyItems: {
    type: Array,
    default: () => [],
  },
  conflicts: {
    type: Array,
    default: () => [],
  },
});

const emit = defineEmits(["update:modelValue", "toggle-item", "set-conflict", "confirm"]);
const { t, formatDateTime } = useAppPreferences();

const totalPendingChanges = computed(
  () => props.localOnlyItems.length + props.remoteOnlyItems.length + props.conflicts.length
);

function formatPreview(preview) {
  if (!preview?.latestItem) {
    return t("common.none");
  }

  const siteName = preview.latestItem.siteName || t("common.unnamedEntry");
  return `${siteName} / ${preview.latestItem.username}`;
}

function formatTime(preview) {
  return preview?.latestItem?.createdAt
    ? formatDateTime(preview.latestItem.createdAt)
    : t("common.none");
}

function formatRecordTitle(detail) {
  return detail?.siteName || t("common.unnamedEntry");
}

function formatNotes(detail) {
  return Array.isArray(detail?.notes) && detail.notes.length ? detail.notes : [];
}

function emitToggle(groupId, selected) {
  emit("toggle-item", { groupId, selected });
}

function emitResolution(groupId, resolution) {
  emit("set-conflict", { groupId, resolution });
}
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="1120"
    persistent
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="border-sm incremental-sync-card">
      <v-card-title class="pt-6 px-6 d-flex align-center justify-space-between ga-3 flex-wrap">
        <div>
          <div class="text-h5">{{ t("syncIncremental.title") }}</div>
          <div class="text-body-2 text-medium-emphasis mt-1">
            {{ t("syncIncremental.description", { source: sourceLabel }) }}
          </div>
        </div>
        <v-chip variant="tonal" color="primary">
          {{ t("syncIncremental.pendingChanges", { count: totalPendingChanges }) }}
        </v-chip>
      </v-card-title>

      <v-card-text class="px-6 pb-2">
        <v-sheet class="rounded-xl px-4 py-4 incremental-sync-intro">
          <div class="text-body-2">
            {{ t("syncIncremental.body") }}
          </div>
        </v-sheet>

        <v-row dense class="mt-4">
          <v-col cols="12" md="6">
            <v-sheet class="rounded-xl pa-4 incremental-preview-card">
              <div class="text-subtitle-1 font-weight-medium">{{ t("common.currentDevice") }}</div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                {{ t("common.countItems", { count: localPreview.totalCount }) }},
                {{ t("list.statusDeleted", { count: localPreview.deletedCount }) }}
              </div>
              <div class="text-body-1 mt-4">{{ formatPreview(localPreview) }}</div>
              <div class="text-caption text-medium-emphasis mt-2">
                {{ t("syncConfirm.addedAt", { time: formatTime(localPreview) }) }}
              </div>
            </v-sheet>
          </v-col>

          <v-col cols="12" md="6">
            <v-sheet class="rounded-xl pa-4 incremental-preview-card">
              <div class="text-subtitle-1 font-weight-medium">{{ sourceLabel }}</div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                {{ t("common.countItems", { count: remotePreview.totalCount }) }},
                {{ t("list.statusDeleted", { count: remotePreview.deletedCount }) }}
              </div>
              <div class="text-body-1 mt-4">{{ formatPreview(remotePreview) }}</div>
              <div class="text-caption text-medium-emphasis mt-2">
                {{ t("syncConfirm.addedAt", { time: formatTime(remotePreview) }) }}
              </div>
            </v-sheet>
          </v-col>
        </v-row>

        <div class="d-flex flex-column ga-4 mt-4">
          <v-sheet
            v-if="remoteOnlyItems.length"
            class="rounded-xl pa-4 incremental-section"
          >
            <div class="vault-section-heading">{{ t("syncIncremental.remoteOnlyTitle") }}</div>
            <div class="vault-section-subtitle mt-1">
              {{ t("syncIncremental.remoteOnlyBody") }}
            </div>

            <div class="d-flex flex-column ga-3 mt-4">
              <v-sheet
                v-for="item in remoteOnlyItems"
                :key="item.id"
                class="rounded-xl pa-4 incremental-item"
              >
                <div class="d-flex flex-column flex-lg-row justify-space-between ga-4">
                  <div class="min-w-0">
                    <div class="text-subtitle-1 font-weight-medium">
                      {{ formatRecordTitle(item.remote.detail) }}
                    </div>
                    <div class="text-body-2 text-medium-emphasis mt-1">
                      {{ item.remote.detail.username }}
                    </div>
                    <v-chip
                      size="small"
                      variant="tonal"
                      class="mt-2"
                      :color="item.remote.detail.status === 'deleted' ? 'warning' : 'primary'"
                    >
                      {{
                        item.remote.detail.status === "deleted"
                          ? t("syncIncremental.statusDeleted")
                          : t("syncIncremental.statusActive")
                      }}
                    </v-chip>
                    <div class="text-caption text-medium-emphasis mt-2">
                      {{ t("item.updatedAt", { time: formatDateTime(item.remote.detail.updatedAt) }) }}
                    </div>
                    <div class="text-caption text-medium-emphasis mt-1">
                      {{ t("common.password") }}
                    </div>
                    <div class="incremental-password mt-1">
                      {{ item.remote.detail.password || t("common.none") }}
                    </div>
                    <div v-if="formatNotes(item.remote.detail).length" class="mt-3">
                      <div class="text-caption text-medium-emphasis">{{ t("common.notes") }}</div>
                      <div class="d-flex flex-column ga-2 mt-2">
                        <div
                          v-for="(note, index) in formatNotes(item.remote.detail)"
                          :key="`${item.id}-remote-note-${index}`"
                          class="incremental-note"
                        >
                          {{ note }}
                        </div>
                      </div>
                    </div>
                  </div>

                  <v-btn-toggle
                    :model-value="item.selected ? 'include' : 'skip'"
                    mandatory
                    class="incremental-choice-shell"
                    @update:model-value="emitToggle(item.id, $event === 'include')"
                  >
                    <v-btn value="include">{{ t("syncIncremental.includeRemote") }}</v-btn>
                    <v-btn value="skip">{{ t("syncIncremental.skipRemote") }}</v-btn>
                  </v-btn-toggle>
                </div>
              </v-sheet>
            </div>
          </v-sheet>

          <v-sheet
            v-if="localOnlyItems.length"
            class="rounded-xl pa-4 incremental-section"
          >
            <div class="vault-section-heading">{{ t("syncIncremental.localOnlyTitle") }}</div>
            <div class="vault-section-subtitle mt-1">
              {{ t("syncIncremental.localOnlyBody") }}
            </div>

            <div class="d-flex flex-column ga-3 mt-4">
              <v-sheet
                v-for="item in localOnlyItems"
                :key="item.id"
                class="rounded-xl pa-4 incremental-item"
              >
                <div class="d-flex flex-column flex-lg-row justify-space-between ga-4">
                  <div class="min-w-0">
                    <div class="text-subtitle-1 font-weight-medium">
                      {{ formatRecordTitle(item.local.detail) }}
                    </div>
                    <div class="text-body-2 text-medium-emphasis mt-1">
                      {{ item.local.detail.username }}
                    </div>
                    <v-chip
                      size="small"
                      variant="tonal"
                      class="mt-2"
                      :color="item.local.detail.status === 'deleted' ? 'warning' : 'primary'"
                    >
                      {{
                        item.local.detail.status === "deleted"
                          ? t("syncIncremental.statusDeleted")
                          : t("syncIncremental.statusActive")
                      }}
                    </v-chip>
                    <div class="text-caption text-medium-emphasis mt-2">
                      {{ t("item.updatedAt", { time: formatDateTime(item.local.detail.updatedAt) }) }}
                    </div>
                    <div class="text-caption text-medium-emphasis mt-1">
                      {{ t("common.password") }}
                    </div>
                    <div class="incremental-password mt-1">
                      {{ item.local.detail.password || t("common.none") }}
                    </div>
                    <div v-if="formatNotes(item.local.detail).length" class="mt-3">
                      <div class="text-caption text-medium-emphasis">{{ t("common.notes") }}</div>
                      <div class="d-flex flex-column ga-2 mt-2">
                        <div
                          v-for="(note, index) in formatNotes(item.local.detail)"
                          :key="`${item.id}-local-note-${index}`"
                          class="incremental-note"
                        >
                          {{ note }}
                        </div>
                      </div>
                    </div>
                  </div>

                  <v-btn-toggle
                    :model-value="item.selected ? 'keep' : 'skip'"
                    mandatory
                    class="incremental-choice-shell"
                    @update:model-value="emitToggle(item.id, $event === 'keep')"
                  >
                    <v-btn value="keep">{{ t("syncIncremental.keepLocal") }}</v-btn>
                    <v-btn value="skip">{{ t("syncIncremental.skipLocal") }}</v-btn>
                  </v-btn-toggle>
                </div>
              </v-sheet>
            </div>
          </v-sheet>

          <v-sheet
            v-if="conflicts.length"
            class="rounded-xl pa-4 incremental-section"
          >
            <div class="vault-section-heading">{{ t("syncIncremental.conflictTitle") }}</div>
            <div class="vault-section-subtitle mt-1">
              {{ t("syncIncremental.conflictBody") }}
            </div>

            <div class="d-flex flex-column ga-4 mt-4">
              <v-sheet
                v-for="item in conflicts"
                :key="item.id"
                class="rounded-xl pa-4 incremental-item"
              >
                <div class="d-flex flex-column ga-4">
                  <v-btn-toggle
                    :model-value="item.resolution"
                    mandatory
                    class="incremental-choice-shell align-self-start"
                    @update:model-value="emitResolution(item.id, $event)"
                  >
                    <v-btn value="local">{{ t("syncIncremental.chooseLocal") }}</v-btn>
                    <v-btn value="remote">{{ t("syncIncremental.chooseRemote") }}</v-btn>
                  </v-btn-toggle>

                  <v-row dense>
                    <v-col cols="12" md="6">
                      <v-sheet
                        class="rounded-xl pa-4 incremental-compare-card"
                        :class="{ 'incremental-compare-card--active': item.resolution === 'local' }"
                      >
                        <div class="d-flex align-center justify-space-between ga-3">
                          <div class="text-subtitle-1 font-weight-medium">
                            {{ t("common.currentDevice") }}
                          </div>
                          <v-chip
                            v-if="item.recommendedResolution === 'local'"
                            size="small"
                            variant="tonal"
                            color="primary"
                          >
                            {{ t("syncIncremental.recommended") }}
                          </v-chip>
                        </div>

                        <template v-if="item.local?.detail">
                          <div class="text-body-1 mt-3">
                            {{ formatRecordTitle(item.local.detail) }}
                          </div>
                          <div class="text-body-2 text-medium-emphasis mt-1">
                            {{ item.local.detail.username }}
                          </div>
                          <v-chip
                            size="small"
                            variant="tonal"
                            class="mt-2"
                            :color="item.local.detail.status === 'deleted' ? 'warning' : 'primary'"
                          >
                            {{
                              item.local.detail.status === "deleted"
                                ? t("syncIncremental.statusDeleted")
                                : t("syncIncremental.statusActive")
                            }}
                          </v-chip>
                          <div class="text-caption text-medium-emphasis mt-2">
                            {{ t("item.updatedAt", { time: formatDateTime(item.local.detail.updatedAt) }) }}
                          </div>
                          <div class="text-caption text-medium-emphasis mt-3">
                            {{ t("common.password") }}
                          </div>
                          <div class="incremental-password mt-1">
                            {{ item.local.detail.password || t("common.none") }}
                          </div>
                          <div v-if="formatNotes(item.local.detail).length" class="mt-3">
                            <div class="text-caption text-medium-emphasis">{{ t("common.notes") }}</div>
                            <div class="d-flex flex-column ga-2 mt-2">
                              <div
                                v-for="(note, index) in formatNotes(item.local.detail)"
                                :key="`${item.id}-conflict-local-note-${index}`"
                                class="incremental-note"
                              >
                                {{ note }}
                              </div>
                            </div>
                          </div>
                        </template>
                        <div v-else class="text-body-2 text-medium-emphasis mt-3">
                          {{ t("common.none") }}
                        </div>
                      </v-sheet>
                    </v-col>

                    <v-col cols="12" md="6">
                      <v-sheet
                        class="rounded-xl pa-4 incremental-compare-card"
                        :class="{ 'incremental-compare-card--active': item.resolution === 'remote' }"
                      >
                        <div class="d-flex align-center justify-space-between ga-3">
                          <div class="text-subtitle-1 font-weight-medium">
                            {{ sourceLabel }}
                          </div>
                          <v-chip
                            v-if="item.recommendedResolution === 'remote'"
                            size="small"
                            variant="tonal"
                            color="primary"
                          >
                            {{ t("syncIncremental.recommended") }}
                          </v-chip>
                        </div>

                        <template v-if="item.remote?.detail">
                          <div class="text-body-1 mt-3">
                            {{ formatRecordTitle(item.remote.detail) }}
                          </div>
                          <div class="text-body-2 text-medium-emphasis mt-1">
                            {{ item.remote.detail.username }}
                          </div>
                          <v-chip
                            size="small"
                            variant="tonal"
                            class="mt-2"
                            :color="item.remote.detail.status === 'deleted' ? 'warning' : 'primary'"
                          >
                            {{
                              item.remote.detail.status === "deleted"
                                ? t("syncIncremental.statusDeleted")
                                : t("syncIncremental.statusActive")
                            }}
                          </v-chip>
                          <div class="text-caption text-medium-emphasis mt-2">
                            {{ t("item.updatedAt", { time: formatDateTime(item.remote.detail.updatedAt) }) }}
                          </div>
                          <div class="text-caption text-medium-emphasis mt-3">
                            {{ t("common.password") }}
                          </div>
                          <div class="incremental-password mt-1">
                            {{ item.remote.detail.password || t("common.none") }}
                          </div>
                          <div v-if="formatNotes(item.remote.detail).length" class="mt-3">
                            <div class="text-caption text-medium-emphasis">{{ t("common.notes") }}</div>
                            <div class="d-flex flex-column ga-2 mt-2">
                              <div
                                v-for="(note, index) in formatNotes(item.remote.detail)"
                                :key="`${item.id}-conflict-remote-note-${index}`"
                                class="incremental-note"
                              >
                                {{ note }}
                              </div>
                            </div>
                          </div>
                        </template>
                        <div v-else class="text-body-2 text-medium-emphasis mt-3">
                          {{ t("common.none") }}
                        </div>
                      </v-sheet>
                    </v-col>
                  </v-row>
                </div>
              </v-sheet>
            </div>
          </v-sheet>

          <v-sheet
            v-if="!totalPendingChanges"
            class="rounded-xl pa-4 incremental-section incremental-section--empty text-center"
          >
            <div class="vault-section-heading">{{ t("syncIncremental.noChangesTitle") }}</div>
            <div class="vault-section-subtitle mt-2">
              {{ t("syncIncremental.noChangesBody") }}
            </div>
          </v-sheet>
        </div>
      </v-card-text>

      <v-card-actions class="px-6 pb-6 pt-4 justify-end ga-2">
        <v-btn variant="text" :disabled="loading" @click="emit('update:modelValue', false)">
          {{ t("common.cancel") }}
        </v-btn>
        <v-btn color="primary" :loading="loading" @click="emit('confirm')">
          {{ t("syncIncremental.apply") }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.incremental-sync-card {
  background: var(--vault-panel-bg);
  box-shadow: var(--vault-shadow);
}

.incremental-sync-intro {
  background: var(--vault-block-bg-subtle);
}

.incremental-preview-card,
.incremental-section,
.incremental-item,
.incremental-compare-card {
  background: var(--vault-block-bg);
}

.incremental-section--empty {
  background: var(--vault-block-bg-subtle);
}

.incremental-choice-shell {
  background: var(--vault-block-bg-subtle);
  padding: 4px;
}

.incremental-password {
  padding: 12px 14px;
  border-radius: 16px;
  background: var(--vault-block-bg-subtle);
  font-family: "Cascadia Code", "Consolas", monospace;
  word-break: break-all;
}

.incremental-note {
  padding: 10px 12px;
  border-radius: 14px;
  background: var(--vault-block-bg-subtle);
  color: rgba(var(--v-theme-on-surface), 0.8);
}

.incremental-compare-card--active {
  box-shadow: 0 0 0 1px rgba(var(--v-theme-primary), 0.18);
}

.min-w-0 {
  min-width: 0;
}
</style>
