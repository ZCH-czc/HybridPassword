<script setup>
import { computed, nextTick, onBeforeUnmount, ref, watch } from "vue";
import InlineSvgIcon from "@/components/InlineSvgIcon.vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

const props = defineProps({
  items: {
    type: Array,
    default: () => [],
  },
  locale: {
    type: String,
    default: "zh-CN",
  },
  refreshing: {
    type: Boolean,
    default: false,
  },
  busyIds: {
    type: Object,
    default: () => ({}),
  },
});

const emit = defineEmits(["refresh", "remove"]);
const { formatDateTime } = useAppPreferences();
const PAGE_SIZE = 40;
const loadMoreRef = ref(null);
const visibleCount = ref(PAGE_SIZE);
let loadMoreObserver = null;

const isZh = computed(() => String(props.locale || "").toLowerCase().startsWith("zh"));
const visibleItems = computed(() => props.items.slice(0, visibleCount.value));

const copy = computed(() =>
  isZh.value
    ? {
        title: "Passkeys",
        count: `${props.items.length}条`,
        refresh: "刷新",
        emptyTitle: "还没有可显示的 passkey",
        emptyBody: "当 Windows 宿主接入 passkey provider 后，这里会显示站点、用户名和最近使用时间。",
        userFallback: "未知用户",
        lastUsed: "最近使用",
        updatedAt: "最近更新",
        remove: "移入最近删除",
        neverUsed: "尚未使用",
        authenticator: "认证器",
        transports: "传输方式",
        removable: "可移除",
        backedUp: "已备份",
        none: "无",
      }
    : {
        title: "Passkeys",
        count: `${props.items.length} items`,
        refresh: "Refresh",
        emptyTitle: "No passkeys are available yet",
        emptyBody:
          "When the Windows host is connected to the passkey provider, this area will list the relying party, username, and recent usage time.",
        userFallback: "Unknown user",
        lastUsed: "Last used",
        updatedAt: "Last updated",
        remove: "Move to deleted",
        neverUsed: "Not used yet",
        authenticator: "Authenticator",
        transports: "Transports",
        removable: "Removable",
        backedUp: "Backed up",
        none: "None",
      }
);

function loadMoreItems() {
  if (visibleCount.value >= props.items.length) {
    return;
  }

  visibleCount.value = Math.min(props.items.length, visibleCount.value + PAGE_SIZE);
}

function resetVisibleItems() {
  visibleCount.value = Math.min(PAGE_SIZE, props.items.length);
}

function disconnectObserver() {
  if (loadMoreObserver) {
    loadMoreObserver.disconnect();
    loadMoreObserver = null;
  }
}

async function setupObserver() {
  disconnectObserver();

  if (typeof IntersectionObserver !== "function") {
    return;
  }

  await nextTick();

  if (!loadMoreRef.value || visibleCount.value >= props.items.length) {
    return;
  }

  loadMoreObserver = new IntersectionObserver(
    (entries) => {
      if (entries.some((entry) => entry.isIntersecting)) {
        loadMoreItems();
      }
    },
    {
      root: null,
      rootMargin: "320px 0px 320px 0px",
      threshold: 0.01,
    }
  );

  loadMoreObserver.observe(loadMoreRef.value);
}

watch(
  () => props.items,
  () => {
    resetVisibleItems();
    void setupObserver();
  },
  { immediate: true }
);

watch(visibleCount, () => {
  void setupObserver();
});

onBeforeUnmount(() => {
  disconnectObserver();
});
</script>

