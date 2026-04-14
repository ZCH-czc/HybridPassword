<script setup>
import { nextTick, reactive, ref, watch } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  loading: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["update:modelValue", "submit"]);

const formRef = ref(null);
const passphraseFieldRef = ref(null);
const formError = ref("");
const { t } = useAppPreferences();

const formState = reactive({
  currentPassphrase: "",
  reveal: false,
});

const requiredRule = (value) => (!!String(value || "").trim() ? true : t("common.requiredField"));

function resetForm() {
  formState.currentPassphrase = "";
  formState.reveal = false;
  formError.value = "";
}

async function focusField() {
  await nextTick();
  const field = passphraseFieldRef.value;

  if (typeof field?.focus === "function") {
    field.focus();
    return;
  }

  const input = field?.$el?.querySelector?.("input");
  input?.focus?.();
}

async function handleSubmit() {
  const result = await formRef.value?.validate();
  if (!result?.valid) {
    formError.value = t("common.requiredField");
    return;
  }

  formError.value = "";
  emit("submit", {
    currentPassphrase: formState.currentPassphrase,
  });
}

watch(
  () => props.modelValue,
  (opened) => {
    if (!opened) {
      resetForm();
      return;
    }

    void focusField();
  }
);

watch(
  () => formState.currentPassphrase,
  () => {
    if (formError.value) {
      formError.value = "";
    }
  }
);
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="560"
    transition="dialog-transition"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="clear-data-card">
      <v-card-title class="px-6 pt-6">{{ t("clearData.title") }}</v-card-title>
      <v-card-text class="px-6 pb-2">
        <div class="text-body-2 text-medium-emphasis mb-4 clear-data-intro">
          {{ t("clearData.description") }}
        </div>

        <v-alert
          class="mb-4 clear-data-warning"
          color="warning"
          density="comfortable"
          icon="mdi-alert-outline"
          variant="tonal"
        >
          {{ t("clearData.warning") }}
        </v-alert>

        <v-form ref="formRef" class="clear-data-form" @submit.prevent="handleSubmit">
          <v-text-field
            ref="passphraseFieldRef"
            v-model="formState.currentPassphrase"
            type="text"
            :label="t('clearData.passphrase')"
            class="clear-data-field"
            :class="['vault-masked-field', { 'vault-masked-field--revealed': formState.reveal }]"
            prepend-inner-icon="mdi-lock-outline"
            :append-inner-icon="formState.reveal ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
            :rules="[requiredRule]"
            autocomplete="off"
            autocorrect="off"
            autocapitalize="none"
            spellcheck="false"
            hide-details
            @click:append-inner="formState.reveal = !formState.reveal"
          />

          <v-expand-transition>
            <v-alert
              v-if="formError"
              class="mt-3 clear-data-error"
              color="error"
              density="comfortable"
              icon="mdi-alert-circle-outline"
              variant="tonal"
            >
              {{ formError }}
            </v-alert>
          </v-expand-transition>
        </v-form>
      </v-card-text>

      <v-card-actions class="px-6 pb-6">
        <v-spacer />
        <v-btn variant="text" @click="emit('update:modelValue', false)">{{ t("common.cancel") }}</v-btn>
        <v-btn color="error" :loading="loading" @click="handleSubmit">
          {{ t("clearData.submit") }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.clear-data-card {
  background: var(--vault-panel-bg);
  box-shadow: var(--vault-shadow);
}

.clear-data-intro,
.clear-data-warning {
  border-radius: calc(var(--vault-radius) - 2px);
}

:deep(.clear-data-form .v-field) {
  background: var(--vault-block-bg) !important;
  box-shadow: none !important;
}

.clear-data-error {
  border-radius: 18px;
}
</style>
