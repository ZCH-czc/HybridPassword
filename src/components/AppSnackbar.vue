<script setup>
import { computed, onBeforeUnmount, watch } from "vue";

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  text: {
    type: String,
    default: "",
  },
  color: {
    type: String,
    default: "success",
  },
  alignment: {
    type: String,
    default: "center",
  },
});

const emit = defineEmits(["update:modelValue"]);

const toneClass = computed(() => `vault-toast-body--${props.color || "success"}`);
let dismissTimer = null;

function clearDismissTimer() {
  if (dismissTimer) {
    clearTimeout(dismissTimer);
    dismissTimer = null;
  }
}

function scheduleDismiss() {
  clearDismissTimer();

  if (!props.modelValue || !props.text) {
    return;
  }

  dismissTimer = setTimeout(() => {
    emit("update:modelValue", false);
    dismissTimer = null;
  }, 2600);
}

watch(
  () => [props.modelValue, props.text],
  () => {
    scheduleDismiss();
  },
  { immediate: true }
);

onBeforeUnmount(() => {
  clearDismissTimer();
});
</script>

<template>
  <Teleport to="body">
    <Transition name="vault-toast">
      <div
        v-if="modelValue && text"
        class="vault-toast-shell"
        :class="`vault-toast-shell--${alignment || 'center'}`"
        aria-live="polite"
        aria-atomic="true"
      >
        <div class="vault-toast-body" :class="toneClass">
          <span class="vault-toast-body__dot" />
          <span class="vault-toast-body__text">{{ text }}</span>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.vault-toast-shell {
  position: fixed;
  left: 50%;
  bottom: calc(var(--vault-bottom-nav-height) - 26px + var(--host-safe-bottom-effective));
  width: min(520px, calc(100vw - 36px));
  z-index: 120;
  transform: translateX(-50%);
  transform-origin: center bottom;
  background: transparent !important;
  box-shadow: none !important;
  border: none !important;
  outline: none !important;
  backdrop-filter: none !important;
  -webkit-backdrop-filter: none !important;
  pointer-events: none;
}

.vault-toast-shell--left {
  left: max(18px, calc(50% - 720px + 28px));
  transform: none;
  transform-origin: left bottom;
}

.vault-toast-shell--right {
  left: auto;
  right: max(18px, calc(50% - 720px + 28px));
  transform: none;
  transform-origin: right bottom;
}

.vault-toast-body {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 52px;
  padding: 0 18px;
  border-radius: calc(var(--vault-radius) + 2px);
  background: var(--vault-toast-bg);
  box-shadow: var(--vault-shadow-soft);
}

.vault-toast-body__dot {
  width: 9px;
  height: 9px;
  border-radius: 999px;
  background: rgb(var(--v-theme-success));
  flex-shrink: 0;
}

.vault-toast-body__text {
  font-size: 0.94rem;
  font-weight: 600;
  color: rgba(var(--v-theme-on-surface), 0.86);
}

.vault-toast-body--error .vault-toast-body__dot {
  background: rgb(var(--v-theme-error));
}

.vault-toast-body--warning .vault-toast-body__dot {
  background: rgb(var(--v-theme-warning));
}

.vault-toast-body--info .vault-toast-body__dot {
  background: rgb(var(--v-theme-primary));
}

.vault-toast-body--success .vault-toast-body__dot {
  background: rgb(var(--v-theme-success));
}

.vault-toast-enter-active,
.vault-toast-leave-active {
  transition:
    opacity 220ms ease,
    transform 320ms cubic-bezier(0.22, 1, 0.36, 1),
    filter 280ms ease;
}

.vault-toast-enter-from,
.vault-toast-leave-to {
  opacity: 0;
  filter: blur(10px);
}

.vault-toast-shell--center.vault-toast-enter-from,
.vault-toast-shell--center.vault-toast-leave-to {
  transform: translateX(-50%) translateY(22px) scale(0.9);
}

.vault-toast-shell--left.vault-toast-enter-from,
.vault-toast-shell--left.vault-toast-leave-to {
  transform: translateY(22px) scale(0.9);
}

.vault-toast-shell--right.vault-toast-enter-from,
.vault-toast-shell--right.vault-toast-leave-to {
  transform: translateY(22px) scale(0.9);
}

@media (max-width: 599px) {
  .vault-toast-shell {
    bottom: calc(var(--vault-bottom-nav-height) - 20px + var(--host-safe-bottom-effective));
    width: min(calc(100vw - 28px), 460px);
  }

  .vault-toast-shell--left {
    left: 14px;
  }

  .vault-toast-shell--right {
    right: 14px;
  }

  .vault-toast-body {
    width: 100%;
    min-height: 50px;
    padding: 0 16px;
  }
}
</style>
