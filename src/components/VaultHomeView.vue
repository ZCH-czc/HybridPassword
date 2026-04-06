<script setup>
import { computed } from "vue";
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

            <div class="d-flex flex-wrap ga-2 mt-4">
              <v-chip color="primary" variant="tonal">{{ t("home.savedCount", { count: summary.total }) }}</v-chip>
              <v-chip color="secondary" variant="tonal">{{ t("home.notesCount", { count: summary.notes }) }}</v-chip>
              <v-chip variant="flat">{{ t("home.filteredCount", { count: summary.filtered }) }}</v-chip>
            </div>

            <div class="d-flex flex-wrap ga-3 mt-6">
              <v-btn variant="text" prepend-icon="mdi-format-list-bulleted" @click="emit('open-list')">
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

            <div class="d-flex align-center ga-3 mt-5">
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

    <v-card class="border-sm">
      <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-2">
        <span>{{ t("home.favorites") }}</span>
        <v-chip color="warning" variant="tonal">{{ t("common.countItems", { count: favoriteItems.length }) }}</v-chip>
      </v-card-title>
      <v-card-text class="pa-4">
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

              <v-btn
                icon
                variant="text"
                size="small"
                :loading="Boolean(favoriteIds[item.id])"
                @click="emit('toggle-favorite', item.id)"
              >
                <v-icon color="warning">mdi-star</v-icon>
              </v-btn>
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
        <v-card class="border-sm h-100">
          <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-2">
            <span>{{ sectionTitle }}</span>
            <v-btn variant="text" size="small" @click="emit('open-list')">{{ t("home.openList") }}</v-btn>
          </v-card-title>

          <v-card-text class="pa-4">
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
                        <v-icon v-if="item.isFavorite" color="warning" size="18">
                          mdi-star
                        </v-icon>
                      </div>
                      <div class="text-body-2 text-medium-emphasis text-truncate">
                        {{ item.username }}
                      </div>
                    </div>
                  </div>

                  <div class="d-flex ga-1">
                    <v-btn
                      icon
                      variant="text"
                      size="small"
                      :loading="Boolean(favoriteIds[item.id])"
                      @click="emit('toggle-favorite', item.id)"
                    >
                      <v-icon>{{ item.isFavorite ? "mdi-star" : "mdi-star-outline" }}</v-icon>
                    </v-btn>
                    <v-btn icon variant="text" size="small" @click="emit('edit', item.id)">
                      <v-icon>mdi-pencil-outline</v-icon>
                    </v-btn>
                    <v-btn
                      icon
                      variant="text"
                      size="small"
                      :loading="Boolean(revealingIds[item.id])"
                      @click="emit('toggle-reveal', item.id)"
                    >
                      <v-icon>
                        {{ revealedPasswords[item.id] ? "mdi-eye-off-outline" : "mdi-eye-outline" }}
                      </v-icon>
                    </v-btn>
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
  background:
    radial-gradient(circle at top left, rgba(var(--v-theme-primary), 0.2), transparent 36%),
    radial-gradient(circle at top right, rgba(var(--v-theme-secondary), 0.14), transparent 30%),
    radial-gradient(circle at bottom right, rgba(var(--v-theme-warning), 0.14), transparent 34%),
    linear-gradient(
      135deg,
      rgba(var(--v-theme-surface), 1),
      rgba(var(--v-theme-surface), 0.9)
    );
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
  background: radial-gradient(circle, rgba(255, 255, 255, 0.46), transparent 58%);
  filter: blur(10px);
  opacity: 0.86;
  animation: vault-ambient-drift 16s ease-in-out infinite;
}

.hero-panel::after {
  inset: auto -8% -36% auto;
  width: 44%;
  aspect-ratio: 1;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(var(--v-theme-secondary), 0.2), transparent 58%);
  filter: blur(18px);
  opacity: 0.42;
}

.hero-copy {
  position: relative;
  z-index: 1;
  max-width: 720px;
}

.hero-side {
  position: relative;
  z-index: 1;
  min-width: min(100%, 320px);
  background:
    linear-gradient(
      180deg,
      rgba(var(--v-theme-surface), 0.92),
      rgba(var(--v-theme-surface), 0.76)
    );
  backdrop-filter: blur(14px);
  -webkit-backdrop-filter: blur(14px);
  animation: vault-float-card 10s ease-in-out infinite;
  box-shadow:
    0 16px 30px rgba(22, 32, 46, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.5);
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
  background:
    linear-gradient(
      180deg,
      rgba(var(--v-theme-surface), 1),
      rgba(var(--v-theme-surface), 0.88)
    ),
    radial-gradient(circle at top right, rgba(var(--v-theme-warning), 0.14), transparent 42%);
  box-shadow:
    0 12px 24px rgba(22, 32, 46, 0.06),
    inset 0 1px 0 rgba(255, 255, 255, 0.5);
}

.favorite-tile::before,
.recent-tile::before {
  content: "";
  position: absolute;
  inset: auto -12% 42% 24%;
  height: 44%;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.28), transparent);
  transform: translateX(-44%) skewX(-20deg);
  filter: blur(10px);
  opacity: 0;
  animation: vault-sheen 13s ease-in-out infinite;
  pointer-events: none;
}

.recent-tile {
  background:
    linear-gradient(
      180deg,
      rgba(var(--v-theme-surface), 0.98),
      rgba(var(--v-theme-surface), 0.86)
    ),
    radial-gradient(circle at top right, rgba(var(--v-theme-secondary), 0.12), transparent 42%);
  box-shadow:
    0 12px 24px rgba(22, 32, 46, 0.06),
    inset 0 1px 0 rgba(255, 255, 255, 0.44);
}

.favorite-tile:hover,
.recent-tile:hover {
  transform: translateY(-4px) scale(1.004);
  box-shadow: 0 18px 34px rgba(20, 34, 58, 0.1);
}

.password-preview {
  font-family: "Cascadia Code", "Consolas", monospace;
  word-break: break-all;
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
