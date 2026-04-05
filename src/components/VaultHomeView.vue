<script setup>
import { computed } from "vue";
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

const sectionTitle = computed(() => (props.searchText ? "搜索结果预览" : "最近项目"));

function previewPassword(recordId) {
  return props.revealedPasswords[recordId] || "••••••••••";
}
</script>

<template>
  <div class="d-flex flex-column ga-4">
    <v-card class="hero-panel border-sm overflow-hidden">
      <v-card-text class="pa-6 pa-sm-7">
        <div class="d-flex flex-column flex-lg-row align-lg-center justify-space-between ga-6">
          <div class="hero-copy">
            <div class="text-overline text-primary">首页</div>
            <div class="text-h4 font-weight-medium mt-2">密码库</div>
            <div class="text-body-1 text-medium-emphasis mt-3">
              快速查看收藏和最近使用的项目。
            </div>

            <div class="d-flex flex-wrap ga-2 mt-4">
              <v-chip color="primary" variant="tonal">已保存 {{ summary.total }}条</v-chip>
              <v-chip color="secondary" variant="tonal">备注 {{ summary.notes }}条</v-chip>
              <v-chip variant="flat">当前结果 {{ summary.filtered }}条</v-chip>
            </div>

            <div class="d-flex flex-wrap ga-3 mt-6">
              <v-btn variant="text" prepend-icon="mdi-format-list-bulleted" @click="emit('open-list')">
                查看全部
              </v-btn>
            </div>
          </div>

          <v-sheet class="hero-side pa-4 pa-sm-5 rounded-xl">
            <div class="text-body-2 text-medium-emphasis">安全状态</div>
            <div class="text-h5 font-weight-medium mt-2">当前会话已解锁</div>
            <div class="text-body-2 text-medium-emphasis mt-2">
              锁定后会清空本次会话中的解密内容。
            </div>

            <div class="d-flex align-center ga-3 mt-5">
              <v-avatar color="primary" variant="tonal">
                <v-icon>mdi-shield-check-outline</v-icon>
              </v-avatar>
              <div>
                <div class="text-subtitle-2 font-weight-medium">主密码保护</div>
                <div class="text-body-2 text-medium-emphasis">密码仅在解锁后按需解密显示</div>
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
        <span>收藏夹</span>
        <v-chip color="primary" variant="tonal">{{ favoriteItems.length }}条</v-chip>
      </v-card-title>
      <v-card-text class="pa-4">
        <div v-if="!favoriteItems.length" class="py-8 text-center">
          <v-icon size="40" color="medium-emphasis">mdi-star-outline</v-icon>
          <div class="text-subtitle-1 mt-3">暂无收藏项目</div>
          <div class="text-body-2 text-medium-emphasis mt-2">
            在列表中点亮星标后，这里会优先显示。
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
                <v-avatar color="primary" variant="tonal" size="38">
                  {{ (item.siteName || item.username || "P").slice(0, 1).toUpperCase() }}
                </v-avatar>
                <div class="min-w-0">
                  <div class="text-subtitle-2 font-weight-medium text-truncate">
                    {{ item.siteName || "未命名项目" }}
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
                <v-icon>mdi-star</v-icon>
              </v-btn>
            </div>

            <div class="d-flex flex-wrap ga-2 mt-4">
              <v-btn size="small" variant="text" @click="emit('edit', item.id)">编辑</v-btn>
              <v-btn size="small" variant="text" @click="emit('copy-password', item.id)">
                复制密码
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
            <v-btn variant="text" size="small" @click="emit('open-list')">查看列表</v-btn>
          </v-card-title>

          <v-card-text class="pa-4">
            <div v-if="!recentItems.length" class="py-10 text-center">
              <v-icon size="46" color="medium-emphasis">mdi-folder-key-network-outline</v-icon>
              <div class="text-h6 mt-3">暂无项目</div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                新建一条密码记录后，这里会显示最近使用的项目。
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
                    <v-avatar color="primary" variant="tonal" size="42">
                      {{ (item.siteName || item.username || "P").slice(0, 1).toUpperCase() }}
                    </v-avatar>

                    <div class="min-w-0">
                      <div class="d-flex align-center flex-wrap ga-2">
                        <div class="text-subtitle-1 font-weight-medium text-truncate">
                          {{ item.siteName || "未命名项目" }}
                        </div>
                        <v-icon v-if="item.isFavorite" color="primary" size="18">
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
                  <div class="text-caption text-medium-emphasis mb-1">密码预览</div>
                  <div class="password-preview text-body-2">
                    {{ previewPassword(item.id) }}
                  </div>
                </v-sheet>

                <div class="d-flex flex-wrap align-center justify-space-between ga-3 mt-3">
                  <div class="d-flex flex-wrap ga-2">
                    <v-chip size="small" variant="tonal" color="primary">
                      {{ item.notes.length }}条备注
                    </v-chip>
                    <v-chip size="small" variant="flat">
                      {{ new Date(item.updatedAt).toLocaleDateString() }}
                    </v-chip>
                  </div>

                  <div class="d-flex flex-wrap ga-2">
                    <v-btn size="small" variant="text" @click="emit('copy-username', item.username)">
                      复制用户名
                    </v-btn>
                    <v-btn size="small" variant="text" @click="emit('copy-password', item.id)">
                      复制密码
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
  background:
    radial-gradient(circle at top left, rgba(26, 115, 232, 0.18), transparent 38%),
    radial-gradient(circle at bottom right, rgba(137, 180, 250, 0.35), transparent 40%),
    linear-gradient(
      135deg,
      rgba(var(--v-theme-surface), 0.98),
      rgba(var(--v-theme-surface), 0.88)
    );
}

.hero-copy {
  max-width: 720px;
}

.hero-side {
  min-width: min(100%, 320px);
  background: rgba(var(--v-theme-surface), 0.62);
  backdrop-filter: blur(14px);
  -webkit-backdrop-filter: blur(14px);
}

.min-w-0 {
  min-width: 0;
}

.favorite-tile,
.recent-tile {
  transition:
    transform 240ms ease,
    box-shadow 240ms ease,
    background-color 240ms ease;
}

.favorite-tile {
  width: min(100%, 280px);
  background: linear-gradient(
    180deg,
    rgba(var(--v-theme-surface), 0.96),
    rgba(var(--v-theme-surface), 0.84)
  );
}

.favorite-tile:hover,
.recent-tile:hover {
  transform: translateY(-2px);
  box-shadow: 0 14px 32px rgba(20, 34, 58, 0.08);
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
