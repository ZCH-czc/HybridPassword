<script setup>
import { computed } from "vue";
import InlineSvgIcon from "@/components/InlineSvgIcon.vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

const props = defineProps({
  modelValue: {
    type: String,
    default: "home",
  },
  alignment: {
    type: String,
    default: "center",
  },
  visible: {
    type: Boolean,
    default: true,
  },
});

const emit = defineEmits(["update:modelValue"]);
const { t } = useAppPreferences();

const items = computed(() => [
  {
    value: "home",
    label: t("nav.home"),
    icon: props.modelValue === "home" ? "mdi-home-variant" : "mdi-home-variant-outline",
  },
  {
    value: "list",
    label: t("nav.list"),
    icon: props.modelValue === "list" ? "mdi-format-list-bulleted-square" : "mdi-format-list-bulleted",
  },
  {
    value: "settings",
    label: t("nav.settings"),
    icon: props.modelValue === "settings" ? "mdi-cog" : "mdi-cog-outline",
  },
]);
</script>

<template>
  <div
    class="app-bottom-nav-shell"
    :class="[
      `app-bottom-nav-shell--${alignment || 'center'}`,
      { 'app-bottom-nav-shell--visible': visible },
    ]"
  >
    <div class="app-bottom-nav__stage">
      <div class="app-bottom-nav__launch-rail" aria-hidden="true"></div>

      <v-sheet
        tag="nav"
        class="app-bottom-nav"
        data-tour-target="bottom-nav"
        role="tablist"
        aria-label="Primary navigation"
      >
        <button
          v-for="item in items"
          :key="item.value"
          type="button"
          class="app-bottom-nav__button"
          :data-tour-target="`bottom-nav-${item.value}`"
          :class="{ 'app-bottom-nav__button--active': item.value === modelValue }"
          :aria-current="item.value === modelValue ? 'page' : undefined"
          @click="emit('update:modelValue', item.value)"
        >
          <span class="app-bottom-nav__capsule">
            <InlineSvgIcon class="app-bottom-nav__icon" :icon="item.icon" :size="22" />
            <span class="app-bottom-nav__label">{{ item.label }}</span>
          </span>
        </button>
      </v-sheet>
    </div>
  </div>
</template>

<style scoped>
.app-bottom-nav-shell {
  --vault-nav-width: min(calc(100vw - 124px), 280px);
  --vault-nav-left: 50%;
  --vault-nav-translate: -50%;
  position: fixed;
  bottom: calc(12px + var(--host-safe-bottom-effective));
  left: var(--vault-nav-left);
  width: var(--vault-nav-width);
  z-index: 30;
  pointer-events: none;
  transform: translateX(var(--vault-nav-translate));
  will-change: left, transform;
  transition:
    left 340ms cubic-bezier(0.22, 1, 0.36, 1),
    transform 340ms cubic-bezier(0.22, 1, 0.36, 1),
    bottom 280ms ease;
}

.app-bottom-nav-shell--left {
  --vault-nav-left: max(18px, calc(50% - 720px + 28px));
  --vault-nav-translate: 0px;
}

.app-bottom-nav-shell--right {
  --vault-nav-left: calc(100% - max(18px, calc(50% - 720px + 28px)) - var(--vault-nav-width));
  --vault-nav-translate: 0px;
}

.app-bottom-nav-shell::before {
  content: "";
  position: absolute;
  inset: 10px 12px -10px;
  border-radius: 999px;
  background: rgba(16, 22, 30, 0.18);
  filter: blur(22px);
  opacity: 0.72;
  pointer-events: none;
}

.app-bottom-nav__stage {
  position: relative;
  transform: translateY(0);
  opacity: 1;
  filter: blur(0);
  will-change: transform, opacity, filter;
  transition:
    transform 420ms cubic-bezier(0.22, 1, 0.36, 1),
    opacity 320ms ease,
    filter 320ms ease;
}

.app-bottom-nav-shell:not(.app-bottom-nav-shell--visible) .app-bottom-nav__stage {
  transform: translateY(26px);
  opacity: 0;
  filter: blur(12px);
}

.app-bottom-nav-shell--visible .app-bottom-nav__stage {
  animation: vault-nav-stage-rise 780ms cubic-bezier(0.18, 1.12, 0.28, 1) both;
}

.app-bottom-nav__launch-rail {
  position: absolute;
  left: 50%;
  bottom: 28px;
  width: 62px;
  height: 8px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.98);
  box-shadow: 0 12px 22px rgba(14, 18, 28, 0.12);
  opacity: 0;
  transform: translateX(-50%) translateY(10px) scaleX(0.14);
  pointer-events: none;
}

.app-bottom-nav {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 2px;
  height: 84px;
  padding: 9px 10px;
  border-radius: 999px !important;
  overflow: hidden;
  background: var(--vault-nav-bg);
  border: none;
  backdrop-filter: blur(26px) saturate(115%);
  -webkit-backdrop-filter: blur(26px) saturate(115%);
  box-shadow: var(--vault-nav-shadow);
  pointer-events: auto;
  transform-origin: center bottom;
  transition:
    transform 680ms cubic-bezier(0.18, 1.2, 0.28, 1),
    background-color 360ms ease,
    box-shadow 360ms ease,
    opacity 260ms ease,
    backdrop-filter 320ms ease;
}

.app-bottom-nav__button {
  position: relative;
  z-index: 1;
  flex: 1 1 0;
  display: flex;
  justify-content: center;
  padding: 0;
  border: none;
  background: transparent;
  appearance: none;
  font: inherit;
  min-width: 0;
  color: rgba(var(--v-theme-on-surface), 0.72);
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
  transition:
    transform 220ms ease,
    color 220ms ease,
    opacity 220ms ease;
}

