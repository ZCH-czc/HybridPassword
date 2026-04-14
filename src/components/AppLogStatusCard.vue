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
  appVersion: {
    type: String,
    default: "0.0.0",
  },
  logs: {
    type: Array,
    default: () => [],
  },
  updatedAt: {
    type: Number,
    default: 0,
  },
  exporting: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["export"]);
const { formatDateTime } = useAppPreferences();

const isZh = computed(() => String(props.locale || "").toLowerCase().startsWith("zh"));

const copy = computed(() =>
  isZh.value
    ? {
        title: "应用日志",
        subtitle: "查看最近的应用事件、错误与同步结果，并可导出给自己留档或排查问题。",
        exportAction: "导出日志",
        latestUpdate: "最近更新",
        totalLogs: "日志数量",
        version: "当前版本",
        platform: "当前平台",
        empty: "当前还没有可显示的应用日志。",
        details: "详细信息",
        repeated: "重复",
        levelInfo: "信息",
        levelWarning: "警告",
        levelError: "错误",
        none: "无",
      }
    : {
        title: "Application logs",
        subtitle:
          "Review recent app events, errors, and sync results, then export them when you need a record or diagnostics.",
        exportAction: "Export logs",
        latestUpdate: "Last updated",
        totalLogs: "Entries",
        version: "Version",
        platform: "Platform",
        empty: "No application logs are available yet.",
        details: "Details",
        repeated: "Repeated",
        levelInfo: "Info",
        levelWarning: "Warning",
        levelError: "Error",
        none: "None",
      }
);

function getLevelColor(level) {
  if (level === "error") {
    return "error";
  }

  if (level === "warning") {
    return "warning";
  }

  return "info";
}

function getLevelLabel(level) {
  if (level === "error") {
    return copy.value.levelError;
  }

  if (level === "warning") {
    return copy.value.levelWarning;
  }

  return copy.value.levelInfo;
}
</script>

<template>
  <v-card class="border-sm about-card">
    <v-card-text class="pa-6 pa-sm-8">
      <div class="about-hero align-start text-left">
        <v-avatar size="84" class="about-hero__avatar">
          <InlineSvgIcon icon="mdi-text-box-search-outline" :size="40" />
        </v-avatar>

        <div class="text-h5 font-weight-bold mt-5">
          {{ copy.title }}
        </div>
        <div class="text-body-1 text-medium-emphasis mt-2 about-hero__body">
          {{ copy.subtitle }}
        </div>
      </div>

      <div class="app-log-meta-grid mt-8">
        <div class="about-panel">
          <div class="about-panel__head">
            <InlineSvgIcon icon="mdi-update" :size="18" />
            <span>{{ copy.latestUpdate }}</span>
          </div>
          <div class="about-panel__value mt-3">
            {{ updatedAt ? formatDateTime(updatedAt) : copy.none }}
          </div>
        </div>

        <div class="about-panel">
          <div class="about-panel__head">
            <InlineSvgIcon icon="mdi-format-list-bulleted" :size="18" />
            <span>{{ copy.totalLogs }}</span>
          </div>
          <div class="about-panel__value mt-3">{{ logs.length }}</div>
        </div>

        <div class="about-panel">
          <div class="about-panel__head">
            <InlineSvgIcon icon="mdi-information-outline" :size="18" />
            <span>{{ copy.version }}</span>
          </div>
          <div class="about-panel__value mt-3">v{{ appVersion }}</div>
        </div>

        <div class="about-panel">
          <div class="about-panel__head">
            <InlineSvgIcon icon="mdi-monitor-dashboard" :size="18" />
            <span>{{ copy.platform }}</span>
          </div>
          <div class="about-panel__value mt-3 text-capitalize">{{ platform }}</div>
        </div>
      </div>

      <div class="d-flex justify-end mt-6">
        <v-btn color="primary" :loading="exporting" @click="emit('export')">
          <template #prepend>
            <InlineSvgIcon icon="mdi-download" :size="18" />
          </template>
          {{ copy.exportAction }}
        </v-btn>
      </div>

      <div v-if="!logs.length" class="about-panel mt-6">
        <div class="text-body-2 text-medium-emphasis">
          {{ copy.empty }}
        </div>
      </div>

      <div v-else class="d-flex flex-column ga-3 mt-6">
        <div
          v-for="entry in logs"
          :key="entry.id || `${entry.timestampUnixTimeMs}-${entry.message}`"
          class="about-panel app-log-item"
        >
          <div class="d-flex align-center justify-space-between ga-3 flex-wrap">
            <div class="d-flex align-center ga-2 flex-wrap">
              <v-chip :color="getLevelColor(entry.level)" size="small" variant="tonal">
                {{ getLevelLabel(entry.level) }}
              </v-chip>
              <span class="text-caption text-medium-emphasis">{{ entry.source || "app" }}</span>
              <span
                v-if="Number(entry.repeatCount || 1) > 1"
                class="text-caption text-medium-emphasis"
              >
                {{ copy.repeated }} x{{ entry.repeatCount }}
              </span>
            </div>

            <span class="text-caption text-medium-emphasis">
              {{
                entry.timestampUnixTimeMs
                  ? formatDateTime(entry.timestampUnixTimeMs)
                  : copy.none
              }}
            </span>
          </div>

          <div class="text-body-1 mt-3">
            {{ entry.message }}
          </div>

          <div v-if="entry.details" class="mt-3">
            <div class="text-caption text-medium-emphasis">{{ copy.details }}</div>
            <pre class="app-log-item__details">{{ entry.details }}</pre>
          </div>
        </div>
      </div>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.about-card {
  background: var(--vault-panel-bg);
}

.about-hero {
  display: flex;
  flex-direction: column;
}

.about-hero__avatar {
  background:
    radial-gradient(circle at 32% 28%, rgba(var(--v-theme-secondary), 0.18), transparent 56%),
    rgba(var(--v-theme-on-surface), 0.06);
  color: rgba(var(--v-theme-on-surface), 0.92);
}

.about-hero__body {
  max-width: 680px;
}

.about-panel {
  padding: 18px 20px;
  border-radius: calc(var(--vault-radius) + 4px);
  background: var(--vault-block-bg);
}

.about-panel__head {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  font-weight: 600;
}

.about-panel__value {
  color: rgba(var(--v-theme-on-surface), 0.76);
  font-weight: 600;
}

.app-log-meta-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.app-log-item__details {
  margin: 8px 0 0;
  padding: 12px 14px;
  border-radius: calc(var(--vault-radius) - 2px);
  background: var(--vault-block-bg-subtle);
  white-space: pre-wrap;
  word-break: break-word;
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 0.83rem;
  line-height: 1.55;
}

@media (max-width: 760px) {
  .app-log-meta-grid {
    grid-template-columns: 1fr;
  }
}
</style>
