<script setup>
import { computed } from "vue";
import InlineSvgIcon from "@/components/InlineSvgIcon.vue";
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
      data-tour-target="global-search"
      prepend-inner-icon="mdi-magnify"
      :placeholder="searchLabel"
      rounded="pill"
      variant="solo-filled"
      :disabled="disabled"
      hide-details
      single-line
      persistent-placeholder
      @update:model-value="emit('update:modelValue', $event)"
    />

    <v-btn
      color="primary"
      prepend-icon="mdi-plus"
      height="54"
      rounded="pill"
      class="px-5 top-bar-create"
      data-tour-target="create-password"
      :disabled="disabled"
      @click="emit('create')"
    >
      {{ t("common.create") }}
    </v-btn>

    <v-btn
      variant="text"
      icon
      size="54"
      class="top-bar-lock"
      :disabled="disabled"
      @click="emit('lock')"
    >
      <InlineSvgIcon icon="mdi-shield-lock-outline" :size="21" />
    </v-btn>
  </div>
</template>

<style scoped>
.top-bar {
  align-items: center;
  gap: 12px;
}

.top-bar-search {
  min-width: 0;
}

:deep(.top-bar-search .v-field) {
  min-height: 54px;
  background: var(--vault-toolbar-block) !important;
  border: none !important;
  box-shadow: none !important;
}

:deep(.top-bar-search .v-input__control),
:deep(.top-bar-search .v-field__field) {
  align-items: center;
}

:deep(.top-bar-search .v-field__input) {
  display: flex;
  align-items: center;
  min-height: 54px;
  padding-top: 0 !important;
  padding-bottom: 0 !important;
  font-size: 0.95rem;
  font-weight: 600;
  line-height: 1.15;
}

:deep(.top-bar-search .v-field__prepend-inner) {
  align-self: center;
  margin-top: 0 !important;
  padding-top: 0 !important;
  margin-inline-end: 12px;
  color: rgba(var(--v-theme-on-surface), 0.62);
}

.top-bar-lock {
  align-self: center;
  background: var(--vault-toolbar-block);
  box-shadow: none;
}

.top-bar-create {
  box-shadow: none;
  min-width: 116px;
}

:global(.v-theme--dark) .top-bar-search .v-field {
  background: var(--vault-toolbar-block) !important;
  box-shadow: none !important;
}

:global(.v-theme--dark) .top-bar-lock {
  background: var(--vault-toolbar-block);
  box-shadow: none;
}

@media (max-width: 680px) {
  .top-bar {
    gap: 10px;
  }

  .top-bar :deep(.v-btn__content) {
    white-space: nowrap;
  }

  .top-bar-create {
    min-width: 102px;
  }
}
</style>
