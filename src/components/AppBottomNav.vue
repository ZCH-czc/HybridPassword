<script setup>
import { computed } from "vue";
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
    :class="`app-bottom-nav-shell--${alignment || 'center'}`"
  >
    <v-sheet
      tag="nav"
      class="app-bottom-nav"
      role="tablist"
      aria-label="Primary navigation"
    >
      <button
        v-for="item in items"
        :key="item.value"
        type="button"
        class="app-bottom-nav__button"
        :class="{ 'app-bottom-nav__button--active': item.value === modelValue }"
        :aria-current="item.value === modelValue ? 'page' : undefined"
        @click="emit('update:modelValue', item.value)"
      >
        <span class="app-bottom-nav__capsule">
          <v-icon class="app-bottom-nav__icon">{{ item.icon }}</v-icon>
          <span class="app-bottom-nav__label">{{ item.label }}</span>
        </span>
      </button>
    </v-sheet>
  </div>
</template>

<style scoped>
.app-bottom-nav-shell {
  position: fixed;
  bottom: calc(10px + var(--host-safe-bottom-effective));
  width: min(calc(100vw - 144px), 286px);
  z-index: 30;
  pointer-events: none;
}

.app-bottom-nav-shell--center {
  left: 50%;
  transform: translateX(-50%);
}

.app-bottom-nav-shell--left {
  left: max(18px, calc(50% - 720px + 24px));
}

.app-bottom-nav-shell--right {
  right: max(18px, calc(50% - 720px + 24px));
}

.app-bottom-nav-shell::before {
  content: "";
  position: absolute;
  left: 22px;
  right: 22px;
  bottom: -6px;
  height: 34px;
  border-radius: 999px;
  background:
    radial-gradient(
      ellipse at center,
      rgba(30, 38, 52, 0.18) 0%,
      rgba(30, 38, 52, 0.1) 38%,
      transparent 78%
    );
  filter: blur(18px);
  opacity: 0.84;
  pointer-events: none;
}

.app-bottom-nav {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 6px;
  height: 90px;
  padding: 10px 10px 12px;
  border-radius: calc(var(--vault-radius) + 8px) !important;
  overflow: hidden;
  background: rgba(var(--v-theme-surface), 0.58);
  border: none;
  backdrop-filter: blur(30px) saturate(145%);
  -webkit-backdrop-filter: blur(30px) saturate(145%);
  box-shadow:
    0 24px 42px rgba(18, 28, 42, 0.14),
    0 8px 16px rgba(18, 28, 42, 0.06);
  pointer-events: auto;
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
  transition:
    transform 220ms ease,
    color 220ms ease,
    opacity 220ms ease;
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
  gap: 4px;
  width: 74px;
  min-height: 58px;
  padding: 10px 8px 8px;
  border-radius: calc(var(--vault-radius) + 4px);
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
  transform: translateY(-2px);
  color: rgb(var(--v-theme-primary));
  background: rgba(255, 255, 255, 0.4);
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  box-shadow:
    0 8px 18px rgba(17, 26, 38, 0.08);
}

.app-bottom-nav__button--active .app-bottom-nav__capsule::after {
  content: "";
  position: absolute;
  left: 14px;
  right: 14px;
  bottom: -10px;
  height: 14px;
  border-radius: 999px;
  background:
    radial-gradient(
      ellipse at center,
      rgba(255, 255, 255, 0.48),
      rgba(var(--v-theme-primary), 0.14) 52%,
      transparent 76%
    );
  filter: blur(10px);
  pointer-events: none;
}

.app-bottom-nav__icon,
.app-bottom-nav__label {
  position: relative;
  z-index: 1;
}

.app-bottom-nav__icon {
  font-size: 22px;
}

.app-bottom-nav__label {
  font-size: 12.5px;
  font-weight: 600;
  line-height: 1;
  letter-spacing: 0.01em;
  white-space: nowrap;
}

:global(.v-theme--dark) .app-bottom-nav-shell::before {
  background:
    radial-gradient(
      ellipse at center,
      rgba(0, 0, 0, 0.48) 0%,
      rgba(0, 0, 0, 0.28) 38%,
      transparent 80%
    );
}

:global(.v-theme--dark) .app-bottom-nav {
  background: rgba(var(--v-theme-surface), 0.52);
}

:global(.v-theme--dark) .app-bottom-nav__button--active .app-bottom-nav__capsule {
  background: rgba(255, 255, 255, 0.12);
}

@media (max-width: 599px) {
  .app-bottom-nav-shell {
    width: min(calc(100vw - 108px), 268px);
    bottom: calc(8px + var(--host-safe-bottom-effective));
  }

  .app-bottom-nav-shell--left {
    left: 16px;
  }

  .app-bottom-nav-shell--right {
    right: 16px;
  }

  .app-bottom-nav {
    height: 86px;
    padding: 9px 9px 11px;
  }

  .app-bottom-nav__capsule {
    width: 70px;
    min-height: 54px;
    padding-top: 9px;
  }

  .app-bottom-nav__icon {
    font-size: 21px;
  }
}
</style>
