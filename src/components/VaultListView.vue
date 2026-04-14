<script setup>
import { computed } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";
import DeletedList from "@/components/DeletedList.vue";
import PasskeyList from "@/components/PasskeyList.vue";
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
  passkeyCount: {
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
  locale: {
    type: String,
    default: "zh-CN",
  },
  supportsPasskeys: {
    type: Boolean,
    default: false,
  },
  passkeyItems: {
    type: Array,
    default: () => [],
  },
  passkeyRefreshing: {
    type: Boolean,
    default: false,
  },
  passkeyBusyIds: {
    type: Object,
    default: () => ({}),
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
  "refresh-passkeys",
  "remove-passkey",
  "update:listMode",
  "toggle-selection-mode",
  "toggle-select",
  "select-all",
  "bulk-favorite",
  "bulk-delete",
]);

const { t } = useAppPreferences();
const isZh = computed(() => String(props.locale || "").toLowerCase().startsWith("zh"));

const passkeyCopy = computed(() =>
  isZh.value
    ? {
        tab: "Passkeys",
        section: "Passkeys",
        status: props.searchText
          ? `匹配到${props.passkeyItems.length}条 passkey`
          : `共${props.passkeyCount}条 passkey 元数据`,
      }
    : {
        tab: "Passkeys",
        section: "Passkeys",
        status: props.searchText
          ? `${props.passkeyItems.length} matching passkeys`
          : `${props.passkeyCount} passkey metadata items`,
      }
);

const listModeItems = computed(() => {
  const items = [
    { title: t("list.tabAll"), value: "all" },
    { title: t("list.tabFavorites"), value: "favorites" },
  ];

  if (props.supportsPasskeys) {
    items.push({ title: passkeyCopy.value.tab, value: "passkeys" });
  }

  items.push({ title: t("list.tabDeleted"), value: "deleted" });
  return items;
});

const statusText = computed(() => {
  if (props.listMode === "passkeys") {
    return passkeyCopy.value.status;
  }

  if (props.listMode === "deleted") {
    return t("list.statusDeleted", { count: props.deletedItems.length });
  }

  if (props.searchText) {
    return t("list.statusSearch", { count: props.items.length });
  }

  return t("list.statusAll", {
    total: props.totalCount,
    favorite: props.favoriteCount,
  });
});

const sectionTitle = computed(() => {
  if (props.listMode === "passkeys") {
    return passkeyCopy.value.section;
  }

  return props.listMode === "favorites" ? t("list.tabFavorites") : t("list.savedPasswords");
});
</script>

<template>
  <div class="d-flex flex-column ga-4">
    <v-card class="border-sm list-header-card">
      <v-card-text class="pa-5 d-flex flex-column flex-lg-row align-lg-center justify-space-between ga-4">
        <div>
          <div class="text-h5 font-weight-medium">{{ t("list.title") }}</div>
          <div class="text-body-2 text-medium-emphasis mt-2">
            {{ statusText }}
          </div>
        </div>

        <v-btn-toggle
          :model-value="listMode"
          rounded="xl"
          mandatory
          class="list-mode-toggle"
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

    <PasskeyList
      v-else-if="listMode === 'passkeys'"
      :items="passkeyItems"
      :locale="locale"
      :refreshing="passkeyRefreshing"
      :busy-ids="passkeyBusyIds"
      @refresh="emit('refresh-passkeys')"
      @remove="emit('remove-passkey', $event)"
    />

    <PasswordList
      v-else
      :items="items"
      :section-title="sectionTitle"
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

<style scoped>
.list-header-card {
  background: var(--vault-panel-bg);
}

.list-mode-toggle {
  background: var(--vault-block-bg-subtle) !important;
  padding: 4px;
}

.list-mode-toggle :deep(.v-btn) {
  background: transparent;
  box-shadow: none;
}

.list-mode-toggle :deep(.v-btn--active) {
  background: var(--vault-block-bg) !important;
}
</style>
