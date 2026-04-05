<script setup>
import { ref } from "vue";

defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["update:modelValue", "complete"]);
const step = ref(0);

const slides = [
  {
    title: "欢迎来到你的密码库",
    body: "主页展示概览、收藏夹和最近项目，适合快速进入日常使用的密码。",
    icon: "mdi-home-variant-outline",
  },
  {
    title: "列表页管理全部项目",
    body: "你可以在列表页查看全部、收藏夹和最近删除，也可以直接恢复误删记录。",
    icon: "mdi-format-list-bulleted",
  },
  {
    title: "设置页负责外观与安全",
    body: "深色模式、导入导出和修改主密码都集中在设置页，主界面会更干净。",
    icon: "mdi-cog-outline",
  },
];

function nextStep() {
  if (step.value >= slides.length - 1) {
    emit("complete");
    emit("update:modelValue", false);
    step.value = 0;
    return;
  }

  step.value += 1;
}

function skipGuide() {
  emit("complete");
  emit("update:modelValue", false);
  step.value = 0;
}
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="620"
    persistent
    transition="dialog-transition"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="onboarding-card">
      <v-card-text class="pa-6 pa-sm-7">
        <div class="d-flex align-center justify-space-between mb-6">
          <v-chip color="primary" variant="tonal">新手指引</v-chip>
          <div class="text-caption text-medium-emphasis">{{ step + 1 }}/{{ slides.length }}</div>
        </div>

        <v-window v-model="step" class="overflow-hidden">
          <v-window-item
            v-for="(slide, index) in slides"
            :key="slide.title"
            :value="index"
          >
            <div class="d-flex flex-column align-center text-center py-4">
              <v-avatar color="primary" variant="tonal" size="68">
                <v-icon size="34">{{ slide.icon }}</v-icon>
              </v-avatar>
              <div class="text-h5 font-weight-medium mt-5">{{ slide.title }}</div>
              <div class="text-body-1 text-medium-emphasis mt-3 onboarding-body">
                {{ slide.body }}
              </div>
            </div>
          </v-window-item>
        </v-window>

        <div class="d-flex align-center justify-space-between mt-4">
          <v-btn variant="text" @click="skipGuide">跳过</v-btn>
          <v-btn color="primary" @click="nextStep">
            {{ step === slides.length - 1 ? "开始使用" : "下一步" }}
          </v-btn>
        </div>
      </v-card-text>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.onboarding-card {
  background:
    linear-gradient(
      180deg,
      rgba(var(--v-theme-surface), 0.98),
      rgba(var(--v-theme-surface), 0.92)
    ),
    radial-gradient(circle at top, rgba(26, 115, 232, 0.1), transparent 40%);
}

.onboarding-body {
  max-width: 420px;
  line-height: 1.7;
}
</style>
