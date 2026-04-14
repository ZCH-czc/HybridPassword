<script setup>
import { computed } from "vue";
import InlineSvgIcon from "@/components/InlineSvgIcon.vue";
import { useAppPreferences } from "@/composables/useAppPreferences";
import PasswordGeneratorCard from "@/components/PasswordGeneratorCard.vue";
import PasswordSummaryBar from "@/components/PasswordSummaryBar.vue";

const props = defineProps({
  summary: {
    type: Object,
    required: true,
  },
  recentItems: {
    type: Array,
    default: () => [],
  },
  favoriteItems: {
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
  favoriteIds: {
    type: Object,
    default: () => ({}),
  },
  searchText: {
    type: String,
    default: "",
  },
});

const emit = defineEmits([
  "open-list",
  "toggle-reveal",
  "toggle-favorite",
  "copy-password",
  "copy-username",
  "edit",
  "apply-generated",
  "copy-generated",
]);

const { t, formatDate } = useAppPreferences();

const sectionTitle = computed(() =>
  props.searchText ? t("home.sectionSearchPreview") : t("home.sectionRecent")
);

function previewPassword(recordId) {
  return props.revealedPasswords[recordId] || "************";
}
</script>

<template>
  <div class="d-flex flex-column ga-4">
    <v-card class="hero-panel border-sm overflow-hidden">
      <v-card-text class="pa-6 pa-sm-7">
        <div class="d-flex flex-column flex-lg-row align-lg-center justify-space-between ga-6">
          <div class="hero-copy">
            <div class="text-overline text-primary">{{ t("home.overline") }}</div>
            <div class="text-h4 font-weight-medium mt-2">{{ t("home.title") }}</div>
            <div class="text-body-1 text-medium-emphasis mt-3">
              {{ t("home.subtitle") }}
            </div>

            <div class="hero-metrics d-flex flex-wrap ga-2 mt-4">
              <div class="hero-stat-pill hero-stat-pill--primary">
                {{ t("home.savedCount", { count: summary.total }) }}
              </div>
              <div class="hero-stat-pill hero-stat-pill--secondary">
                {{ t("home.notesCount", { count: summary.notes }) }}
              </div>
              <div class="hero-stat-pill">
                {{ t("home.filteredCount", { count: summary.filtered }) }}
              </div>
            </div>

            <div class="d-flex flex-wrap ga-3 mt-6">
              <v-btn
                variant="flat"
                prepend-icon="mdi-format-list-bulleted"
                class="hero-open-button"
                @click="emit('open-list')"
              >
                {{ t("home.viewAll") }}
              </v-btn>
            </div>
          </div>

          <v-sheet class="hero-side pa-4 pa-sm-5 rounded-xl">
            <div class="text-body-2 text-medium-emphasis">{{ t("home.securityStatus") }}</div>
            <div class="text-h5 font-weight-medium mt-2">{{ t("home.unlockedTitle") }}</div>
            <div class="text-body-2 text-medium-emphasis mt-2">
              {{ t("home.unlockedBody") }}
            </div>

            <div class="hero-side-row d-flex align-center ga-3 mt-5">
              <v-avatar color="secondary" variant="tonal">
                <v-icon>mdi-shield-check-outline</v-icon>
              </v-avatar>
              <div>
                <div class="text-subtitle-2 font-weight-medium">{{ t("home.masterProtected") }}</div>
                <div class="text-body-2 text-medium-emphasis">{{ t("home.masterProtectedBody") }}</div>
              </div>
            </div>
          </v-sheet>
        </div>
      </v-card-text>
    </v-card>

    <PasswordSummaryBar
      :total="summary.total"
      :filtered="summary.filtered"
      :notes="summary.notes"
    />

    <v-card class="border-sm home-section-card">
      <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-2 py-5 px-5">
        <span>{{ t("home.favorites") }}</span>
        <v-chip color="warning" variant="tonal">{{ t("common.countItems", { count: favoriteItems.length }) }}</v-chip>
      </v-card-title>
      <v-card-text class="px-5 pb-5 pt-0">
        <div v-if="!favoriteItems.length" class="py-8 text-center">
          <v-icon size="40" color="medium-emphasis">mdi-star-outline</v-icon>
          <div class="text-subtitle-1 mt-3">{{ t("home.noFavorites") }}</div>
          <div class="text-body-2 text-medium-emphasis mt-2">
            {{ t("home.noFavoritesBody") }}
          </div>
        </div>

        <div v-else class="d-flex flex-wrap ga-3">
          <v-sheet
            v-for="item in favoriteItems"
            :key="item.id"
            class="favorite-tile pa-4 rounded-xl border-sm"
          >
            <div class="d-flex align-center justify-space-between ga-2">
              <div class="d-flex align-center ga-3 min-w-0">
                <v-avatar color="warning" variant="tonal" size="38">
                  {{ (item.siteName || item.username || "P").slice(0, 1).toUpperCase() }}
                </v-avatar>
                <div class="min-w-0">
                  <div class="text-subtitle-2 font-weight-medium text-truncate">
                    {{ item.siteName || t("common.unnamedEntry") }}
                  </div>
                  <div class="text-body-2 text-medium-emphasis text-truncate">
                    {{ item.username }}
                  </div>
                </div>
              </div>

              <button
                type="button"
                class="home-icon-action home-icon-action--warning"
                :disabled="Boolean(favoriteIds[item.id])"
                @click="emit('toggle-favorite', item.id)"
              >
                <v-progress-circular
                  v-if="Boolean(favoriteIds[item.id])"
                  indeterminate
                  size="16"
                  width="2"
                  color="warning"
                />
                <InlineSvgIcon v-else icon="mdi-star" :size="20" />
              </button>
            </div>

            <div class="d-flex flex-wrap ga-2 mt-4">
              <v-btn size="small" variant="text" @click="emit('edit', item.id)">{{ t("common.edit") }}</v-btn>
              <v-btn size="small" variant="text" @click="emit('copy-password', item.id)">
                {{ t("home.copyPassword") }}
              </v-btn>
            </div>
          </v-sheet>
        </div>
      </v-card-text>
    </v-card>

    <v-row dense>
      <v-col cols="12" lg="7">
        <v-card class="border-sm h-100 home-section-card">
          <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-2 py-5 px-5">
            <span>{{ sectionTitle }}</span>
            <v-btn variant="text" size="small" @click="emit('open-list')">{{ t("home.openList") }}</v-btn>
          </v-card-title>

          <v-card-text class="px-5 pb-5 pt-0">
            <div v-if="!recentItems.length" class="py-10 text-center">
              <v-icon size="46" color="medium-emphasis">mdi-folder-key-network-outline</v-icon>
              <div class="text-h6 mt-3">{{ t("home.noItems") }}</div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                {{ t("home.noItemsBody") }}
              </div>
            </div>

            <TransitionGroup v-else name="vault-list" tag="div" class="d-flex flex-column ga-3">
              <v-sheet
                v-for="item in recentItems"
                :key="item.id"
                class="pa-4 rounded-xl border-sm bg-surface recent-tile"
              >
                <div class="d-flex align-start justify-space-between ga-3">
                  <div class="d-flex align-center ga-3 min-w-0">
                    <v-avatar color="secondary" variant="tonal" size="42">
                      {{ (item.siteName || item.username || "P").slice(0, 1).toUpperCase() }}
                    </v-avatar>

                    <div class="min-w-0">
                      <div class="d-flex align-center flex-wrap ga-2">
                        <div class="text-subtitle-1 font-weight-medium text-truncate">
                          {{ item.siteName || t("common.unnamedEntry") }}
                        </div>
                        <InlineSvgIcon
                          v-if="item.isFavorite"
                          icon="mdi-star"
                          :size="18"
                          class="text-warning"
                        />
                      </div>
                      <div class="text-body-2 text-medium-emphasis text-truncate">
                        {{ item.username }}
                      </div>
                    </div>
                  </div>

                  <div class="d-flex ga-1">
                    <button
                      type="button"
                      class="home-icon-action"
                      :class="{ 'home-icon-action--warning': item.isFavorite }"
                      :disabled="Boolean(favoriteIds[item.id])"
                      @click="emit('toggle-favorite', item.id)"
                    >
                      <v-progress-circular
                        v-if="Boolean(favoriteIds[item.id])"
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
                      class="home-icon-action"
                      @click="emit('edit', item.id)"
                    >
                      <InlineSvgIcon icon="mdi-pencil-outline" :size="20" />
                    </button>
                    <button
                      type="button"
                      class="home-icon-action"
                      :disabled="Boolean(revealingIds[item.id])"
                      @click="emit('toggle-reveal', item.id)"
                    >
                      <v-progress-circular
                        v-if="Boolean(revealingIds[item.id])"
                        indeterminate
                        size="16"
                        width="2"
                        color="primary"
                      />
                      <InlineSvgIcon
                        v-else
                        :icon="revealedPasswords[item.id] ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
                        :size="20"
                      />
                    </button>
                  </div>
                </div>

                <v-sheet class="mt-3 pa-3 rounded-lg bg-surface-variant">
                  <div class="text-caption text-medium-emphasis mb-1">{{ t("home.passwordPreview") }}</div>
                  <div class="password-preview text-body-2">
                    {{ previewPassword(item.id) }}
                  </div>
                </v-sheet>

                <div class="d-flex flex-wrap align-center justify-space-between ga-3 mt-3">
                  <div class="d-flex flex-wrap ga-2">
                    <v-chip size="small" variant="tonal" color="secondary">
                      {{ t("common.countNotes", { count: item.notes.length }) }}
                    </v-chip>
                    <v-chip size="small" variant="flat">
                      {{ formatDate(item.updatedAt) }}
                    </v-chip>
                  </div>

                  <div class="d-flex flex-wrap ga-2">
                    <v-btn size="small" variant="text" @click="emit('copy-username', item.username)">
                      {{ t("home.copyUsername") }}
                    </v-btn>
                    <v-btn size="small" variant="text" @click="emit('copy-password', item.id)">
                      {{ t("home.copyPassword") }}
                    </v-btn>
                  </div>
                </div>
              </v-sheet>
            </TransitionGroup>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" lg="5">
        <PasswordGeneratorCard
          class="h-100"
          @apply="emit('apply-generated', $event)"
          @copy="emit('copy-generated', $event)"
        />
      </v-col>
    </v-row>
  </div>
</template>

<style scoped>
.hero-panel {
  position: relative;
  isolation: isolate;
  background: var(--vault-panel-bg);
}

.hero-panel::before,
.hero-panel::after {
  content: "";
  position: absolute;
  pointer-events: none;
}

.hero-panel::before {
  inset: -10% auto auto -8%;
  width: 42%;
  aspect-ratio: 1;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(var(--v-theme-primary), 0.12), transparent 60%);
  filter: blur(18px);
  opacity: 0.58;
  animation: vault-ambient-drift 22s ease-in-out infinite;
}

