<script setup>
import { reactive, watch } from "vue";

const props = defineProps({
  settings: {
    type: Object,
    default: () => ({
      baseUrl: "",
      remotePath: "",
      username: "",
      hasPassword: false,
    }),
  },
  disabled: {
    type: Boolean,
    default: false,
  },
  saving: {
    type: Boolean,
    default: false,
  },
  transferring: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["save", "upload", "download"]);

const form = reactive({
  baseUrl: "",
  remotePath: "",
  username: "",
  password: "",
});

watch(
  () => props.settings,
  (value) => {
    form.baseUrl = value?.baseUrl || "";
    form.remotePath = value?.remotePath || "";
    form.username = value?.username || "";
    form.password = "";
  },
  { immediate: true, deep: true }
);

function emitSave() {
  emit("save", {
    baseUrl: form.baseUrl,
    remotePath: form.remotePath,
    username: form.username,
    password: form.password,
    updatePassword: form.password.length > 0,
  });
}
</script>

<template>
  <v-card class="border-sm">
    <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-3">
      <span>WebDAV 同步</span>
      <v-chip color="primary" variant="tonal">加密快照</v-chip>
    </v-card-title>

    <v-card-text class="d-flex flex-column ga-4">
      <v-text-field
        v-model="form.baseUrl"
        label="WebDAV 地址"
        placeholder="https://example.com/dav/"
        variant="solo-filled"
        hide-details
        :disabled="disabled"
      />

      <v-text-field
        v-model="form.remotePath"
        label="远端文件路径"
        placeholder="password-vault/snapshot.json"
        variant="solo-filled"
        hide-details
        :disabled="disabled"
      />

      <v-row dense>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="form.username"
            label="用户名"
            variant="solo-filled"
            hide-details
            :disabled="disabled"
          />
        </v-col>

        <v-col cols="12" md="6">
          <v-text-field
            v-model="form.password"
            label="密码"
            type="password"
            autocomplete="new-password"
            variant="solo-filled"
            hide-details
            :disabled="disabled"
          />
        </v-col>
      </v-row>

      <v-sheet class="rounded-xl px-4 py-3 bg-surface-variant text-body-2 text-medium-emphasis">
        <div>WebDAV 中保存的是加密快照，不会上传明文密码。</div>
        <div class="mt-1">
          {{ settings.hasPassword ? "当前已保存 WebDAV 密码，留空则保持不变。" : "如果服务端需要认证，请填写用户名和密码。" }}
        </div>
      </v-sheet>

      <div class="d-flex flex-wrap ga-2">
        <v-btn
          color="primary"
          prepend-icon="mdi-content-save-outline"
          :loading="saving"
          :disabled="disabled || transferring"
          @click="emitSave"
        >
          保存配置
        </v-btn>
        <v-btn
          variant="tonal"
          prepend-icon="mdi-upload-outline"
          :loading="transferring"
          :disabled="disabled || saving"
          @click="emit('upload')"
        >
          上传当前数据
        </v-btn>
        <v-btn
          variant="tonal"
          prepend-icon="mdi-download-outline"
          :loading="transferring"
          :disabled="disabled || saving"
          @click="emit('download')"
        >
          从 WebDAV 拉取
        </v-btn>
      </div>
    </v-card-text>
  </v-card>
</template>
