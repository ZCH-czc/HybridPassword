<script setup>
import { computed } from "vue";
import InlineSvgIcon from "@/components/InlineSvgIcon.vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

const props = defineProps({
  locale: {
    type: String,
    default: "zh-CN",
  },
  platform: {
    type: String,
    default: "web",
  },
  supported: {
    type: Boolean,
    default: false,
  },
  supportsMetadataManagement: {
    type: Boolean,
    default: false,
  },
  hasPlatformAuthenticator: {
    type: Boolean,
    default: false,
  },
  companionAppIntegrated: {
    type: Boolean,
    default: false,
  },
  items: {
    type: Array,
    default: () => [],
  },
  deletedItems: {
    type: Array,
    default: () => [],
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

const emit = defineEmits([
  "refresh",
  "open-status",
  "remove",
  "restore",
  "permanent-delete",
]);

const { formatDateTime } = useAppPreferences();

const isZh = computed(() => String(props.locale || "").toLowerCase().startsWith("zh"));

const copy = computed(() =>
  isZh.value
    ? {
        title: "Passkeys",
        subtitle: "查看 Windows 中已保存的通行密钥与账户信息。",
        statusReady: "可用",
        statusUnavailable: "未就绪",
        windowsOnly: "仅限 Windows",
        heroReady:
          "当前设备已经可以读取 Windows 系统里的通行密钥。你可以在这里查看站点、账户和最近使用时间。",
        heroUnavailable:
          "当前环境还没有准备好使用 Windows 通行密钥。你仍然可以在“应用状态”里查看原因。",
        helperReady: "系统通行密钥元数据可读取",
        helperUnavailable: "系统通行密钥尚不可读取",
        helperAuthenticatorReady: "平台认证器已就绪",
        helperAuthenticatorMissing: "未检测到平台认证器",
        helperCompanionReady: "后台组件已连接",
        helperCompanionPending: "后台组件未连接",
        helperDeleted: "最近删除",
        refresh: "刷新数据",
        openStatus: "应用状态",
        statusEntryTitle: "查看应用状态",
        statusEntryBody: "日志、Companion、能力诊断与自动启动设置都在这里。",
        savedTitle: "已保存的 Passkeys",
        savedSubtitle: `${props.items.length}条`,
        deletedTitle: "最近删除",
        deletedSubtitle: `${props.deletedItems.length}条`,
        deletedEmpty: "最近删除 0条",
        emptyTitle: "还没有可显示的 Passkey",
        emptyBody: "当 Windows 系统中存在通行密钥后，这里会先显示站点和账户信息。",
        lastUsed: "最近使用",
        updatedAt: "最近更新",
        authenticator: "认证器",
        transports: "传输方式",
        removable: "可移除",
        backedUp: "已备份",
        remove: "移入最近删除",
        restore: "恢复",
        permanentDelete: "彻底删除",
        unknownUser: "未知账户",
        none: "无",
      }
    : {
        title: "Passkeys",
        subtitle: "View passkeys currently saved in Windows.",
        statusReady: "Available",
        statusUnavailable: "Not ready",
        windowsOnly: "Windows only",
        heroReady:
          "This device can already read Windows system passkeys. Review each site, account, and last-used time here.",
        heroUnavailable:
          "This environment is not ready for Windows passkeys yet. You can still check why in App status.",
        helperReady: "System passkey metadata is available",
        helperUnavailable: "System passkey metadata is not available yet",
        helperAuthenticatorReady: "Platform authenticator ready",
        helperAuthenticatorMissing: "No platform authenticator detected",
        helperCompanionReady: "Background companion connected",
        helperCompanionPending: "Background companion not connected",
        helperDeleted: "Recently deleted",
        refresh: "Refresh",
        openStatus: "App status",
        statusEntryTitle: "View app status",
        statusEntryBody: "Logs, companion status, diagnostics, and auto-launch settings live here.",
        savedTitle: "Saved passkeys",
        savedSubtitle: `${props.items.length} items`,
        deletedTitle: "Recently deleted",
        deletedSubtitle: `${props.deletedItems.length} items`,
        deletedEmpty: "Deleted 0 items",
        emptyTitle: "No passkeys are available yet",
        emptyBody:
          "As soon as Windows has system passkeys available, their site and account metadata will appear here.",
        lastUsed: "Last used",
        updatedAt: "Updated",
        authenticator: "Authenticator",
        transports: "Transports",
        removable: "Removable",
        backedUp: "Backed up",
        remove: "Move to deleted",
        restore: "Restore",
        permanentDelete: "Delete permanently",
        unknownUser: "Unknown account",
        none: "None",
      }
);

const summaryTiles = computed(() => [
  {
    key: "availability",
    label: props.supported ? copy.value.statusReady : copy.value.statusUnavailable,
    value: props.supported ? copy.value.helperReady : copy.value.helperUnavailable,
    icon: props.supported ? "mdi-check-decagram-outline" : "mdi-progress-question",
  },
  {
    key: "authenticator",
    label: props.hasPlatformAuthenticator
      ? copy.value.helperAuthenticatorReady
      : copy.value.helperAuthenticatorMissing,
    value: props.hasPlatformAuthenticator ? copy.value.statusReady : copy.value.statusUnavailable,
    icon: "mdi-fingerprint",
  },
  {
    key: "companion",
    label: props.companionAppIntegrated
      ? copy.value.helperCompanionReady
      : copy.value.helperCompanionPending,
    value: props.companionAppIntegrated ? copy.value.statusReady : copy.value.statusUnavailable,
    icon: "mdi-application-cog-outline",
  },
  {
    key: "deleted",
    label: copy.value.helperDeleted,
    value: isZh.value ? `${props.deletedItems.length}条` : `${props.deletedItems.length} items`,
    icon: "mdi-delete-clock-outline",
  },
]);

function formatTransportList(transportHints = []) {
  return Array.isArray(transportHints) && transportHints.length
    ? transportHints.join(" / ")
    : copy.value.none;
}

function getDisplaySite(rpId) {
  const value = String(rpId || "").trim();
  if (!value) {
    return "unknown";
  }

  return value.replace(/^www\./i, "");
}

function getAvatarText(rpId) {
  const site = getDisplaySite(rpId);
  if (!site || site === "unknown") {
    return "PK";
  }

  const parts = site.split(/[.\-_]/).filter(Boolean);
  if (parts.length >= 2) {
    return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
  }

  return site.slice(0, 2).toUpperCase();
}

function getAvatarStyle(seed) {
  const value = String(seed || "passkey");
  let hash = 0;
  for (let index = 0; index < value.length; index += 1) {
    hash = (hash * 31 + value.charCodeAt(index)) % 360;
  }

  const hueA = hash % 360;
  const hueB = (hash + 32) % 360;
  return {
    background: `linear-gradient(135deg, hsla(${hueA}, 72%, 88%, 0.95), hsla(${hueB}, 78%, 82%, 0.95))`,
    color: `hsl(${(hash + 210) % 360}, 28%, 24%)`,
  };
}
</script>

<template>
  <div class="d-flex flex-column ga-4">
    <v-card class="border-sm passkey-card">
      <v-card-text class="pa-5 pa-sm-6">
        <div class="d-flex flex-column flex-xl-row align-xl-center justify-space-between ga-4">
          <div class="d-flex align-start ga-4 min-w-0">
            <v-avatar size="58" class="passkey-hero-avatar">
              <InlineSvgIcon icon="mdi-key-chain-variant" :size="28" />
            </v-avatar>

            <div class="min-w-0">
              <div class="d-flex align-center ga-2 flex-wrap">
                <div class="text-h5 font-weight-bold">
                  {{ copy.title }}
                </div>
                <v-chip :color="supported ? 'success' : 'secondary'" variant="tonal">
                  {{ supported ? copy.statusReady : copy.statusUnavailable }}
                </v-chip>
                <v-chip variant="tonal" color="secondary">
                  {{ copy.windowsOnly }}
                </v-chip>
              </div>

              <div class="text-body-2 text-medium-emphasis mt-2">
                {{ copy.subtitle }}
              </div>
              <div class="text-body-1 mt-3 passkey-hero-copy">
                {{ supported ? copy.heroReady : copy.heroUnavailable }}
              </div>
            </div>
          </div>

          <div class="d-flex align-center ga-2 flex-wrap justify-end">
            <v-btn
              variant="text"
              :disabled="platform !== 'windows'"
              @click="emit('open-status')"
            >
              <template #prepend>
                <InlineSvgIcon icon="mdi-chart-box-outline" :size="18" />
              </template>
              {{ copy.openStatus }}
            </v-btn>

            <v-btn
              color="primary"
              :loading="refreshing"
              :disabled="platform !== 'windows' || !supportsMetadataManagement"
              @click="emit('refresh')"
            >
              <template #prepend>
                <InlineSvgIcon icon="mdi-refresh" :size="18" />
              </template>
              {{ copy.refresh }}
            </v-btn>
          </div>
        </div>

        <div class="passkey-summary-grid mt-5">
          <v-sheet
            v-for="tile in summaryTiles"
            :key="tile.key"
            class="pa-4 passkey-summary-tile"
          >
            <div class="d-flex align-center ga-3">
              <v-avatar size="38" class="passkey-summary-tile__avatar">
                <InlineSvgIcon :icon="tile.icon" :size="18" />
              </v-avatar>
              <div class="min-w-0">
                <div class="text-caption text-medium-emphasis">
                  {{ tile.label }}
                </div>
                <div class="text-subtitle-1 font-weight-medium text-wrap">
                  {{ tile.value }}
                </div>
              </div>
            </div>
          </v-sheet>
        </div>

        <button type="button" class="passkey-status-entry mt-4" @click="emit('open-status')">
          <div class="passkey-status-entry__main">
            <v-avatar size="40" class="passkey-status-entry__avatar">
              <InlineSvgIcon icon="mdi-chart-box-outline" :size="18" />
            </v-avatar>
            <div class="min-w-0">
              <div class="text-subtitle-1 font-weight-medium">
                {{ copy.statusEntryTitle }}
              </div>
              <div class="text-body-2 text-medium-emphasis mt-1">
                {{ copy.statusEntryBody }}
              </div>
            </div>
          </div>
          <InlineSvgIcon icon="mdi-chevron-right" :size="18" />
        </button>
      </v-card-text>
    </v-card>

    <v-card class="border-sm passkey-card">
      <v-card-text class="pa-5 pa-sm-6">
        <div class="d-flex align-center justify-space-between ga-3 flex-wrap">
          <div>
            <div class="text-h6 font-weight-bold">{{ copy.savedTitle }}</div>
            <div class="text-body-2 text-medium-emphasis mt-1">
              {{ copy.savedSubtitle }}
            </div>
          </div>
        </div>

        <v-sheet v-if="!items.length" class="pa-6 mt-4 text-center settings-block passkey-empty">
          <InlineSvgIcon icon="mdi-key-chain-variant" :size="34" />
          <div class="text-subtitle-1 font-weight-medium mt-3">{{ copy.emptyTitle }}</div>
          <div class="text-body-2 text-medium-emphasis mt-2">
            {{ copy.emptyBody }}
          </div>
        </v-sheet>

        <div v-else class="d-flex flex-column ga-3 mt-4">
          <v-sheet
            v-for="item in items"
            :key="item.id"
            class="pa-4 rounded-xl vault-surface-block passkey-record-card"
          >
            <div class="d-flex flex-column flex-lg-row justify-space-between ga-4">
              <div class="d-flex align-start ga-4 min-w-0">
                <v-avatar size="48" class="passkey-record-card__avatar" :style="getAvatarStyle(item.rpId)">
                  <span class="passkey-record-card__avatar-text">
                    {{ getAvatarText(item.rpId) }}
                  </span>
                </v-avatar>

                <div class="min-w-0 flex-grow-1">
                  <div class="d-flex align-center gap-2 flex-wrap">
                    <div class="text-subtitle-1 font-weight-bold text-truncate">
                      {{ getDisplaySite(item.rpId) }}
                    </div>
                  </div>
                  <div class="text-body-2 text-medium-emphasis text-truncate mt-1">
                    {{ item.username || item.displayName || copy.unknownUser }}
                  </div>

                  <div v-if="item.displayName" class="text-caption text-medium-emphasis mt-2">
                    {{ item.displayName }}
                  </div>

                  <div class="passkey-meta-grid mt-3">
                    <div class="passkey-meta-tile">
                      <span class="passkey-meta-tile__label">{{ copy.lastUsed }}</span>
                      <span class="passkey-meta-tile__value">
                        {{ item.lastUsedAt ? formatDateTime(item.lastUsedAt) : copy.none }}
                      </span>
                    </div>
                    <div class="passkey-meta-tile">
                      <span class="passkey-meta-tile__label">{{ copy.updatedAt }}</span>
                      <span class="passkey-meta-tile__value">
                        {{ formatDateTime(item.updatedAt) }}
                      </span>
                    </div>
                  </div>

                  <div class="d-flex flex-wrap ga-2 mt-3">
                    <v-chip
                      v-if="item.authenticatorName"
                      size="small"
                      variant="tonal"
                      color="secondary"
                    >
                      {{ copy.authenticator }} · {{ item.authenticatorName }}
                    </v-chip>
                    <v-chip size="small" variant="tonal" color="secondary">
                      {{ copy.transports }} · {{ formatTransportList(item.transportHints) }}
                    </v-chip>
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
              </div>

              <div class="d-flex align-end justify-end">
                <v-btn
                  variant="text"
                  :loading="Boolean(busyIds[item.id])"
                  @click="emit('remove', item.id)"
                >
                  <template #prepend>
                    <InlineSvgIcon icon="mdi-delete-clock-outline" :size="18" />
                  </template>
                  {{ copy.remove }}
                </v-btn>
              </div>
            </div>
          </v-sheet>
        </div>
      </v-card-text>
    </v-card>

    <v-card class="border-sm passkey-card">
      <v-card-text class="pa-5 pa-sm-6">
        <div class="d-flex align-center justify-space-between ga-3 flex-wrap">
          <div>
            <div class="text-h6 font-weight-bold">{{ copy.deletedTitle }}</div>
            <div class="text-body-2 text-medium-emphasis mt-1">
              {{ copy.deletedSubtitle }}
            </div>
          </div>
        </div>

        <v-sheet
          v-if="!deletedItems.length"
          class="pa-4 mt-4 text-body-2 text-medium-emphasis settings-block"
        >
          {{ copy.deletedEmpty }}
        </v-sheet>

        <div v-else class="d-flex flex-column ga-3 mt-4">
          <v-sheet
            v-for="item in deletedItems"
            :key="item.id"
            class="pa-4 rounded-xl vault-surface-block passkey-record-card passkey-record-card--deleted"
          >
            <div class="d-flex flex-column flex-lg-row justify-space-between ga-4">
              <div class="d-flex align-start ga-4 min-w-0">
                <v-avatar size="44" class="passkey-record-card__avatar" :style="getAvatarStyle(item.rpId)">
                  <span class="passkey-record-card__avatar-text">
                    {{ getAvatarText(item.rpId) }}
                  </span>
                </v-avatar>

                <div class="min-w-0">
                  <div class="text-subtitle-1 font-weight-medium text-truncate">
                    {{ getDisplaySite(item.rpId) }}
                  </div>
                  <div class="text-body-2 text-medium-emphasis text-truncate mt-1">
                    {{ item.username || item.displayName || copy.unknownUser }}
                  </div>
                </div>
              </div>

              <div class="d-flex flex-wrap justify-end ga-2">
                <v-btn
                  variant="text"
                  :loading="Boolean(busyIds[item.id])"
                  @click="emit('restore', item.id)"
                >
                  <template #prepend>
                    <InlineSvgIcon icon="mdi-restore" :size="18" />
                  </template>
                  {{ copy.restore }}
                </v-btn>
                <v-btn
                  color="error"
                  variant="tonal"
                  :loading="Boolean(busyIds[item.id])"
                  @click="emit('permanent-delete', item.id)"
                >
                  <template #prepend>
                    <InlineSvgIcon icon="mdi-delete-forever-outline" :size="18" />
                  </template>
                  {{ copy.permanentDelete }}
                </v-btn>
              </div>
            </div>
          </v-sheet>
        </div>
      </v-card-text>
    </v-card>
  </div>
</template>

<style scoped>
.passkey-card {
  background: var(--vault-panel-bg);
}

.passkey-hero-avatar {
  background:
    radial-gradient(circle at 30% 30%, rgba(var(--v-theme-secondary), 0.22), transparent 58%),
    rgba(var(--v-theme-on-surface), 0.05);
  color: rgba(var(--v-theme-on-surface), 0.86);
}

.passkey-hero-copy {
  max-width: 720px;
}

.passkey-summary-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.passkey-summary-tile {
  background: var(--vault-block-bg);
  border-radius: calc(var(--vault-radius) + 2px);
}

.passkey-summary-tile__avatar {
  background: rgba(var(--v-theme-on-surface), 0.05);
  color: rgba(var(--v-theme-on-surface), 0.72);
}

.passkey-status-entry {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  width: 100%;
  padding: 16px 18px;
  border: none;
  border-radius: calc(var(--vault-radius) + 2px);
  background: var(--vault-block-bg);
  color: inherit;
  text-align: left;
  cursor: pointer;
  transition:
    transform 220ms ease,
    background-color 220ms ease;
}

.passkey-status-entry:hover {
  transform: translateY(-1px);
  background: rgba(var(--v-theme-surface-variant), 0.9);
}

.passkey-status-entry__main {
  display: flex;
  align-items: center;
  gap: 14px;
  min-width: 0;
}

.passkey-status-entry__avatar {
  background: rgba(var(--v-theme-on-surface), 0.05);
  color: rgba(var(--v-theme-on-surface), 0.72);
}

.passkey-empty {
  min-height: 200px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.passkey-record-card {
  transition:
    transform 220ms ease,
    background-color 220ms ease;
}

.passkey-record-card:hover {
  transform: translateY(-1px);
}

.passkey-record-card__avatar {
  flex: 0 0 auto;
  box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.28);
}

.passkey-record-card__avatar-text {
  font-size: 0.85rem;
  font-weight: 700;
  letter-spacing: 0.04em;
}

.passkey-record-card--deleted {
  opacity: 0.92;
}

.passkey-meta-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.passkey-meta-tile {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 10px 12px;
  border-radius: 16px;
  background: var(--vault-block-bg-subtle);
}

.passkey-meta-tile__label {
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.75rem;
}

.passkey-meta-tile__value {
  font-size: 0.9rem;
  font-weight: 600;
}

.min-w-0 {
  min-width: 0;
}

@media (max-width: 980px) {
  .passkey-summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 720px) {
  .passkey-meta-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 640px) {
  .passkey-summary-grid {
    grid-template-columns: 1fr;
  }

  .passkey-hero-avatar {
    width: 48px !important;
    height: 48px !important;
  }
}
</style>