.hero-panel::after {
  inset: auto -8% -36% auto;
  width: 44%;
  aspect-ratio: 1;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(var(--v-theme-secondary), 0.12), transparent 60%);
  filter: blur(22px);
  opacity: 0.34;
}

.hero-copy {
  position: relative;
  z-index: 1;
  max-width: 720px;
}

.hero-metrics {
  gap: 10px;
}

.hero-stat-pill {
  display: inline-flex;
  align-items: center;
  min-height: 36px;
  padding: 0 16px;
  border-radius: 999px;
  background: var(--vault-block-bg);
  color: rgba(var(--v-theme-on-surface), 0.8);
  font-size: 0.88rem;
  font-weight: 600;
}

.hero-stat-pill--primary {
  color: rgb(var(--v-theme-primary));
}

.hero-stat-pill--secondary {
  color: rgb(var(--v-theme-secondary));
}

.hero-open-button {
  background: var(--vault-block-bg) !important;
  box-shadow: none !important;
}

.hero-side {
  position: relative;
  z-index: 1;
  min-width: min(100%, 320px);
  background: var(--vault-block-bg-subtle);
  box-shadow: none;
}

.hero-side-row {
  padding: 14px 16px;
  border-radius: calc(var(--vault-radius) - 2px);
  background: var(--vault-block-bg);
}

