<script setup>
import { reactive, ref, watch } from "vue";
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
const formError = ref("");
const { t } = useAppPreferences();

const formState = reactive({
  currentPassphrase: "",
  nextPassphrase: "",
  confirmPassphrase: "",
  reveal: false,
});

const requiredRule = (value) => (!!String(value || "").trim() ? true : t("common.requiredField"));
const minLengthRule = (value) =>
  String(value || "").length >= 8 ? true : t("master.minLength");
const confirmRule = (value) =>
  value === formState.nextPassphrase ? true : t("changeMaster.mismatch");

function resolveFormError() {
  if (!String(formState.currentPassphrase || "").trim()) {
    return t("common.requiredField");
  }

  if (!String(formState.nextPassphrase || "").trim()) {
    return t("common.requiredField");
  }

  if (String(formState.nextPassphrase || "").length < 8) {
    return t("master.minLength");
  }

  if (!String(formState.confirmPassphrase || "").trim()) {
    return t("common.requiredField");
  }

  if (formState.confirmPassphrase !== formState.nextPassphrase) {
    return t("changeMaster.mismatch");
  }

  return "";
}

async function handleSubmit() {
  const result = await formRef.value?.validate();
  if (!result?.valid) {
    formError.value = resolveFormError() || t("common.requiredField");
    return;
  }

  formError.value = "";

  emit("submit", {
    currentPassphrase: formState.currentPassphrase,
    nextPassphrase: formState.nextPassphrase,
  });
}

watch(
  () => props.modelValue,
  (opened) => {
    if (!opened) {
      formState.currentPassphrase = "";
      formState.nextPassphrase = "";
      formState.confirmPassphrase = "";
      formState.reveal = false;
      formError.value = "";
    }
  }
);

watch(
  () => [
    formState.currentPassphrase,
    formState.nextPassphrase,
    formState.confirmPassphrase,
    props.modelValue,
  ],
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
    <v-card class="change-master-card">
      <v-card-title class="px-6 pt-6">{{ t("changeMaster.title") }}</v-card-title>
      <v-card-text class="px-6 pb-2">
        <div class="text-body-2 text-medium-emphasis mb-4 change-master-intro">
          {{ t("changeMaster.description") }}
        </div>

        <v-form ref="formRef" class="change-master-form" @submit.prevent="handleSubmit">
          <v-text-field
            v-model="formState.currentPassphrase"
            type="text"
            :label="t('changeMaster.current')"
            class="change-master-field"
            :class="['vault-masked-field', { 'vault-masked-field--revealed': formState.reveal }]"
            prepend-inner-icon="mdi-lock-outline"
            :rules="[requiredRule]"
            autocomplete="off"
            autocorrect="off"
            autocapitalize="none"
            spellcheck="false"
            hide-details
          />

          <v-text-field
            v-model="formState.nextPassphrase"
            type="text"
            :label="t('changeMaster.next')"
            class="change-master-field"
            :class="['vault-masked-field', { 'vault-masked-field--revealed': formState.reveal }]"
            prepend-inner-icon="mdi-lock-reset"
            :append-inner-icon="formState.reveal ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
            :rules="[requiredRule, minLengthRule]"
            autocomplete="off"
            autocorrect="off"
            autocapitalize="none"
            spellcheck="false"
            hide-details
            @click:append-inner="formState.reveal = !formState.reveal"
          />

          <v-text-field
            v-model="formState.confirmPassphrase"
            type="text"
            :label="t('changeMaster.confirm')"
            class="change-master-field"
            :class="['vault-masked-field', { 'vault-masked-field--revealed': formState.reveal }]"
            prepend-inner-icon="mdi-lock-check-outline"
            :rules="[requiredRule, confirmRule]"
            autocomplete="off"
            autocorrect="off"
            autocapitalize="none"
            spellcheck="false"
            hide-details
          />

          <v-expand-transition>
            <v-alert
              v-if="formError"
              class="mt-3 change-master-alert"
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
        <v-btn color="primary" :loading="loading" @click="handleSubmit">
          {{ t("changeMaster.submit") }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.change-master-card {
  background: var(--vault-panel-bg);
  box-shadow: var(--vault-shadow);
}

.change-master-intro {
  padding: 14px 16px;
  border-radius: calc(var(--vault-radius) - 2px);
  background: var(--vault-block-bg-subtle);
}

:deep(.change-master-form .v-field) {
  background: var(--vault-block-bg) !important;
  box-shadow: none !important;
}

.change-master-alert {
  border-radius: 18px;
}
</style>
