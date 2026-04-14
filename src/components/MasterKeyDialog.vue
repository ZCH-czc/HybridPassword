<script setup>
import { computed, nextTick, reactive, ref, watch } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

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
    default: "Biometrics",
  },
  biometricLoading: {
    type: Boolean,
    default: false,
  },
  requiresSecretKey: {
    type: Boolean,
    default: false,
  },
  secretKeyHint: {
    type: String,
    default: "",
  },
});

const emit = defineEmits(["submit", "biometric-unlock", "after-close"]);
const passphraseFieldRef = ref(null);
const formError = ref("");
const { t } = useAppPreferences();

const formState = reactive({
  passphrase: "",
  confirmPassphrase: "",
  secretKey: "",
  reveal: false,
});

const fieldErrors = reactive({
  passphrase: "",
  confirmPassphrase: "",
  secretKey: "",
});

const isSetup = computed(() => props.mode === "setup");
const headline = computed(() =>
  isSetup.value ? t("master.setupTitle") : t("master.unlockTitle")
);
const bodyText = computed(() =>
  isSetup.value ? t("master.setupBody") : t("master.unlockBody")
);

function resetFormState() {
  formState.passphrase = "";
  formState.confirmPassphrase = "";
  formState.secretKey = "";
  formState.reveal = false;
  fieldErrors.passphrase = "";
  fieldErrors.confirmPassphrase = "";
  fieldErrors.secretKey = "";
  formError.value = "";
}

function clearErrors() {
  fieldErrors.passphrase = "";
  fieldErrors.confirmPassphrase = "";
  fieldErrors.secretKey = "";
  formError.value = "";
}

function validateForm() {
  clearErrors();

  if (!String(formState.passphrase || "").trim()) {
    fieldErrors.passphrase = t("common.requiredField");
  } else if (String(formState.passphrase || "").length < 8) {
    fieldErrors.passphrase = t("master.minLength");
  }

  if (!isSetup.value && props.requiresSecretKey && !String(formState.secretKey || "").trim()) {
    fieldErrors.secretKey = t("common.requiredField");
  }

  if (isSetup.value) {
    if (!String(formState.confirmPassphrase || "").trim()) {
      fieldErrors.confirmPassphrase = t("common.requiredField");
    } else if (formState.confirmPassphrase !== formState.passphrase) {
      fieldErrors.confirmPassphrase = t("master.confirmMismatch");
    }
  }

  formError.value =
    fieldErrors.passphrase || fieldErrors.secretKey || fieldErrors.confirmPassphrase || "";

  return !formError.value;
}

async function focusPassphraseField() {
  await nextTick();
  window.setTimeout(() => {
    passphraseFieldRef.value?.focus?.();
    passphraseFieldRef.value?.select?.();
  }, 120);
}

function handleSubmit() {
  if (!validateForm()) {
    return;
  }

  emit("submit", {
    passphrase: formState.passphrase,
    secretKey: formState.secretKey,
  });
}

function handleFieldInput() {
  if (formError.value) {
    clearErrors();
  }
}

watch(
  () => [props.modelValue, props.mode],
  () => {
    resetFormState();

    if (props.modelValue) {
      void focusPassphraseField();
    }
  }
);
</script>

