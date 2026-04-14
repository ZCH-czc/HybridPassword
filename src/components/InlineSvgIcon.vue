<script setup>
import {
  mdiCellphoneCog,
  mdiChevronLeft,
  mdiChevronRight,
  mdiCog,
  mdiCogOutline,
  mdiCodeTags,
  mdiDatabaseOutline,
  mdiDeleteOutline,
  mdiEyeOffOutline,
  mdiEyeOutline,
  mdiFormatListBulleted,
  mdiFormatListBulletedSquare,
  mdiGithub,
  mdiHomeVariant,
  mdiHomeVariantOutline,
  mdiInformationOutline,
  mdiKeyChainVariant,
  mdiMicrosoftWindows,
  mdiOpenInNew,
  mdiPaletteOutline,
  mdiPencilOutline,
  mdiShieldLockOutline,
  mdiStar,
  mdiStarOutline,
  mdiSync,
} from "@mdi/js";
import { computed } from "vue";

const props = defineProps({
  icon: {
    type: String,
    required: true,
  },
  size: {
    type: [Number, String],
    default: 20,
  },
  title: {
    type: String,
    default: "",
  },
});

const iconPaths = {
  "mdi-cellphone-cog": mdiCellphoneCog,
  "mdi-chevron-left": mdiChevronLeft,
  "mdi-chevron-right": mdiChevronRight,
  "mdi-cog": mdiCog,
  "mdi-cog-outline": mdiCogOutline,
  "mdi-code-tags": mdiCodeTags,
  "mdi-database-outline": mdiDatabaseOutline,
  "mdi-delete-outline": mdiDeleteOutline,
  "mdi-eye-off-outline": mdiEyeOffOutline,
  "mdi-eye-outline": mdiEyeOutline,
  "mdi-format-list-bulleted": mdiFormatListBulleted,
  "mdi-format-list-bulleted-square": mdiFormatListBulletedSquare,
  "mdi-github": mdiGithub,
  "mdi-home-variant": mdiHomeVariant,
  "mdi-home-variant-outline": mdiHomeVariantOutline,
  "mdi-information-outline": mdiInformationOutline,
  "mdi-key-chain-variant": mdiKeyChainVariant,
  "mdi-microsoft-windows": mdiMicrosoftWindows,
  "mdi-open-in-new": mdiOpenInNew,
  "mdi-palette-outline": mdiPaletteOutline,
  "mdi-pencil-outline": mdiPencilOutline,
  "mdi-shield-lock-outline": mdiShieldLockOutline,
  "mdi-star": mdiStar,
  "mdi-star-outline": mdiStarOutline,
  "mdi-sync": mdiSync,
};

const iconPath = computed(() => iconPaths[props.icon] || mdiStarOutline);
const iconSize = computed(() =>
  typeof props.size === "number" ? `${props.size}px` : String(props.size)
);
</script>

<template>
  <svg
    class="inline-svg-icon"
    :width="iconSize"
    :height="iconSize"
    viewBox="0 0 24 24"
    fill="currentColor"
    aria-hidden="true"
    xmlns="http://www.w3.org/2000/svg"
  >
    <title v-if="title">{{ title }}</title>
    <path :d="iconPath" />
  </svg>
</template>

<style scoped>
.inline-svg-icon {
  display: inline-block;
  vertical-align: middle;
  flex-shrink: 0;
  color: currentColor;
  fill: currentColor;
}
</style>
