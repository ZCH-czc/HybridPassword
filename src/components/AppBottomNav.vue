<script setup>
defineProps({
  modelValue: {
    type: String,
    default: "home",
  },
});

const emit = defineEmits(["update:modelValue"]);
</script>

<template>
  <v-bottom-navigation
    :model-value="modelValue"
    grow
    color="primary"
    class="app-bottom-nav border-sm"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-btn value="home">
      <v-icon>mdi-home-variant-outline</v-icon>
      <span>主页</span>
    </v-btn>

    <v-btn value="list">
      <v-icon>mdi-format-list-bulleted</v-icon>
      <span>列表</span>
    </v-btn>

    <v-btn value="settings">
      <v-icon>mdi-cog-outline</v-icon>
      <span>设置</span>
    </v-btn>
  </v-bottom-navigation>
</template>

<style scoped>
.app-bottom-nav {
  position: fixed;
  inset-inline: 0;
  bottom: 0;
  width: 100%;
  height: calc(68px + var(--host-safe-bottom));
  padding-bottom: var(--host-safe-bottom);
  border-radius: 0 !important;
  overflow: hidden;
  background: rgba(var(--v-theme-surface), 0.9);
  backdrop-filter: blur(18px);
  -webkit-backdrop-filter: blur(18px);
  box-shadow: var(--vault-shadow);
  z-index: 30;
}
</style>