<template>
  <Teleport to="body">
    <Transition name="unlock-screen" appear @after-leave="emit('after-close')">
      <section
        v-if="modelValue"
        class="unlock-screen"
        role="dialog"
        aria-modal="true"
        aria-label="Vault unlock"
      >
        <div class="unlock-screen__ambient" aria-hidden="true"></div>

        <div class="unlock-screen__doors" aria-hidden="true">
          <span class="unlock-screen__door unlock-screen__door--left"></span>
          <span class="unlock-screen__seam"></span>
          <span class="unlock-screen__door unlock-screen__door--right"></span>
        </div>

        <div class="unlock-screen__shell">
          <div class="unlock-screen__hero">
            <div class="unlock-screen__badge">
              <v-icon size="34">mdi-shield-lock-outline</v-icon>
            </div>
            <div class="unlock-screen__eyebrow">Password Vault</div>
            <h1 class="unlock-screen__title">{{ headline }}</h1>
            <p class="unlock-screen__subtitle">{{ bodyText }}</p>
          </div>

          <div class="unlock-screen__panel">
            <form class="unlock-screen__form" @submit.prevent="handleSubmit">
              <v-sheet
                v-if="!isSetup && requiresSecretKey"
                class="unlock-screen__helper rounded-xl px-4 py-3 text-body-2"
              >
                <div>{{ t("master.secretKeyRequired") }}</div>
                <div v-if="secretKeyHint" class="mt-1 text-medium-emphasis">
                  {{ t("master.secretKeyHint", { hint: secretKeyHint }) }}
                </div>
              </v-sheet>

              <div
                class="unlock-screen__field"
                :class="{ 'unlock-screen__field--invalid': Boolean(fieldErrors.passphrase) }"
              >
                <div class="unlock-screen__field-top">
                  <div class="unlock-screen__field-label">
                    <v-icon size="18">mdi-lock-outline</v-icon>
                    <span>{{ t("master.passphrase") }}</span>
                  </div>

                  <button
                    type="button"
                    class="unlock-screen__toggle"
                    @click="formState.reveal = !formState.reveal"
                  >
                    <v-icon size="20">
                      {{ formState.reveal ? "mdi-eye-off-outline" : "mdi-eye-outline" }}
                    </v-icon>
                  </button>
                </div>

                <input
                  ref="passphraseFieldRef"
                  v-model="formState.passphrase"
                  type="text"
                  class="unlock-screen__input"
                  :class="{ 'unlock-screen__input--masked': !formState.reveal }"
                  autocomplete="off"
                  autocorrect="off"
                  autocapitalize="none"
                  spellcheck="false"
                  @input="handleFieldInput"
                />
              </div>

              <div
                v-if="!isSetup && requiresSecretKey"
                class="unlock-screen__field"
                :class="{ 'unlock-screen__field--invalid': Boolean(fieldErrors.secretKey) }"
              >
                <div class="unlock-screen__field-top">
                  <div class="unlock-screen__field-label">
                    <v-icon size="18">mdi-key-variant</v-icon>
                    <span>{{ t("master.secretKey") }}</span>
                  </div>
                </div>

                <input
                  v-model="formState.secretKey"
                  type="text"
                  class="unlock-screen__input unlock-screen__input--secret"
                  autocomplete="off"
                  autocorrect="off"
                  autocapitalize="characters"
                  spellcheck="false"
                  @input="handleFieldInput"
                />
              </div>

              <div
                v-if="isSetup"
                class="unlock-screen__field"
                :class="{ 'unlock-screen__field--invalid': Boolean(fieldErrors.confirmPassphrase) }"
              >
                <div class="unlock-screen__field-top">
                  <div class="unlock-screen__field-label">
                    <v-icon size="18">mdi-lock-check-outline</v-icon>
                    <span>{{ t("master.confirmPassphrase") }}</span>
                  </div>
                </div>

                <input
                  v-model="formState.confirmPassphrase"
                  type="text"
                  class="unlock-screen__input"
                  :class="{ 'unlock-screen__input--masked': !formState.reveal }"
                  autocomplete="off"
                  autocorrect="off"
                  autocapitalize="none"
                  spellcheck="false"
                  @input="handleFieldInput"
                />
              </div>

              <Transition name="unlock-alert">
                <v-alert
                  v-if="formError"
                  class="unlock-screen__alert"
                  color="error"
                  density="comfortable"
                  icon="mdi-alert-circle-outline"
                  variant="tonal"
                >
                  {{ formError }}
                </v-alert>
              </Transition>

              <div class="unlock-screen__actions">
                <v-btn
                  block
                  size="x-large"
                  color="primary"
                  class="unlock-screen__primary"
                  :loading="loading"
                  @click="handleSubmit"
                >
                  {{ isSetup ? t("master.createAndUnlock") : t("common.unlock") }}
                </v-btn>

                <v-btn
                  v-if="!isSetup && biometricEnabled"
                  block
                  size="large"
                  variant="text"
                  prepend-icon="mdi-fingerprint"
                  class="unlock-screen__biometric"
                  :loading="biometricLoading"
                  @click="emit('biometric-unlock')"
                >
                  {{ t("master.useBiometric", { label: biometricLabel }) }}
                </v-btn>
              </div>
            </form>
          </div>
        </div>
      </section>
    </Transition>
  </Teleport>
</template>

