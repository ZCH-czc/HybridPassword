<script setup>
import { reactive, watch } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

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
const { t } = useAppPreferences();

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
  <v-card class="border-sm webdav-card">
    <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-3">
      <span>{{ t("settings.webDav") }}</span>
      <v-chip color="primary" variant="tonal">{{ t("settings.encryptedSnapshot") }}</v-chip>
    </v-card-title>

    <v-card-text class="d-flex flex-column ga-4">
      <v-text-field
        v-model="form.baseUrl"
        :label="t('settings.webDavUrl')"
        placeholder="https://example.com/dav/"
        variant="solo-filled"
        hide-details
        :disabled="disabled"
      />

      <v-text-field
        v-model="form.remotePath"
        :label="t('settings.webDavPath')"
        placeholder="password-vault/snapshot.json"
        variant="solo-filled"
        hide-details
        :disabled="disabled"
      />

      <v-row dense>
        <v-col cols="12" md="6">
          <v-text-field
            v-model="form.username"
            :label="t('settings.webDavUsername')"
            variant="solo-filled"
            hide-details
            :disabled="disabled"
          />
        </v-col>

        <v-col cols="12" md="6">
          <v-text-field
            v-model="form.password"
            :label="t('settings.webDavPassword')"
            type="text"
            class="vault-masked-field"
            autocomplete="new-password"
            autocorrect="off"
            autocapitalize="none"
            spellcheck="false"
            variant="solo-filled"
            hide-details
            :disabled="disabled"
          />
        </v-col>
      </v-row>

      <v-sheet class="rounded-xl px-4 py-3 vault-surface-block vault-surface-block--subtle text-body-2 text-medium-emphasis">
        <div>{{ t("settings.webDavHint") }}</div>
        <div class="mt-1">
          {{ settings.hasPassword ? t("settings.webDavPasswordSaved") : t("settings.webDavPasswordEmpty") }}
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
          {{ t("settings.saveConfig") }}
        </v-btn>
        <v-btn
          variant="tonal"
          prepend-icon="mdi-upload-outline"
          :loading="transferring"
          :disabled="disabled || saving"
          @click="emit('upload')"
        >
          {{ t("settings.uploadCurrentData") }}
        </v-btn>
        <v-btn
          variant="tonal"
          prepend-icon="mdi-download-outline"
          :loading="transferring"
          :disabled="disabled || saving"
          @click="emit('download')"
        >
          {{ t("settings.downloadFromWebDav") }}
        </v-btn>
      </div>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.webdav-card {
  background: var(--vault-panel-bg);
}
</style>
