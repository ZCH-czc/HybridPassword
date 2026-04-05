<script setup>
import { computed } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

const props = defineProps({
  modelValue: {
    type: String,
    default: "",
  },
  currentView: {
    type: String,
    default: "home",
  },
  disabled: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["update:modelValue", "create", "lock"]);
const { t } = useAppPreferences();

const searchLabel = computed(() =>
  props.currentView === "home" ? t("search.homeLabel") : t("search.listLabel")
);
</script>

<template>
  <div class="top-bar d-flex align-center ga-3">
    <v-text-field
      :model-value="modelValue"
      class="flex-grow-1 top-bar-search"
      prepend-inner-icon="mdi-magnify"
      :label="searchLabel"
      rounded="pill"
      variant="solo-filled"
      :disabled="disabled"
      hide-details
      single-line
      @update:model-value="emit('update:modelValue', $event)"
    />

    <v-btn
      color="primary"
      prepend-icon="mdi-plus"
      height="56"
      class="px-5"
      :disabled="disabled"
      @click="emit('create')"
    >
      {{ t("common.create") }}
    </v-btn>

    <v-btn
      variant="text"
      icon
      size="56"
      class="top-bar-lock"
      :disabled="disabled"
      @click="emit('lock')"
    >
      <v-icon>mdi-lock-outline</v-icon>
    </v-btn>
  </div>
</template>

<style scoped>
.top-bar {
  align-items: stretch;
}

.top-bar-search {
  min-width: 0;
}

:deep(.top-bar-search .v-field) {
  min-height: 56px;
}

.top-bar-lock {
  align-self: center;
}

@media (max-width: 680px) {
  .top-bar {
    gap: 10px;
  }

  .top-bar :deep(.v-btn__content) {
    white-space: nowrap;
  }
}
</style>