<style scoped>
.unlock-screen {
  position: fixed;
  inset: 0;
  z-index: 2100;
  display: flex;
  align-items: center;
  justify-content: center;
  padding:
    calc(var(--host-safe-top-effective) + 28px)
    max(20px, 4vw)
    calc(var(--host-safe-bottom-effective) + 30px);
  overflow: hidden;
  isolation: isolate;
  background:
    radial-gradient(circle at 18% 18%, rgba(var(--v-theme-primary), 0.12), transparent 28%),
    radial-gradient(circle at 82% 14%, rgba(var(--v-theme-secondary), 0.08), transparent 24%),
    linear-gradient(180deg, rgba(var(--v-theme-background), 0.98), rgba(var(--v-theme-background), 1));
}

.unlock-screen__ambient {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at 50% 0%, rgba(255, 255, 255, 0.12), transparent 28%),
    radial-gradient(circle at 50% 100%, rgba(255, 255, 255, 0.06), transparent 26%);
  opacity: 0.8;
  pointer-events: none;
}

.unlock-screen__doors {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.unlock-screen__door {
  position: absolute;
  top: -4%;
  bottom: -4%;
  width: 51%;
  background:
    linear-gradient(180deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.94)),
    linear-gradient(90deg, rgba(255, 255, 255, 0.22), transparent);
  box-shadow:
    0 30px 60px rgba(25, 32, 44, 0.14),
    inset 0 0 0 1px rgba(255, 255, 255, 0.05);
  backdrop-filter: blur(24px);
  -webkit-backdrop-filter: blur(24px);
}

.unlock-screen__door--left {
  right: 50%;
  border-radius: 0 36px 36px 0;
  transform-origin: right center;
}

.unlock-screen__door--right {
  left: 50%;
  border-radius: 36px 0 0 36px;
  transform-origin: left center;
}

.unlock-screen__seam {
  position: absolute;
  top: 10%;
  bottom: 10%;
  left: 50%;
  width: 10px;
  transform: translateX(-50%);
  border-radius: 999px;
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.84), rgba(255, 255, 255, 0.18)),
    radial-gradient(circle at center, rgba(255, 255, 255, 0.78), transparent 70%);
  box-shadow: 0 0 26px rgba(255, 255, 255, 0.24);
  opacity: 0.58;
}

.unlock-screen__shell {
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  width: min(100%, 560px);
  gap: 22px;
}

.unlock-screen__hero {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 10px;
}

.unlock-screen__badge {
  display: grid;
  place-items: center;
  width: 76px;
  height: 76px;
  border-radius: 26px;
  background: rgba(var(--v-theme-surface), 0.84);
  color: rgb(var(--v-theme-on-surface));
  box-shadow: 0 14px 34px rgba(24, 31, 43, 0.12);
}

.unlock-screen__eyebrow {
  font-size: 0.76rem;
  font-weight: 700;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: rgba(var(--v-theme-on-surface), 0.5);
}

.unlock-screen__title {
  margin: 0;
  font-size: clamp(2rem, 4vw, 2.7rem);
  line-height: 1.08;
  letter-spacing: -0.04em;
  font-weight: 700;
}

.unlock-screen__subtitle {
  max-width: 460px;
  margin: 0;
  font-size: 1rem;
  line-height: 1.6;
  color: rgba(var(--v-theme-on-surface), 0.66);
}

.unlock-screen__panel {
  width: 100%;
  padding: clamp(22px, 4vw, 30px);
  border-radius: 30px;
  background: rgba(var(--v-theme-surface), 0.78);
  box-shadow: 0 28px 60px rgba(18, 24, 34, 0.16);
  backdrop-filter: blur(26px);
  -webkit-backdrop-filter: blur(26px);
}

.unlock-screen__form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.unlock-screen__helper {
  background: rgba(var(--v-theme-surface-variant), 0.7);
  color: rgb(var(--v-theme-on-surface));
}

.unlock-screen__field {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 16px 18px 18px;
  border-radius: 24px;
  background: rgba(var(--v-theme-surface-variant), 0.68);
  transition:
    transform 260ms cubic-bezier(0.22, 1, 0.36, 1),
    box-shadow 220ms ease,
    background-color 220ms ease;
}

.unlock-screen__field:focus-within {
  transform: translateY(-1px);
  box-shadow: 0 0 0 1px rgba(var(--v-theme-primary), 0.18);
}

