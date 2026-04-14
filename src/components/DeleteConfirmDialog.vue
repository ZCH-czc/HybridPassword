<script setup>
import { computed } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  title: {
    type: String,
    default: "",
  },
  count: {
    type: Number,
    default: 1,
  },
  loading: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["update:modelValue", "confirm"]);
const { t } = useAppPreferences();

const targetLabel = computed(() => {
  if (props.count > 1) {
    return t("deleteDialog.multiTarget", { count: props.count });
  }

  return props.title || t("deleteDialog.singleTarget");
});
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="420"
    transition="dialog-bottom-transition"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="border-sm delete-confirm-card">
      <v-card-text class="pa-6">
        <div class="text-h6 font-weight-medium">{{ t("deleteDialog.title") }}</div>
        <div class="text-body-2 text-medium-emphasis mt-3">
          {{ t("deleteDialog.message", { target: targetLabel }) }}
        </div>
      </v-card-text>

      <v-card-actions class="px-6 pb-6">
        <v-spacer />
        <v-btn variant="text" @click="emit('update:modelValue', false)">{{ t("common.cancel") }}</v-btn>
        <v-btn color="error" :loading="loading" @click="emit('confirm')">
          {{ t("deleteDialog.confirm") }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.delete-confirm-card {
  background: var(--vault-panel-bg);
  box-shadow: var(--vault-shadow);
}
</style>