.app-bottom-nav-shell:not(.app-bottom-nav-shell--visible) .app-bottom-nav {
  transform: translateY(14px) scaleX(0.2) scaleY(0.14);
  background: rgba(255, 255, 255, 0.96);
  box-shadow: 0 18px 26px rgba(14, 18, 28, 0.12);
}

.app-bottom-nav-shell--visible .app-bottom-nav__launch-rail {
  animation: vault-nav-rail-pop 560ms cubic-bezier(0.18, 1.08, 0.28, 1) both;
}

.app-bottom-nav-shell--visible .app-bottom-nav {
  animation: vault-nav-pop 720ms cubic-bezier(0.18, 1.15, 0.28, 1) both;
}

.app-bottom-nav-shell:not(.app-bottom-nav-shell--visible) .app-bottom-nav__button {
  opacity: 0;
  transform: translateY(8px) scale(0.94);
}

.app-bottom-nav__button:hover {
  color: rgba(var(--v-theme-on-surface), 0.92);
}

.app-bottom-nav__button:focus-visible {
  outline: none;
}

.app-bottom-nav__capsule {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
  width: 70px;
  min-height: 58px;
  padding: 9px 8px 8px;
  border-radius: 20px;
  transform: translate3d(0, 0, 0);
  will-change: transform;
  transition:
    transform 260ms cubic-bezier(0.22, 1, 0.36, 1),
    background-color 260ms ease,
    box-shadow 260ms ease,
    border-color 260ms ease,
    color 260ms ease;
}

.app-bottom-nav__button:focus-visible .app-bottom-nav__capsule {
  box-shadow: 0 0 0 3px rgba(var(--v-theme-primary), 0.14);
}

.app-bottom-nav__button--active .app-bottom-nav__capsule {
  transform: translate3d(0, -1px, 0);
  color: rgb(var(--v-theme-on-surface));
  background: var(--vault-nav-active-bg);
  box-shadow: none;
}

.app-bottom-nav__icon,
.app-bottom-nav__label {
  position: relative;
  z-index: 1;
  color: inherit;
  text-shadow: none;
  filter: none;
}

.app-bottom-nav__icon {
  font-size: 22px;
}

.app-bottom-nav__label {
  font-size: 13px;
  font-weight: 600;
  line-height: 1;
  letter-spacing: 0.01em;
  white-space: nowrap;
}

:global(.v-theme--dark) .app-bottom-nav-shell::before {
  background: rgba(0, 0, 0, 0.42);
}

:global(.v-theme--dark) .app-bottom-nav-shell:not(.app-bottom-nav-shell--visible) .app-bottom-nav {
  background: rgba(248, 250, 252, 0.96);
  box-shadow: 0 18px 30px rgba(0, 0, 0, 0.24);
}

:global(.v-theme--dark) .app-bottom-nav__launch-rail {
  background: rgba(248, 250, 252, 0.98);
  box-shadow: 0 12px 24px rgba(0, 0, 0, 0.22);
}

:global(.v-theme--dark) .app-bottom-nav {
  background: var(--vault-nav-bg);
}

:global(.v-theme--dark) .app-bottom-nav__button--active .app-bottom-nav__capsule {
  color: rgb(var(--v-theme-on-surface));
  background: var(--vault-nav-active-bg);
  box-shadow: none;
}

@media (max-width: 599px) {
  .app-bottom-nav-shell {
    --vault-nav-width: min(calc(100vw - 104px), 258px);
    bottom: calc(10px + var(--host-safe-bottom-effective));
  }

  .app-bottom-nav-shell--left {
    --vault-nav-left: 16px;
  }

  .app-bottom-nav-shell--right {
    --vault-nav-left: calc(100% - 16px - var(--vault-nav-width));
  }

  .app-bottom-nav {
    height: 80px;
    padding: 8px 9px;
  }

  .app-bottom-nav__capsule {
    width: 66px;
    min-height: 54px;
    padding-top: 8px;
  }

  .app-bottom-nav__icon {
    font-size: 21px;
  }
}

@keyframes vault-nav-stage-rise {
  0% {
    opacity: 0;
    filter: blur(12px);
    transform: translateY(28px);
  }

  38% {
    opacity: 1;
    filter: blur(0);
    transform: translateY(-4px);
  }

  100% {
    opacity: 1;
    filter: blur(0);
    transform: translateY(0);
  }
}

@keyframes vault-nav-pop {
  0% {
    transform: translateY(18px) scaleX(0.18) scaleY(0.12);
    opacity: 0;
    background: rgba(255, 255, 255, 0.98);
  }

  20% {
    transform: translateY(12px) scaleX(0.28) scaleY(0.18);
    opacity: 1;
    background: rgba(255, 255, 255, 0.98);
  }

  58% {
    transform: translateY(-2px) scaleX(1.02) scaleY(1.02);
    opacity: 1;
    background: var(--vault-nav-bg);
  }

  100% {
    transform: translateY(0) scaleX(1) scaleY(1);
    opacity: 1;
    background: var(--vault-nav-bg);
  }
}

@keyframes vault-nav-rail-pop {
  0% {
    opacity: 0;
    transform: translateX(-50%) translateY(14px) scaleX(0.12);
  }

  26% {
    opacity: 1;
    transform: translateX(-50%) translateY(10px) scaleX(0.18);
  }

  56% {
    opacity: 1;
    transform: translateX(-50%) translateY(2px) scaleX(1);
  }

  100% {
    opacity: 0;
    transform: translateX(-50%) translateY(-4px) scaleX(1.55);
  }
}
</style>
