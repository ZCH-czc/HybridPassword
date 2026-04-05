<script setup>
import { computed, ref } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["update:modelValue", "complete"]);
const step = ref(0);
const { t } = useAppPreferences();

const slides = computed(() => [
  {
    title: t("onboarding.slide1.title"),
    body: t("onboarding.slide1.body"),
    icon: "mdi-home-variant-outline",
  },
  {
    title: t("onboarding.slide2.title"),
    body: t("onboarding.slide2.body"),
    icon: "mdi-format-list-bulleted",
  },
  {
    title: t("onboarding.slide3.title"),
    body: t("onboarding.slide3.body"),
    icon: "mdi-cog-outline",
  },
]);

function nextStep() {
  if (step.value >= slides.value.length - 1) {
    emit("complete");
    emit("update:modelValue", false);
    step.value = 0;
    return;
  }

  step.value += 1;
}

function skipGuide() {
  emit("complete");
  emit("update:modelValue", false);
  step.value = 0;
}
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="620"
    persistent
    transition="dialog-transition"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="onboarding-card">
      <v-card-text class="pa-6 pa-sm-7">
        <div class="d-flex align-center justify-space-between mb-6">
          <v-chip color="primary" variant="tonal">{{ t("onboarding.title") }}</v-chip>
          <div class="text-caption text-medium-emphasis">{{ step + 1 }}/{{ slides.length }}</div>
        </div>

        <v-window v-model="step" class="overflow-hidden">
          <v-window-item
            v-for="(slide, index) in slides"
            :key="slide.title"
            :value="index"
          >
            <div class="d-flex flex-column align-center text-center py-4">
              <v-avatar color="primary" variant="tonal" size="68">
                <v-icon size="34">{{ slide.icon }}</v-icon>
              </v-avatar>
              <div class="text-h5 font-weight-medium mt-5">{{ slide.title }}</div>
              <div class="text-body-1 text-medium-emphasis mt-3 onboarding-body">
                {{ slide.body }}
              </div>
            </div>
          </v-window-item>
        </v-window>

        <div class="d-flex align-center justify-space-between mt-4">
          <v-btn variant="text" @click="skipGuide">{{ t("common.skip") }}</v-btn>
          <v-btn color="primary" @click="nextStep">
            {{ step === slides.length - 1 ? t("common.start") : t("common.next") }}
          </v-btn>
        </div>
      </v-card-text>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.onboarding-card {
  background:
    linear-gradient(
      180deg,
      rgba(var(--v-theme-surface), 0.98),
      rgba(var(--v-theme-surface), 0.92)
    ),
    radial-gradient(circle at top, rgba(26, 115, 232, 0.1), transparent 40%);
}

.onboarding-body {
  max-width: 420px;
  line-height: 1.7;
}
</style>
