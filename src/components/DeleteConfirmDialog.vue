<script setup>
import { computed } from "vue";

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  title: {
    type: String,
    default: "",
  },
  count: {
    type: Number,
    default: 1,
  },
  loading: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["update:modelValue", "confirm"]);

const targetLabel = computed(() => {
  if (props.count > 1) {
    return `${props.count}条密码记录`;
  }

  return props.title || "该密码记录";
});
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="420"
    transition="dialog-bottom-transition"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="border-sm">
      <v-card-text class="pa-6">
        <div class="text-h6 font-weight-medium">移入最近删除</div>
        <div class="text-body-2 text-medium-emphasis mt-3">
          你即将把
          <span class="font-weight-medium">{{ targetLabel }}</span>
          移入最近删除，之后仍然可以在列表或设置中的最近删除里恢复。
        </div>
      </v-card-text>

      <v-card-actions class="px-6 pb-6">
        <v-spacer />
        <v-btn variant="text" @click="emit('update:modelValue', false)">取消</v-btn>
        <v-btn color="error" :loading="loading" @click="emit('confirm')">移入最近删除</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
