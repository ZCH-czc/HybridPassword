<script setup>
import { computed, ref } from "vue";
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

const displayTitle = computed(() => props.item.siteName || "未命名项目");
const avatarLabel = computed(() => (displayTitle.value?.slice(0, 1) || "P").toUpperCase());
const displayPassword = computed(() =>
  props.revealedPassword ? props.revealedPassword : maskPassword("placeholder")
);

function formatDate(timestamp) {
  return new Date(timestamp).toLocaleString();
}
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

          <v-avatar color="primary" variant="tonal" size="42">
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
                color="primary"
                variant="tonal"
              >
                收藏
              </v-chip>
            </div>
            <div class="text-body-2 text-medium-emphasis text-truncate">
              {{ item.username }}
            </div>
            <div class="text-caption text-medium-emphasis mt-1">
              更新于 {{ formatDate(item.updatedAt) }}
            </div>
          </div>
        </div>

        <div v-if="!selectionMode" class="d-flex ga-1">
          <v-btn
            icon
            variant="text"
            :loading="favoriteLoading"
            @click="emit('toggle-favorite', item.id)"
          >
            <v-icon>{{ item.isFavorite ? "mdi-star" : "mdi-star-outline" }}</v-icon>
          </v-btn>
          <v-btn icon variant="text" :loading="editLoading" @click="emit('edit', item.id)">
            <v-icon>mdi-pencil-outline</v-icon>
          </v-btn>
          <v-btn icon variant="text" color="error" @click="emit('delete', item.id)">
            <v-icon>mdi-delete-outline</v-icon>
          </v-btn>
        </div>
      </div>

      <v-sheet class="mt-4 pa-3 rounded-lg border-sm bg-surface-variant vault-inner-sheet">
        <div class="text-caption text-medium-emphasis mb-1">密码</div>
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
            {{ revealedPassword ? "隐藏明文" : "显示明文" }}
          </v-btn>

          <v-btn size="small" variant="text" @click="emit('copy-password', item.id)">
            复制密码
          </v-btn>

          <v-btn size="small" variant="text" @click="emit('copy-username', item.username)">
            复制用户名
          </v-btn>
        </div>
      </v-sheet>

      <div class="d-flex align-center justify-space-between mt-3">
        <div class="text-body-2 text-medium-emphasis">
          {{ item.notes.length ? `共 ${item.notes.length}条备注` : "暂无备注" }}
        </div>

        <v-btn
          variant="text"
          size="small"
          :disabled="!item.notes.length"
          @click="notesExpanded = !notesExpanded"
        >
          {{ notesExpanded ? "收起备注" : "展开备注" }}
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
  transition:
    transform 220ms ease,
    box-shadow 220ms ease,
    background-color 220ms ease,
    border-color 220ms ease;
}

.vault-list-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 14px 32px rgba(26, 40, 70, 0.08);
}

.vault-list-card--selected {
  box-shadow: 0 0 0 2px rgba(var(--v-theme-primary), 0.22), var(--vault-shadow-soft);
}
</style>