.home-section-card {
  background: var(--vault-panel-bg);
}

.min-w-0 {
  min-width: 0;
}

.favorite-tile,
.recent-tile {
  position: relative;
  overflow: hidden;
  transition:
    transform 260ms cubic-bezier(0.22, 1, 0.36, 1),
    box-shadow 260ms cubic-bezier(0.22, 1, 0.36, 1),
    background-color 240ms ease;
}

.favorite-tile {
  width: min(100%, 280px);
  background: var(--vault-panel-bg);
  box-shadow: none;
}

.favorite-tile::before,
.recent-tile::before {
  content: "";
  position: absolute;
  inset: auto -12% 42% 24%;
  height: 44%;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.12), transparent);
  transform: translateX(-44%) skewX(-20deg);
  filter: blur(10px);
  opacity: 0;
  animation: vault-sheen 18s ease-in-out infinite;
  pointer-events: none;
}

.recent-tile {
  background: var(--vault-panel-bg);
  box-shadow: none;
}

.favorite-tile:hover,
.recent-tile:hover {
  transform: translateY(-2px) scale(1.003);
  box-shadow: none;
}

.password-preview {
  font-family: "Cascadia Code", "Consolas", monospace;
  word-break: break-all;
}

.home-icon-action {
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
    color 180ms ease;
}

.home-icon-action:hover {
  background: rgba(var(--v-theme-on-surface), 0.06);
  color: rgba(var(--v-theme-on-surface), 0.96);
}

.home-icon-action:focus-visible {
  outline: none;
  background: rgba(var(--v-theme-on-surface), 0.08);
}

.home-icon-action:disabled {
  opacity: 0.56;
  cursor: default;
}

.home-icon-action--warning {
  color: rgb(var(--v-theme-warning));
}

:global(.v-theme--dark) .home-icon-action:hover {
  background: rgba(255, 255, 255, 0.05);
}

.vault-list-enter-active,
.vault-list-leave-active,
.vault-list-move {
  transition: all 260ms cubic-bezier(0.2, 0.7, 0.2, 1);
}

.vault-list-enter-from,
.vault-list-leave-to {
  opacity: 0;
  transform: translateY(10px);
}
</style>
