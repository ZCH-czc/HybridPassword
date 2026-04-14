<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from "vue";

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  step: {
    type: String,
    default: "create",
  },
  locale: {
    type: String,
    default: "zh-CN",
  },
});

const emit = defineEmits(["update:modelValue", "update:step", "complete", "skip"]);

const stepOrder = ["create", "summary", "nav", "search", "settings"];
const stepSelectors = {
  create: '[data-tour-target="create-password"]',
  summary: '[data-tour-target="home-summary"]',
  nav: '[data-tour-target="bottom-nav"]',
  search: '[data-tour-target="global-search"]',
  settings: '[data-tour-target="settings-sections"]',
};

const focusPaddingMap = {
  create: { x: 1, y: 1 },
  summary: { x: 6, y: 6 },
  nav: { x: 8, y: 8 },
  search: { x: 4, y: 4 },
  settings: { x: 6, y: 6 },
};

const rectState = reactive({
  top: 0,
  left: 0,
  width: 0,
  height: 0,
  radius: 22,
  ready: false,
});

const viewport = reactive({
  width: 0,
  height: 0,
});

const cardSize = reactive({
  width: 360,
  height: 240,
});

const cardRef = ref(null);
const confettiCanvasRef = ref(null);
const prefersReducedMotion = ref(false);

let pendingMeasureTimer = null;
let confettiRaf = 0;
let lastFrameTime = 0;
let confettiParticles = [];

const isZh = computed(() => String(props.locale || "").toLowerCase().startsWith("zh"));
const currentStepIndex = computed(() => Math.max(0, stepOrder.indexOf(props.step)));
const isLastStep = computed(() => currentStepIndex.value === stepOrder.length - 1);
const focusPadding = computed(() => focusPaddingMap[props.step] || { x: 8, y: 8 });

const stepCopy = computed(() => {
  if (isZh.value) {
    return {
      create: {
        title: "先从新建开始",
        body: "点这里可以保存第一条登录信息。网站、用户名、密码和多条备注都能放进去。",
      },
      summary: {
        title: "这里是你的概览卡片",
        body: "它会告诉你已经保存了多少项目、当前筛选出来多少结果，以及一共有多少条备注。",
      },
      nav: {
        title: "底部栏负责切换区域",
        body: "主页看概览，列表找密码，设置改外观、安全和同步。后面你也可以在设置里调整它靠左、居中或靠右。",
      },
      search: {
        title: "从这里快速找到密码",
        body: "进入列表后，可以搜索网站、用户名和备注，结果会实时更新，而且不区分大小写。",
      },
      settings: {
        title: "设置都集中在这里",
        body: "外观、安全、同步和数据管理都放在这里。以后想改主密码、查看 Secret Key 或配置同步，都从这里进入。",
      },
      next: "下一步",
      done: "开始使用",
      skip: "跳过引导",
      back: "返回",
    };
  }

  return {
    create: {
      title: "Start with a new item",
      body: "Tap here to save your first login. Site, username, password, and multiple notes can all live together.",
    },
    summary: {
      title: "These cards are your overview",
      body: "They show how many items are saved, how many match the current filter, and how many notes exist in total.",
    },
    nav: {
      title: "The bottom bar switches areas",
      body: "Home shows your overview, List helps you find passwords, and Settings controls appearance, security, and sync.",
    },
    search: {
      title: "Find saved passwords fast",
      body: "On the list page you can search sites, usernames, and notes. Results update instantly and ignore letter case.",
    },
    settings: {
      title: "Everything is grouped in Settings",
      body: "Appearance, security, sync, and data tools all live here. This is where you change the master password or reveal the Secret Key later.",
    },
    next: "Next",
    done: "Start using it",
    skip: "Skip guide",
    back: "Back",
  };
});

const currentStepData = computed(() => stepCopy.value[props.step] || stepCopy.value.create);

function clamp(value, min, max) {
  return Math.min(Math.max(value, min), max);
}