.unlock-screen__field--invalid {
  box-shadow: 0 0 0 1px rgba(var(--v-theme-error), 0.28);
}

.unlock-screen__field-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.unlock-screen__field-label {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  color: rgba(var(--v-theme-on-surface), 0.72);
  font-size: 0.92rem;
  font-weight: 600;
}

.unlock-screen__toggle {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 38px;
  height: 38px;
  border: none;
  border-radius: 999px;
  background: transparent;
  color: rgba(var(--v-theme-on-surface), 0.72);
  cursor: pointer;
  transition:
    background-color 180ms ease,
    color 180ms ease,
    transform 180ms ease;
}

.unlock-screen__toggle:hover {
  background: rgba(var(--v-theme-on-surface), 0.06);
}

.unlock-screen__toggle:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px rgba(var(--v-theme-primary), 0.14);
}

.unlock-screen__input {
  width: 100%;
  min-height: 34px;
  padding: 0;
  border: none;
  outline: none;
  background: transparent;
  color: rgb(var(--v-theme-on-surface));
  font: inherit;
  font-size: 1.08rem;
  line-height: 1.5;
}

.unlock-screen__input--masked {
  -webkit-text-security: disc;
}

.unlock-screen__input--secret {
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.unlock-screen__input::placeholder {
  color: rgba(var(--v-theme-on-surface), 0.42);
}

.unlock-screen__field--invalid .unlock-screen__field-label,
.unlock-screen__field--invalid .unlock-screen__toggle {
  color: rgb(var(--v-theme-error));
}

.unlock-screen__alert {
  border-radius: 20px;
}

.unlock-screen__actions {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-top: 4px;
}

.unlock-screen__primary {
  min-height: 58px;
  box-shadow: none;
}

.unlock-screen__biometric {
  min-height: 52px;
  color: rgba(var(--v-theme-on-surface), 0.78);
}

.unlock-screen-enter-active {
  transition: opacity 460ms ease;
}

.unlock-screen-leave-active {
  transition: opacity 0ms linear 860ms;
}

.unlock-screen-enter-active .unlock-screen__door,
.unlock-screen-leave-active .unlock-screen__door {
  transition:
    transform 860ms cubic-bezier(0.2, 0.95, 0.24, 1),
    opacity 640ms ease;
}

.unlock-screen-enter-active .unlock-screen__seam,
.unlock-screen-leave-active .unlock-screen__seam {
  transition:
    transform 860ms cubic-bezier(0.2, 0.95, 0.24, 1),
    opacity 620ms ease,
    filter 620ms ease;
}

.unlock-screen-enter-active .unlock-screen__shell,
.unlock-screen-leave-active .unlock-screen__shell {
  transition:
    transform 760ms cubic-bezier(0.22, 1, 0.36, 1),
    opacity 420ms ease,
    filter 560ms ease;
}

.unlock-screen-enter-from {
  opacity: 0;
}

.unlock-screen-enter-from .unlock-screen__door--left {
  transform: translateX(-114%) rotate(-3deg);
}

.unlock-screen-enter-from .unlock-screen__door--right {
  transform: translateX(114%) rotate(3deg);
}

.unlock-screen-enter-from .unlock-screen__seam {
  opacity: 0;
  filter: blur(10px);
  transform: translateX(-50%) scaleX(3.2) scaleY(0.4);
}

.unlock-screen-enter-from .unlock-screen__shell {
  opacity: 0;
  filter: blur(16px);
  transform: translateY(26px) scale(0.96);
}

.unlock-screen-enter-to .unlock-screen__door--left,
.unlock-screen-enter-to .unlock-screen__door--right {
  transform: translateX(0) rotate(0deg);
}

.unlock-screen-enter-to .unlock-screen__seam {
  opacity: 0.58;
  filter: blur(0);
  transform: translateX(-50%) scaleX(1) scaleY(1);
}

.unlock-screen-enter-to .unlock-screen__shell {
  opacity: 1;
  filter: blur(0);
  transform: translateY(0) scale(1);
}

.unlock-screen-leave-from {
  opacity: 1;
}

.unlock-screen-leave-from .unlock-screen__door--left,
.unlock-screen-leave-from .unlock-screen__door--right {
  transform: translateX(0) rotate(0deg);
}

.unlock-screen-leave-from .unlock-screen__seam {
  opacity: 0.42;
  filter: blur(0);
  transform: translateX(-50%) scaleX(1) scaleY(1);
}

.unlock-screen-leave-from .unlock-screen__shell {
  opacity: 1;
  filter: blur(0);
  transform: translateY(0) scale(1);
}

.unlock-screen-leave-to {
  opacity: 0;
}

.unlock-screen-leave-to .unlock-screen__door--left {
  transform: translateX(-122%) rotate(-5deg);
}

.unlock-screen-leave-to .unlock-screen__door--right {
  transform: translateX(122%) rotate(5deg);
}

.unlock-screen-leave-to .unlock-screen__seam {
  opacity: 0.96;
  filter: blur(8px);
  transform: translateX(-50%) scaleX(5.8) scaleY(0.48);
}

.unlock-screen-leave-to .unlock-screen__shell {
  opacity: 0;
  filter: blur(14px);
  transform: translateY(12px) scale(0.97);
}

.unlock-alert-enter-active,
.unlock-alert-leave-active {
  transition:
    opacity 220ms ease,
    transform 220ms ease;
}

.unlock-alert-enter-from,
.unlock-alert-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

:global(html[data-theme-mode="dark"]) .unlock-screen {
  background:
    radial-gradient(circle at 18% 18%, rgba(var(--v-theme-primary), 0.16), transparent 28%),
    radial-gradient(circle at 82% 14%, rgba(var(--v-theme-secondary), 0.08), transparent 24%),
    linear-gradient(180deg, rgba(10, 14, 20, 0.98), rgba(8, 11, 16, 1));
}

:global(html[data-theme-mode="dark"]) .unlock-screen__ambient {
  opacity: 0.68;
}

:global(html[data-theme-mode="dark"]) .unlock-screen__door {
  background:
    linear-gradient(180deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.94)),
    linear-gradient(90deg, rgba(255, 255, 255, 0.08), transparent);
  box-shadow: 0 36px 72px rgba(0, 0, 0, 0.32);
}