<template>
  <v-card class="border-sm passkey-list-card">
    <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-3">
      <div class="d-flex align-center ga-3">
        <span>{{ copy.title }}</span>
        <v-chip variant="tonal" color="secondary">{{ copy.count }}</v-chip>
      </div>

      <v-btn
        color="primary"
        prepend-icon="mdi-refresh"
        :loading="refreshing"
        @click="emit('refresh')"
      >
        {{ copy.refresh }}
      </v-btn>
    </v-card-title>

    <v-card-text class="pa-4">
      <div v-if="!items.length" class="py-10 text-center">
        <InlineSvgIcon icon="mdi-key-chain-variant" :size="40" />
        <div class="text-h6 mt-3">{{ copy.emptyTitle }}</div>
        <div class="text-body-2 text-medium-emphasis mt-2">
          {{ copy.emptyBody }}
        </div>
      </div>

      <TransitionGroup v-else name="vault-list" tag="div" class="d-flex flex-column ga-3">
        <v-sheet
          v-for="item in visibleItems"
          :key="item.id"
          class="pa-4 rounded-xl vault-surface-block passkey-list-item"
        >
          <div class="d-flex flex-column flex-lg-row align-lg-center justify-space-between ga-4">
            <div class="min-w-0">
              <div class="d-flex align-center ga-2 flex-wrap">
                <div class="text-subtitle-1 font-weight-medium text-truncate">
                  {{ item.rpId || "unknown" }}
                </div>
                <v-chip
                  v-if="item.syncState"
                  size="small"
                  variant="tonal"
                  color="secondary"
                >
                  {{ item.syncState }}
                </v-chip>
              </div>

              <div class="text-body-2 text-medium-emphasis text-truncate mt-1">
                {{ item.username || item.displayName || copy.userFallback }}
              </div>

              <div v-if="item.displayName && item.displayName !== item.username" class="text-caption text-medium-emphasis mt-2">
                {{ item.displayName }}
              </div>

              <div v-if="item.origin" class="text-caption text-medium-emphasis mt-2 text-truncate">
                {{ item.origin }}
              </div>

              <div v-if="item.authenticatorName" class="text-caption text-medium-emphasis mt-2">
                {{ copy.authenticator }}: {{ item.authenticatorName }}
              </div>

              <div class="text-caption text-medium-emphasis mt-2">
                {{ copy.transports }}:
                {{ item.transportHints?.length ? item.transportHints.join(" · ") : copy.none }}
              </div>

              <div class="d-flex flex-wrap ga-2 mt-3">
                <v-chip
                  v-if="item.isRemovable"
                  size="small"
                  variant="tonal"
                  color="secondary"
                >
                  {{ copy.removable }}
                </v-chip>
                <v-chip
                  v-if="item.isBackedUp"
                  size="small"
                  variant="tonal"
                  color="secondary"
                >
                  {{ copy.backedUp }}
                </v-chip>
              </div>
            </div>

            <div class="d-flex flex-column align-lg-end ga-2">
              <div class="text-caption text-medium-emphasis">
                {{ copy.lastUsed }}:
                {{ item.lastUsedAt ? formatDateTime(item.lastUsedAt) : copy.neverUsed }}
              </div>
              <div class="text-caption text-medium-emphasis">
                {{ copy.updatedAt }}: {{ formatDateTime(item.updatedAt) }}
              </div>

              <div class="d-flex flex-wrap justify-end ga-2 mt-2">
                <v-btn
                  variant="text"
                  prepend-icon="mdi-delete-clock-outline"
                  :loading="Boolean(busyIds[item.id])"
                  @click="emit('remove', item.id)"
                >
                  {{ copy.remove }}
                </v-btn>
              </div>
            </div>
          </div>
        </v-sheet>
      </TransitionGroup>

      <div
        v-if="visibleItems.length < items.length"
        ref="loadMoreRef"
        class="d-flex justify-center py-4"
      >
        <v-progress-circular indeterminate size="24" width="2" color="primary" />
      </div>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.passkey-list-card {
  background: var(--vault-panel-bg);
}

.passkey-list-item {
  transition:
    transform 260ms cubic-bezier(0.2, 0.7, 0.2, 1),
    background-color 220ms ease;
}

.passkey-list-item:hover {
  transform: translateY(-1px);
}

.min-w-0 {
  min-width: 0;
}

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
