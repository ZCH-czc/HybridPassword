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

async function handleSubmit() {
  const result = await formRef.value?.validate();
  if (!result?.valid) {
    return;
  }

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
        <div class="text-body-2 text-medium-emphasis mb-4">
          {{ t("changeMaster.description") }}
        </div>

        <v-form ref="formRef" @submit.prevent="handleSubmit">
          <v-text-field
            v-model="formState.currentPassphrase"
            :type="formState.reveal ? 'text' : 'password'"
            :label="t('changeMaster.current')"
            prepend-inner-icon="mdi-lock-outline"
            :rules="[requiredRule]"
          />

          <v-text-field
            v-model="formState.nextPassphrase"
            :type="formState.reveal ? 'text' : 'password'"
            :label="t('changeMaster.next')"
            prepend-inner-icon="mdi-lock-reset"
            :append-inner-icon="formState.reveal ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
            :rules="[requiredRule, minLengthRule]"
            @click:append-inner="formState.reveal = !formState.reveal"
          />

          <v-text-field
            v-model="formState.confirmPassphrase"
            :type="formState.reveal ? 'text' : 'password'"
            :label="t('changeMaster.confirm')"
            prepend-inner-icon="mdi-lock-check-outline"
            :rules="[requiredRule, confirmRule]"
          />
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
  background: rgba(var(--v-theme-surface), 0.96);
}
</style>
