<script setup>
import { computed, reactive, ref, watch } from "vue";

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  mode: {
    type: String,
    default: "unlock",
  },
  loading: {
    type: Boolean,
    default: false,
  },
  biometricEnabled: {
    type: Boolean,
    default: false,
  },
  biometricLabel: {
    type: String,
    default: "生物识别",
  },
  biometricLoading: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["submit", "biometric-unlock"]);
const formRef = ref(null);

const formState = reactive({
  passphrase: "",
  confirmPassphrase: "",
  reveal: false,
});

const isSetup = computed(() => props.mode === "setup");

const requiredRule = (value) => (!!String(value || "").trim() ? true : "该字段不能为空");
const minLengthRule = (value) =>
  String(value || "").length >= 8 ? true : "主密码至少需要 8 位";
const confirmRule = (value) =>
  !isSetup.value || value === formState.passphrase ? true : "两次输入的主密码不一致";

async function handleSubmit() {
  const result = await formRef.value?.validate();

  if (!result?.valid) {
    return;
  }

  emit("submit", formState.passphrase);
}

watch(
  () => [props.modelValue, props.mode],
  () => {
    formState.passphrase = "";
    formState.confirmPassphrase = "";
    formState.reveal = false;
  }
);
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    persistent
    max-width="520"
    class="master-key-overlay"
    scrim="rgba(15, 23, 42, 0.22)"
  >
    <v-card class="border-sm master-key-card">
      <v-card-text class="pa-6 pa-sm-7">
        <div class="d-flex flex-column align-center text-center mb-6">
          <v-avatar color="primary" variant="tonal" size="56" class="mb-4">
            <v-icon size="30">mdi-shield-lock-outline</v-icon>
          </v-avatar>
          <div class="text-h5 font-weight-medium">
            {{ isSetup ? "初始化主密码" : "解锁密码库" }}
          </div>
          <div class="text-body-2 text-medium-emphasis mt-2">
            {{
              isSetup
                ? "主密码会保护当前设备上的密码库，请务必牢记。"
                : "输入主密码后，应用会在本地临时解锁你的密码库。"
            }}
          </div>
        </div>

        <v-form ref="formRef" @submit.prevent="handleSubmit">
          <v-text-field
            v-model="formState.passphrase"
            :type="formState.reveal ? 'text' : 'password'"
            label="主密码"
            prepend-inner-icon="mdi-lock-outline"
            :append-inner-icon="formState.reveal ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
            :rules="[requiredRule, minLengthRule]"
            @click:append-inner="formState.reveal = !formState.reveal"
          />

          <v-text-field
            v-if="isSetup"
            v-model="formState.confirmPassphrase"
            :type="formState.reveal ? 'text' : 'password'"
            label="确认主密码"
            prepend-inner-icon="mdi-lock-check-outline"
            :rules="[requiredRule, confirmRule]"
          />

          <v-btn
            class="mt-4"
            block
            size="large"
            color="primary"
            :loading="loading"
            @click="handleSubmit"
          >
            {{ isSetup ? "创建并解锁" : "解锁" }}
          </v-btn>

          <v-btn
            v-if="!isSetup && biometricEnabled"
            class="mt-3"
            block
            size="large"
            variant="tonal"
            prepend-icon="mdi-fingerprint"
            :loading="biometricLoading"
            @click="emit('biometric-unlock')"
          >
            {{ `使用${biometricLabel}解锁` }}
          </v-btn>
        </v-form>
      </v-card-text>
    </v-card>
  </v-dialog>
</template>

<style scoped>
:deep(.master-key-overlay .v-overlay__scrim) {
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
}

.master-key-card {
  background:
    linear-gradient(
      180deg,
      rgba(var(--v-theme-surface), 0.96),
      rgba(var(--v-theme-surface), 0.9)
    ),
    radial-gradient(circle at top, rgba(26, 115, 232, 0.08), transparent 42%);
  box-shadow: 0 30px 60px rgba(15, 23, 42, 0.18);
}
</style>
