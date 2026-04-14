<script setup>
import { computed } from "vue";
import ImportReviewList from "@/components/ImportReviewList.vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  items: {
    type: Array,
    default: () => [],
  },
  busy: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits([
  "update:modelValue",
  "manual-add",
  "manual-add-many",
  "dismiss",
  "dismiss-many",
]);
const { t } = useAppPreferences();

const hasItems = computed(() => props.items.length > 0);
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="920"
    scrollable
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="border-sm import-review-card">
      <v-card-text class="pa-6 pa-sm-7">
        <div class="d-flex flex-column flex-sm-row align-sm-center justify-space-between ga-4">
          <div>
            <div class="text-h6 font-weight-bold">{{ t("importReview.title") }}</div>
            <div class="text-body-2 text-medium-emphasis mt-2">
              {{ t("importReview.description", { count: items.length }) }}
            </div>
          </div>

          <v-btn variant="text" @click="emit('update:modelValue', false)">
            {{ t("common.done") }}
          </v-btn>
        </div>

        <ImportReviewList
          v-if="hasItems"
          class="mt-6"
          :items="items"
          :busy="busy"
          @manual-add="emit('manual-add', $event)"
          @manual-add-many="emit('manual-add-many', $event)"
          @dismiss="emit('dismiss', $event)"
          @dismiss-many="emit('dismiss-many', $event)"
        />
      </v-card-text>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.import-review-card {
  background: var(--vault-panel-bg);
  box-shadow: var(--vault-shadow);
}
</style>
