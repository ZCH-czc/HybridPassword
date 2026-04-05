<script setup>
defineProps({
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
});

const emit = defineEmits(["update:modelValue"]);
</script>

<template>
  <v-snackbar
    :model-value="modelValue"
    :color="color"
    timeout="2600"
    location="bottom right"
    @update:model-value="emit('update:modelValue', $event)"
  >
    {{ text }}
  </v-snackbar>
</template>