function parseRadius(value, fallback) {
  const segment = String(value || "").split(" ")[0];
  const parsed = Number.parseFloat(segment);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function roundRectValue(value) {
  return Math.round(Number(value || 0) * 10) / 10;
}

function getSafeBottomPx() {
  const rawValue = getComputedStyle(document.documentElement)
    .getPropertyValue("--host-safe-bottom-effective")
    .trim();
  const parsed = Number.parseFloat(rawValue);
  return Number.isFinite(parsed) ? parsed : 0;
}

function readTargetElement() {
  const selector = stepSelectors[props.step];
  return selector ? document.querySelector(selector) : null;
}

function updateViewport() {
  const visualViewport = window.visualViewport;
  viewport.width = Math.round(visualViewport?.width || window.innerWidth || 0);
  viewport.height = Math.round(visualViewport?.height || window.innerHeight || 0);
  resizeCanvas();
}

const focusStyle = computed(() => {
  const padX = focusPadding.value.x;
  const padY = focusPadding.value.y;
  const height = rectState.height + padY * 2;
  const radius = clamp(rectState.radius + padY, 16, Math.max(16, height / 2));

  return {
    top: `${rectState.top - padY}px`,
    left: `${rectState.left - padX}px`,
    width: `${rectState.width + padX * 2}px`,
    height: `${height}px`,
    borderRadius: `${radius}px`,
  };
});

const cardStyle = computed(() => {
  const marginX = viewport.width <= 460 ? 12 : 18;
  const marginY = viewport.height <= 760 ? 12 : 18;
  const availableWidth = Math.max(220, viewport.width - marginX * 2);
  const width = Math.min(408, availableWidth);
  const measuredHeight = Math.max(208, Math.round(cardSize.height || 240));
  const maxHeight = Math.max(190, viewport.height - marginY * 2);
  const height = Math.min(measuredHeight, maxHeight);

  if (!rectState.ready || !viewport.width || !viewport.height) {
    return {
      left: `${marginX}px`,
      right: `${marginX}px`,
      top: "auto",
      bottom: `calc(${marginY}px + var(--host-safe-bottom-effective))`,
      width: "auto",
      maxHeight: `calc(100vh - ${marginY * 2}px - var(--host-safe-top-effective) - var(--host-safe-bottom-effective))`,
      transform: "none",
    };
  }

  const centerX = rectState.left + rectState.width / 2;
  const belowTop = rectState.top + rectState.height + 18;
  const aboveTop = rectState.top - height - 18;
  const preferAbove = rectState.top + rectState.height / 2 > viewport.height * 0.55;
  const left = clamp(centerX - width / 2, marginX, viewport.width - width - marginX);
  let top = preferAbove ? aboveTop : belowTop;

  if (preferAbove && top < marginY) {
    top = belowTop;
  }

  if (!preferAbove && top + height > viewport.height - marginY) {
    top = aboveTop;
  }

  top = clamp(top, marginY, viewport.height - height - marginY);

  return {
    left: `${left}px`,
    right: "auto",
    top: `${top}px`,
    bottom: "auto",
    width: `${width}px`,
    maxHeight: `${maxHeight}px`,
    transform: "none",
  };
});

async function measureCard() {
  await nextTick();
  const element = cardRef.value;
  if (!element) {
    return;
  }

  const rect = element.getBoundingClientRect();
  if (rect.width > 0) {
    cardSize.width = rect.width;
  }
  if (rect.height > 0) {
    cardSize.height = rect.height;
  }
}

async function updateRect() {
  await nextTick();

  if (pendingMeasureTimer) {
    clearTimeout(pendingMeasureTimer);
    pendingMeasureTimer = null;
  }

  const target = readTargetElement();
  if (!target) {
    rectState.ready = false;
    pendingMeasureTimer = window.setTimeout(() => {
      pendingMeasureTimer = null;
      if (props.modelValue) {
        void updateRect();
      }
    }, 180);
    return;
  }

  const rect = target.getBoundingClientRect();
  const style = window.getComputedStyle(target);
  const borderRadius =
    style.borderRadius ||
    style.borderTopLeftRadius ||
    style.borderTopRightRadius ||
    style.borderBottomLeftRadius ||
    style.borderBottomRightRadius;

  rectState.top = roundRectValue(rect.top);
  rectState.left = roundRectValue(rect.left);
  rectState.width = roundRectValue(rect.width);
  rectState.height = roundRectValue(rect.height);
  rectState.radius = parseRadius(borderRadius, Math.min(rect.height / 2, 24));
  rectState.ready = rect.width > 0 && rect.height > 0;

  await measureCard();
}

function resizeCanvas() {
  const canvas = confettiCanvasRef.value;
  if (!canvas || !viewport.width || !viewport.height) {
    return;
  }

  const ratio = Math.max(1, window.devicePixelRatio || 1);
  canvas.width = Math.round(viewport.width * ratio);
  canvas.height = Math.round(viewport.height * ratio);
  canvas.style.width = `${viewport.width}px`;
  canvas.style.height = `${viewport.height}px`;

  const context = canvas.getContext("2d");
  if (!context) {
    return;
  }

  context.setTransform(ratio, 0, 0, ratio, 0, 0);
}

function stopConfetti() {
  if (confettiRaf) {
    cancelAnimationFrame(confettiRaf);
    confettiRaf = 0;
  }

  lastFrameTime = 0;
  confettiParticles = [];

  const canvas = confettiCanvasRef.value;
  const context = canvas?.getContext("2d");
  if (canvas && context && viewport.width && viewport.height) {
    context.clearRect(0, 0, viewport.width, viewport.height);
  }
}

function drawParticle(context, particle) {
  context.save();
  context.translate(particle.x, particle.y);
  context.rotate(particle.rotation);
  context.globalAlpha = particle.alpha;
  context.fillStyle = particle.color;

  if (particle.shape === "dot") {
    context.beginPath();
    context.arc(0, 0, particle.size * 0.5, 0, Math.PI * 2);
    context.fill();
  } else {
    const width = particle.shape === "streamer" ? particle.size * 0.34 : particle.size;
    const height = particle.shape === "streamer" ? particle.size * 2.6 : particle.size * 0.7;
    context.fillRect(-width / 2, -height / 2, width, height);
  }

  context.restore();
}

function renderConfetti(timestamp) {
  const canvas = confettiCanvasRef.value;
  const context = canvas?.getContext("2d");
  if (!canvas || !context || !viewport.width || !viewport.height) {
    stopConfetti();
    return;
  }

  if (!lastFrameTime) {
    lastFrameTime = timestamp;
  }

  const delta = Math.min(0.032, (timestamp - lastFrameTime) / 1000);
  lastFrameTime = timestamp;
  context.clearRect(0, 0, viewport.width, viewport.height);

  confettiParticles = confettiParticles.filter((particle) => {
    particle.life += delta;
    particle.velocityY += particle.gravity * delta;
    particle.velocityX *= particle.drag;
    particle.velocityY *= particle.drag;
    particle.x += particle.velocityX * delta;
    particle.y += particle.velocityY * delta;
    particle.rotation += particle.spin * delta;

    const progress = particle.life / particle.maxLife;
    particle.alpha = progress < 0.72 ? 1 : Math.max(0, 1 - (progress - 0.72) / 0.28);

    drawParticle(context, particle);

    return progress < 1 && particle.y < viewport.height + 80;
  });

  if (!confettiParticles.length) {
    stopConfetti();
    return;
  }

  confettiRaf = requestAnimationFrame(renderConfetti);
}

function getConfettiLaunchers(originElement) {
  if (originElement instanceof HTMLElement) {
    const rect = originElement.getBoundingClientRect();
    const baseY = rect.top + rect.height * 0.5;
    const centerX = rect.left + rect.width / 2;

    if (rect.width > 120) {
      return [
        { x: rect.left + rect.width * 0.35, y: baseY, angle: -100 },
        { x: rect.left + rect.width * 0.65, y: baseY, angle: -80 },
      ];
    }

    return [{ x: centerX, y: baseY, angle: -90 }];
  }

  const bottomInset = getSafeBottomPx();
  if (viewport.width > 640) {
    return [
      { x: viewport.width * 0.5 - 26, y: viewport.height - bottomInset - 20, angle: -104 },
      { x: viewport.width * 0.5 + 26, y: viewport.height - bottomInset - 20, angle: -76 },
    ];
  }

  return [{ x: viewport.width * 0.5, y: viewport.height - bottomInset - 20, angle: -90 }];
}

function fireConfettiCannon(originElement = null) {
  if (prefersReducedMotion.value) {
    return;
  }

  updateViewport();
  resizeCanvas();
  const palette = ["#ffffff", "#8ea9ff", "#69e0c0", "#ffd970", "#ff8cac", "#7bd3ff"];

  getConfettiLaunchers(originElement).forEach((launcher, cannonIndex, launchers) => {
    const launchCount = launchers.length > 1 ? 26 : 34;

    for (let index = 0; index < launchCount; index += 1) {
      const angle = (launcher.angle + (Math.random() * 52 - 26)) * (Math.PI / 180);
      const speed = 460 + Math.random() * 420;
      const randomShape = Math.random();
      const shape = randomShape > 0.66 ? "streamer" : randomShape > 0.32 ? "rect" : "dot";

      confettiParticles.push({
        x: launcher.x + (Math.random() * 14 - 7),
        y: launcher.y + (Math.random() * 6 - 3),
        size: shape === "streamer" ? 16 + Math.random() * 10 : 8 + Math.random() * 7,
        shape,
        color: palette[(index + cannonIndex * 2) % palette.length],
        velocityX: Math.cos(angle) * speed,
        velocityY: Math.sin(angle) * speed,
        gravity: 860 + Math.random() * 220,
        drag: 0.992 - Math.random() * 0.008,
        rotation: Math.random() * Math.PI * 2,
        spin: (Math.random() * 14 - 7) * (shape === "streamer" ? 1.4 : 1),
        alpha: 1,
        life: 0,
        maxLife: 0.9 + Math.random() * 0.45,
      });
    }
  });

  if (!confettiRaf) {
    confettiRaf = requestAnimationFrame(renderConfetti);
  }
}

function handleNext(event) {
  const originElement = event?.currentTarget instanceof HTMLElement ? event.currentTarget : null;
  fireConfettiCannon(originElement);

  if (isLastStep.value) {
    emit("complete");
    emit("update:modelValue", false);
    return;
  }

  emit("update:step", stepOrder[currentStepIndex.value + 1]);
}

function handleBack() {
  if (currentStepIndex.value <= 0) {
    return;
  }

  emit("update:step", stepOrder[currentStepIndex.value - 1]);
}

function handleSkip() {
  emit("skip");
  emit("update:modelValue", false);
}

function handleWindowChange() {
  if (!props.modelValue) {
    return;
  }

  updateViewport();
  void updateRect();
}

watch(
  () => [props.modelValue, props.step],
  async ([visible]) => {
    if (!visible) {
      rectState.ready = false;
      stopConfetti();
      return;
    }

    updateViewport();
    await updateRect();
    await measureCard();
  },
  { immediate: true }
);

onMounted(() => {
  updateViewport();
  prefersReducedMotion.value = window.matchMedia?.("(prefers-reduced-motion: reduce)")?.matches || false;
  window.addEventListener("resize", handleWindowChange, { passive: true });
  window.addEventListener("scroll", handleWindowChange, { passive: true, capture: true });
  window.visualViewport?.addEventListener("resize", handleWindowChange, { passive: true });
  window.visualViewport?.addEventListener("scroll", handleWindowChange, { passive: true });
});

onBeforeUnmount(() => {
  window.removeEventListener("resize", handleWindowChange);
  window.removeEventListener("scroll", handleWindowChange, true);
  window.visualViewport?.removeEventListener("resize", handleWindowChange);
  window.visualViewport?.removeEventListener("scroll", handleWindowChange);
  stopConfetti();

  if (pendingMeasureTimer) {
    clearTimeout(pendingMeasureTimer);
    pendingMeasureTimer = null;
  }
});
</script>

<template>
  <Teleport to="body">
    <Transition name="coach-overlay" appear>
      <section v-if="modelValue" class="coach-overlay" aria-modal="true" role="dialog">
        <canvas ref="confettiCanvasRef" class="coach-overlay__confetti-canvas" aria-hidden="true"></canvas>

        <div v-if="rectState.ready" class="coach-overlay__focus" :style="focusStyle"></div>

        <div ref="cardRef" class="coach-overlay__card" :style="cardStyle">
          <div class="coach-overlay__eyebrow">{{ currentStepIndex + 1 }} / {{ stepOrder.length }}</div>
          <div class="coach-overlay__title">{{ currentStepData.title }}</div>
          <div class="coach-overlay__body">{{ currentStepData.body }}</div>

          <div class="coach-overlay__actions">
            <v-btn variant="text" @click="handleSkip">{{ stepCopy.skip }}</v-btn>
            <div class="d-flex align-center ga-2 coach-overlay__primary-actions">
              <v-btn v-if="currentStepIndex > 0" variant="text" @click="handleBack">
                {{ stepCopy.back }}
              </v-btn>
              <v-btn color="primary" @click="handleNext($event)">
                {{ isLastStep ? stepCopy.done : stepCopy.next }}
              </v-btn>
            </div>
          </div>
        </div>
      </section>
    </Transition>
  </Teleport>
</template>

<style scoped>
.coach-overlay {
  position: fixed;
  inset: 0;
  z-index: 2200;
  background:
    radial-gradient(circle at 14% 18%, rgba(var(--v-theme-primary), 0.08), transparent 28%),
    radial-gradient(circle at 82% 14%, rgba(var(--v-theme-secondary), 0.06), transparent 24%),
    linear-gradient(180deg, rgba(8, 12, 18, 0.3), rgba(8, 12, 18, 0.42));
}

.coach-overlay__confetti-canvas {
  position: fixed;
  inset: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
}

.coach-overlay__focus {
  position: fixed;
  pointer-events: none;
  background: rgba(255, 255, 255, 0.02);
  border: 1px solid rgba(255, 255, 255, 0.24);
  box-shadow: 0 0 0 9999px rgba(8, 12, 18, 0.34);
  transition:
    top 260ms cubic-bezier(0.22, 1, 0.36, 1),
    left 260ms cubic-bezier(0.22, 1, 0.36, 1),
    width 260ms cubic-bezier(0.22, 1, 0.36, 1),
    height 260ms cubic-bezier(0.22, 1, 0.36, 1),
    border-radius 260ms cubic-bezier(0.22, 1, 0.36, 1);
  animation: coach-focus-pulse 2200ms ease-in-out infinite;
}

.coach-overlay__card {
  position: fixed;
  padding: 20px 20px 18px;
  border-radius: 28px;
  background: rgba(var(--v-theme-surface), 0.96);
  box-shadow: 0 24px 56px rgba(8, 12, 20, 0.26);
  overflow: auto;
  overscroll-behavior: contain;
  transition:
    top 260ms cubic-bezier(0.22, 1, 0.36, 1),
    left 260ms cubic-bezier(0.22, 1, 0.36, 1),
    right 260ms cubic-bezier(0.22, 1, 0.36, 1),
    width 260ms cubic-bezier(0.22, 1, 0.36, 1),
    max-height 260ms cubic-bezier(0.22, 1, 0.36, 1);
}

.coach-overlay__eyebrow {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.coach-overlay__title {
  margin-top: 10px;
  font-size: 1.22rem;
  font-weight: 700;
  letter-spacing: -0.02em;
}

.coach-overlay__body {
  margin-top: 10px;
  color: rgba(var(--v-theme-on-surface), 0.74);
  line-height: 1.68;
}

.coach-overlay__actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-top: 18px;
}

.coach-overlay-enter-active,
.coach-overlay-leave-active {
  transition: opacity 220ms ease;
}

.coach-overlay-enter-from,
.coach-overlay-leave-to {
  opacity: 0;
}

@keyframes coach-focus-pulse {
  0%,
  100% {
    border-color: rgba(255, 255, 255, 0.24);
    box-shadow: 0 0 0 9999px rgba(8, 12, 18, 0.34);
  }

  50% {
    border-color: rgba(255, 255, 255, 0.34);
    box-shadow: 0 0 0 9999px rgba(8, 12, 18, 0.34);
  }
}

:global(html[data-theme-mode="dark"]) .coach-overlay__card {
  background: rgba(var(--v-theme-surface), 0.94);
}

@media (max-width: 640px) {
  .coach-overlay__card {
    padding: 18px 18px 16px;
    border-radius: 24px;
  }

  .coach-overlay__actions {
    flex-direction: column;
    align-items: stretch;
  }

  .coach-overlay__primary-actions {
    display: grid;
    grid-template-columns: 1fr;
    width: 100%;
  }
}
</style>
