<script setup>
import { computed } from "vue";
import DeletedList from "@/components/DeletedList.vue";
import PasswordList from "@/components/PasswordList.vue";

const props = defineProps({
  items: {
    type: Array,
    default: () => [],
  },
  deletedItems: {
    type: Array,
    default: () => [],
  },
  totalCount: {
    type: Number,
    default: 0,
  },
  favoriteCount: {
    type: Number,
    default: 0,
  },
  searchText: {
    type: String,
    default: "",
  },
  listMode: {
    type: String,
    default: "all",
  },
  revealedPasswords: {
    type: Object,
    default: () => ({}),
  },
  revealingIds: {
    type: Object,
    default: () => ({}),
  },
  editingIds: {
    type: Object,
    default: () => ({}),
  },
  favoriteIds: {
    type: Object,
    default: () => ({}),
  },
  deletedBusyIds: {
    type: Object,
    default: () => ({}),
  },
  selectionMode: {
    type: Boolean,
    default: false,
  },
  selectedIds: {
    type: Array,
    default: () => [],
  },
  bulkLoading: {
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
  "restore",
  "permanent-delete",
  "update:listMode",
  "toggle-selection-mode",
  "toggle-select",
  "select-all",
  "bulk-favorite",
  "bulk-delete",
]);

const listModeItems = [
  { title: "全部", value: "all" },
  { title: "收藏夹", value: "favorites" },
  { title: "最近删除", value: "deleted" },
];

const statusText = computed(() => {
  if (props.listMode === "deleted") {
    return `最近删除 ${props.deletedItems.length}条`;
  }

  if (props.searchText) {
    return `搜索结果 ${props.items.length}条`;
  }

  return `共 ${props.totalCount}条，收藏 ${props.favoriteCount}条`;
});
</script>

<template>
  <div class="d-flex flex-column ga-4">
    <v-card class="border-sm">
      <v-card-text class="pa-5 d-flex flex-column flex-lg-row align-lg-center justify-space-between ga-4">
        <div>
          <div class="text-h5 font-weight-medium">全部项目</div>
          <div class="text-body-2 text-medium-emphasis mt-2">
            {{ statusText }}
          </div>
        </div>

        <v-btn-toggle
          :model-value="listMode"
          rounded="xl"
          mandatory
          @update:model-value="emit('update:listMode', $event)"
        >
          <v-btn
            v-for="mode in listModeItems"
            :key="mode.value"
            :value="mode.value"
          >
            {{ mode.title }}
          </v-btn>
        </v-btn-toggle>
      </v-card-text>
    </v-card>

    <DeletedList
      v-if="listMode === 'deleted'"
      :items="deletedItems"
      :busy-ids="deletedBusyIds"
      @restore="emit('restore', $event)"
      @permanent-delete="emit('permanent-delete', $event)"
    />

    <PasswordList
      v-else
      :items="items"
      :revealed-passwords="revealedPasswords"
      :revealing-ids="revealingIds"
      :editing-ids="editingIds"
      :favorite-ids="favoriteIds"
      :selection-mode="selectionMode"
      :selected-ids="selectedIds"
      :bulk-loading="bulkLoading"
      @toggle-reveal="emit('toggle-reveal', $event)"
      @toggle-favorite="emit('toggle-favorite', $event)"
      @edit="emit('edit', $event)"
      @delete="emit('delete', $event)"
      @copy-password="emit('copy-password', $event)"
      @copy-username="emit('copy-username', $event)"
      @toggle-selection-mode="emit('toggle-selection-mode', $event)"
      @toggle-select="emit('toggle-select', $event)"
      @select-all="emit('select-all', $event)"
      @bulk-favorite="emit('bulk-favorite')"
      @bulk-delete="emit('bulk-delete')"
    />
  </div>
</template>
