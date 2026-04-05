<script setup>
defineProps({
  items: {
    type: Array,
    default: () => [],
  },
  busyIds: {
    type: Object,
    default: () => ({}),
  },
});

const emit = defineEmits(["restore", "permanent-delete"]);

function formatDeletedAt(timestamp) {
  return new Date(timestamp).toLocaleString();
}
</script>

<template>
  <v-card class="border-sm">
    <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-3">
      <span>最近删除</span>
      <v-chip variant="tonal" color="error">{{ `${items.length}条` }}</v-chip>
    </v-card-title>

    <v-card-text class="pa-4">
      <div v-if="!items.length" class="py-8 text-center">
        <v-icon size="40" color="medium-emphasis">mdi-delete-clock-outline</v-icon>
        <div class="text-subtitle-1 mt-3">最近删除 0条</div>
        <div class="text-body-2 text-medium-emphasis mt-2">
          从列表中删除的项目会先进入这里，你可以恢复或彻底删除。
        </div>
      </div>

      <TransitionGroup v-else name="deleted-list" tag="div" class="d-flex flex-column ga-3">
        <v-sheet
          v-for="item in items"
          :key="item.id"
          class="pa-4 rounded-xl bg-surface deleted-tile"
        >
          <div class="d-flex flex-column flex-md-row align-md-center justify-space-between ga-4">
            <div class="min-w-0">
              <div class="text-subtitle-1 font-weight-medium text-truncate">
                {{ item.siteName || "未命名项目" }}
              </div>
              <div class="text-body-2 text-medium-emphasis text-truncate mt-1">
                {{ item.username }}
              </div>
              <div class="text-caption text-medium-emphasis mt-2">
                删除于 {{ formatDeletedAt(item.deletedAt) }}
              </div>
            </div>

            <div class="d-flex flex-wrap ga-2">
              <v-btn
                variant="text"
                prepend-icon="mdi-restore"
                :loading="Boolean(busyIds[item.id])"
                @click="emit('restore', item.id)"
              >
                恢复
              </v-btn>
              <v-btn
                color="error"
                variant="tonal"
                prepend-icon="mdi-delete-forever-outline"
                :loading="Boolean(busyIds[item.id])"
                @click="emit('permanent-delete', item.id)"
              >
                彻底删除
              </v-btn>
            </div>
          </div>
        </v-sheet>
      </TransitionGroup>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.deleted-tile {
  transition:
    transform 240ms ease,
    box-shadow 240ms ease,
    background-color 240ms ease;
}

.deleted-tile:hover {
  transform: translateY(-2px);
  box-shadow: 0 14px 30px rgba(28, 38, 63, 0.08);
}

.min-w-0 {
  min-width: 0;
}

.deleted-list-enter-active,
.deleted-list-leave-active,
.deleted-list-move {
  transition: all 260ms cubic-bezier(0.2, 0.7, 0.2, 1);
}

.deleted-list-enter-from,
.deleted-list-leave-to {
  opacity: 0;
  transform: translateY(12px);
}
</style>
