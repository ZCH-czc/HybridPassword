<script setup>
import { reactive, ref, watch } from "vue";
import { useAppPreferences } from "@/composables/useAppPreferences";
import {
  clonePasswordDraft,
  createEmptyPasswordDraft,
  sanitizeText,
} from "@/models/password-item";

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  initialDraft: {
    type: Object,
    default: () => createEmptyPasswordDraft(),
  },
  passwordInjection: {
    type: Object,
    default: () => ({ nonce: 0, value: "" }),
  },
  loading: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["update:modelValue", "save"]);
const formRef = ref(null);
const { t } = useAppPreferences();

const localDraft = reactive(createEmptyPasswordDraft());

const requiredRule = (value) => (!!String(value || "").trim() ? true : t("common.requiredField"));

function syncDraft() {
  const draft = clonePasswordDraft(props.initialDraft);
  localDraft.id = draft.id;
  localDraft.siteName = draft.siteName;
  localDraft.username = draft.username;
  localDraft.password = draft.password;
  localDraft.notes = draft.notes.length ? [...draft.notes] : [""];
  localDraft.isFavorite = Boolean(draft.isFavorite);
}

function addNote() {
  localDraft.notes.push("");
}

function removeNote(index) {
  if (localDraft.notes.length === 1) {
    localDraft.notes[0] = "";
    return;
  }

  localDraft.notes.splice(index, 1);
}

async function handleSave() {
  const result = await formRef.value?.validate();

  if (!result?.valid) {
    return;
  }

  emit("save", {
    id: localDraft.id,
    siteName: sanitizeText(localDraft.siteName),
    username: sanitizeText(localDraft.username),
    password: localDraft.password,
    notes: [...localDraft.notes],
    isFavorite: Boolean(localDraft.isFavorite),
  });
}

watch(
  () => props.modelValue,
  (value) => {
    if (value) {
      syncDraft();
    }
  },
  { immediate: true }
);

watch(
  () => props.initialDraft,
  () => {
    if (props.modelValue) {
      syncDraft();
    }
  },
  { deep: true }
);

watch(
  () => props.passwordInjection?.nonce,
  (nonce) => {
    if (nonce && props.passwordInjection?.value) {
      localDraft.password = props.passwordInjection.value;
    }
  }
);
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="760"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="border-sm editor-card">
      <v-card-title class="px-6 pt-6">
        {{ localDraft.id ? t("editor.editTitle") : t("editor.createTitle") }}
      </v-card-title>

      <v-card-text class="px-6 pb-2">
        <v-form ref="formRef" @submit.prevent="handleSave">
          <v-row>
            <v-col cols="12">
              <v-text-field
                v-model="localDraft.siteName"
                :label="t('editor.siteName')"
                prepend-inner-icon="mdi-web"
              />
            </v-col>

            <v-col cols="12" md="6">
              <v-text-field
                v-model="localDraft.username"
                :label="t('editor.username')"
                prepend-inner-icon="mdi-account-outline"
                :rules="[requiredRule]"
              />
            </v-col>

            <v-col cols="12" md="6">
              <v-text-field
                v-model="localDraft.password"
                :label="t('editor.password')"
                prepend-inner-icon="mdi-form-textbox-password"
                :rules="[requiredRule]"
                type="text"
              />
            </v-col>

            <v-col cols="12">
              <div class="d-flex align-center justify-space-between mb-2">
                <div class="text-subtitle-1 font-weight-medium">{{ t("editor.notes") }}</div>
                <v-btn size="small" variant="text" prepend-icon="mdi-plus" @click="addNote">
                  {{ t("editor.addNote") }}
                </v-btn>
              </div>

              <div
                v-for="(note, index) in localDraft.notes"
                :key="`${localDraft.id || 'new'}-${index}`"
                class="d-flex ga-2 mb-2"
              >
                <v-text-field
                  v-model="localDraft.notes[index]"
                  :label="t('editor.noteLabel', { index: index + 1 })"
                  prepend-inner-icon="mdi-note-text-outline"
                  hide-details
                />

                <v-btn icon variant="text" color="error" @click="removeNote(index)">
                  <v-icon>mdi-close</v-icon>
                </v-btn>
              </div>
            </v-col>
          </v-row>
        </v-form>
      </v-card-text>

      <v-card-actions class="px-6 pb-6">
        <v-spacer />
        <v-btn variant="text" @click="emit('update:modelValue', false)">{{ t("common.cancel") }}</v-btn>
        <v-btn color="primary" :loading="loading" @click="handleSave">{{ t("common.save") }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.editor-card {
  background: var(--vault-panel-bg);
  box-shadow: var(--vault-shadow);
}

:deep(.editor-card .v-field) {
  background: var(--vault-block-bg) !important;
  box-shadow: none !important;
}
</style>
