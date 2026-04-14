<script setup>
import { computed } from "vue";
import ImportReviewList from "@/components/ImportReviewList.vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

const props = defineProps({
  items: {
    type: Array,
    default: () => [],
  },
  updatedAt: {
    type: Number,
    default: 0,
  },
  busy: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["manual-add", "manual-add-many", "dismiss", "dismiss-many"]);
const { t, formatDateTime } = useAppPreferences();

const updatedAtLabel = computed(() =>
  props.updatedAt ? formatDateTime(props.updatedAt) : ""
);
</script>

<template>
  <v-card class="border-sm pending-import-card">
    <v-card-title class="d-flex flex-column align-stretch ga-3 pa-4 pa-sm-5">
      <div class="d-flex align-center justify-space-between flex-wrap ga-3">
        <div class="d-flex align-center ga-3">
          <span>{{ t("settings.pendingImportReview") }}</span>
          <v-chip variant="tonal" color="warning">
            {{ t("common.countItems", { count: items.length }) }}
          </v-chip>
        </div>

        <div v-if="updatedAtLabel" class="text-caption text-medium-emphasis">
          {{ t("importReview.updatedAt", { time: updatedAtLabel }) }}
        </div>
      </div>

      <div class="text-body-2 text-medium-emphasis">
        {{ t("settings.pendingImportReviewBody") }}
      </div>
    </v-card-title>

    <v-card-text class="pa-4 pt-0 pa-sm-5 pt-sm-0">
      <ImportReviewList
        :items="items"
        :busy="busy"
        @manual-add="emit('manual-add', $event)"
        @manual-add-many="emit('manual-add-many', $event)"
        @dismiss="emit('dismiss', $event)"
        @dismiss-many="emit('dismiss-many', $event)"
      />
    </v-card-text>
  </v-card>
</template>

<style scoped>
.pending-import-card {
  background: var(--vault-panel-bg);
}
</style>