:global(html[data-theme-mode="dark"]) .unlock-screen__seam {
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.9), rgba(255, 255, 255, 0.14)),
    radial-gradient(circle at center, rgba(255, 255, 255, 0.84), transparent 70%);
  box-shadow: 0 0 30px rgba(255, 255, 255, 0.18);
}

:global(html[data-theme-mode="dark"]) .unlock-screen__badge {
  background: rgba(var(--v-theme-surface-variant), 0.86);
  box-shadow: 0 16px 34px rgba(0, 0, 0, 0.24);
}

:global(html[data-theme-mode="dark"]) .unlock-screen__panel {
  background: rgba(var(--v-theme-surface), 0.78);
  box-shadow: 0 30px 64px rgba(0, 0, 0, 0.34);
}

:global(html[data-theme-mode="dark"]) .unlock-screen__helper {
  background: rgba(var(--v-theme-surface-variant), 0.72);
}

:global(html[data-theme-mode="dark"]) .unlock-screen__field {
  background: rgba(var(--v-theme-surface-variant), 0.74);
}

:global(html[data-theme-mode="dark"]) .unlock-screen__field:focus-within {
  box-shadow: 0 0 0 1px rgba(var(--v-theme-primary), 0.24);
}

:global(html[data-theme-mode="dark"]) .unlock-screen__toggle:hover {
  background: rgba(255, 255, 255, 0.06);
}

@media (max-width: 640px) {
  .unlock-screen {
    align-items: stretch;
    justify-content: flex-start;
    padding:
      calc(var(--host-safe-top-effective) + 24px)
      18px
      calc(var(--host-safe-bottom-effective) + 24px);
  }

  .unlock-screen__shell {
    width: 100%;
    margin: auto 0;
    gap: 18px;
  }

  .unlock-screen__badge {
    width: 68px;
    height: 68px;
    border-radius: 24px;
  }

  .unlock-screen__panel {
    padding: 20px;
    border-radius: 26px;
  }

  .unlock-screen__field {
    padding: 15px 16px 16px;
    border-radius: 22px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .unlock-screen,
  .unlock-screen__door,
  .unlock-screen__shell,
  .unlock-screen__field,
  .unlock-screen__toggle {
    transition-duration: 0ms !important;
    animation: none !important;
  }
}
</style>
