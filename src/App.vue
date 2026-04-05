<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from "vue";
import { useLocale, useTheme } from "vuetify";
import AppBottomNav from "@/components/AppBottomNav.vue";
import AppSnackbar from "@/components/AppSnackbar.vue";
import AppTopBar from "@/components/AppTopBar.vue";
import ChangeMasterPasswordDialog from "@/components/ChangeMasterPasswordDialog.vue";
import DeleteConfirmDialog from "@/components/DeleteConfirmDialog.vue";
import MasterKeyDialog from "@/components/MasterKeyDialog.vue";
import OnboardingDialog from "@/components/OnboardingDialog.vue";
import PasswordEditorDialog from "@/components/PasswordEditorDialog.vue";
import SyncConfirmDialog from "@/components/SyncConfirmDialog.vue";
import VaultHomeView from "@/components/VaultHomeView.vue";
import VaultListView from "@/components/VaultListView.vue";
import VaultSettingsView from "@/components/VaultSettingsView.vue";
import {
  getDefaultLocale,
  useAppPreferences,
} from "@/composables/useAppPreferences";
import { usePasswordVault } from "@/composables/usePasswordVault";
import { createEmptyPasswordDraft } from "@/models/password-item";
import {
  copyTextToClipboard,
  downloadBlobFile,
  pickFileFromBrowser,
} from "@/utils/browser-utils";
import { buildCsvContent, buildTxtContent, readTextFile } from "@/utils/csv-utils";
import {
  disableBiometricUnlock,
  downloadLanSnapshotWithHost,
  downloadSnapshotFromWebDavWithHost,
  enableBiometricUnlock,
  getHostBridgeState,
  getSyncSettingsWithHost,
  openAutostartSettings,
  pickCsvFileWithHost,
  publishLanSnapshotWithHost,
  saveTextFileWithHost,
  saveWebDavSettingsWithHost,
  scanLanDevicesWithHost,
  setExcludeFromRecents,
  setLanDeviceNameWithHost,
  setLaunchAtStartup,
  setMinimizeToTray,
  unlockWithBiometric,
  updateStoredMasterPassword,
  uploadSnapshotToWebDavWithHost,
} from "@/utils/native-bridge";
import { parseEncryptedVaultSnapshot } from "@/utils/vault-sync";
import { getAppSettingsRecord, saveAppSettingsRecord } from "@/utils/indexed-db";

const theme = useTheme();
const vuetifyLocale = useLocale();
const vault = usePasswordVault();
const {
  state: preferences,
  t,
  setLocale,
  setThemeMode,
  setSystemPrefersDark,
  getVuetifyLocale,
  resolvedTheme,
} = useAppPreferences();

const currentView = ref("home");
const listMode = ref("all");
const searchText = ref("");
const editorVisible = ref(false);
const deleteDialogVisible = ref(false);
const onboardingVisible = ref(false);
const changeMasterPasswordVisible = ref(false);
const editorDraft = ref(createEmptyPasswordDraft());
const importStrategy = ref("overwrite");
const passwordInjection = ref({
  nonce: 0,
  value: "",
});
const pendingDeleteIds = ref([]);
const pendingDeleteTitle = ref("");
const pendingLanPublishTimer = ref(null);
let colorSchemeMediaQuery = null;

const settings = reactive({
  onboardingCompleted: false,
});

const session = reactive({
  masterPassphrase: "",
});

const host = reactive({
  checked: false,
  isSupported: false,
  isBiometricAvailable: false,
  isBiometricEnabled: false,
  supportsNativeFileDialogs: false,
  biometricLabel: "Biometrics",
  platform: "web",
  message: "",
  safeAreaTop: 0,
  safeAreaBottom: 0,
  supportsMinimizeToTray: false,
  minimizeToTrayEnabled: false,
  supportsLaunchAtStartup: false,
  launchAtStartupEnabled: false,
  supportsExcludeFromRecents: false,
  excludeFromRecentsEnabled: false,
  supportsAutostartSettingsShortcut: false,
  supportsWebDavSync: false,
  supportsLanSync: false,
});

const busy = reactive({
  saving: false,
  importing: false,
  exporting: false,
  deleting: false,
  bulkActing: false,
  changingMasterPassword: false,
  biometricUnlocking: false,
  biometricConfiguring: false,
  platformSettings: false,
  autostartOpening: false,
  webDavSaving: false,
  webDavTransferring: false,
  lanScanning: false,
  lanSavingDeviceName: false,
  lanConfirming: false,
});

const itemLoadingState = reactive({
  revealingIds: {},
  editingIds: {},
  favoriteIds: {},
  deletedBusyIds: {},
});

const selection = reactive({
  active: false,
  ids: [],
});

const snackbar = reactive({
  show: false,
  text: "",
  color: "success",
});

const syncSettings = reactive({
  deviceId: "",
  deviceName: "",
  webDav: {
    baseUrl: "",
    remotePath: "",
    username: "",
    hasPassword: false,
  },
});

const syncState = reactive({
  lanDevices: [],
  confirmVisible: false,
  confirmSourceLabel: "",
  confirmSnapshotText: "",
  confirmLocalPreview: emptyPreview(),
  confirmRemotePreview: emptyPreview(),
});

const isLocked = computed(() => !vault.state.unlocked);
const masterDialogVisible = computed(() => !vault.state.bootstrapping && !vault.state.unlocked);
const masterDialogMode = computed(() => (vault.state.requiresSetup ? "setup" : "unlock"));
const shouldShowSearchBar = computed(
  () => currentView.value === "home" || currentView.value === "list"
);
const biometricUnlockReady = computed(
  () =>
    host.isSupported &&
    host.isBiometricAvailable &&
    host.isBiometricEnabled &&
    masterDialogMode.value === "unlock"
);

const searchedRecords = computed(() => {
  const keyword = searchText.value.trim().toLowerCase();
  if (!keyword) {
    return vault.records.value;
  }

  return vault.records.value.filter((record) => {
    const searchArea = [record.siteName, record.username, ...(record.notes || [])]
      .join(" ")
      .toLowerCase();
    return searchArea.includes(keyword);
  });
});

const searchedDeletedRecords = computed(() => {
  const keyword = searchText.value.trim().toLowerCase();
  if (!keyword) {
    return vault.deletedRecords.value;
  }

  return vault.deletedRecords.value.filter((record) => {
    const searchArea = [record.siteName, record.username, ...(record.notes || [])]
      .join(" ")
      .toLowerCase();
    return searchArea.includes(keyword);
  });
});

const deletedRecords = computed(() => vault.deletedRecords.value);
const favoriteRecords = computed(() =>
  searchedRecords.value.filter((record) => record.isFavorite)
);

const listRecords = computed(() => {
  if (listMode.value === "favorites") {
    return favoriteRecords.value;
  }

  if (listMode.value === "deleted") {
    return searchedDeletedRecords.value;
  }

  return searchedRecords.value;
});

const recentRecords = computed(() => searchedRecords.value.slice(0, 4));
const homeFavoriteRecords = computed(() => favoriteRecords.value.slice(0, 4));
const deleteDialogCount = computed(() => pendingDeleteIds.value.length);
const selectedRecords = computed(() => {
  const selectedIds = new Set(selection.ids);
  return listRecords.value.filter((record) => selectedIds.has(record.id));
});

const summary = computed(() => ({
  total: vault.records.value.length,
  filtered: searchedRecords.value.length,
  notes: vault.records.value.reduce((count, record) => count + record.notes.length, 0),
}));

const lanPublishKey = computed(() => {
  if (!vault.state.unlocked || !host.supportsLanSync) {
    return "disabled";
  }

  const active = vault.records.value
    .map((record) => `${record.id}:${record.createdAt}:${record.updatedAt}`)
    .join("|");
  const removed = vault.deletedRecords.value
    .map((record) => `${record.id}:${record.createdAt}:${record.updatedAt}:${record.deletedAt}`)
    .join("|");

  return `${syncSettings.deviceName}::${active}::${removed}`;
});

function emptyPreview() {
  return {
    totalCount: 0,
    deletedCount: 0,
    latestItem: null,
  };
}

function notify(text, color = "success") {
  snackbar.text = text;
  snackbar.color = color;
  snackbar.show = true;
}

function applyResolvedTheme() {
  theme.global.name.value = resolvedTheme.value;
  document.documentElement.dataset.themeMode = resolvedTheme.value;
}

function applyLocalePreference(localeCode) {
  const nextLocale = localeCode || getDefaultLocale();
  setLocale(nextLocale);
  vuetifyLocale.current.value = getVuetifyLocale(nextLocale);
}

function applyThemeModePreference(themeMode) {
  setThemeMode(themeMode || "system");
  applyResolvedTheme();
}

function updateSystemThemePreference(prefersDark) {
  setSystemPrefersDark(prefersDark);
  applyResolvedTheme();
}

function applySafeAreaInsets(top, bottom) {
  document.documentElement.style.setProperty("--host-safe-top", `${Math.max(0, top || 0)}px`);
  document.documentElement.style.setProperty(
    "--host-safe-bottom",
    `${Math.max(0, bottom || 0)}px`
  );
}

async function persistSettings() {
  await saveAppSettingsRecord({
    themeMode: preferences.themeMode,
    locale: preferences.locale,
    onboardingCompleted: settings.onboardingCompleted,
  });
}

async function loadAppSettings() {
  try {
    const record = await getAppSettingsRecord();
    applyLocalePreference(record?.locale || getDefaultLocale());
    applyThemeModePreference(record?.themeMode || "system");
    settings.onboardingCompleted = Boolean(record?.onboardingCompleted);
  } catch {
    applyLocalePreference(getDefaultLocale());
    applyThemeModePreference("system");
  }
}

async function loadSyncSettings() {
  if (!host.supportsLanSync && !host.supportsWebDavSync) {
    syncSettings.deviceId = "";
    syncSettings.deviceName = "";
    syncSettings.webDav.baseUrl = "";
    syncSettings.webDav.remotePath = "";
    syncSettings.webDav.username = "";
    syncSettings.webDav.hasPassword = false;
    return;
  }

  const state = await getSyncSettingsWithHost();
  syncSettings.deviceId = state.deviceId;
  syncSettings.deviceName = state.deviceName;
  syncSettings.webDav.baseUrl = state.webDav.baseUrl;
  syncSettings.webDav.remotePath = state.webDav.remotePath;
  syncSettings.webDav.username = state.webDav.username;
  syncSettings.webDav.hasPassword = state.webDav.hasPassword;
}

async function refreshHostState() {
  const state = await getHostBridgeState();
  host.checked = true;
  host.isSupported = state.isSupported;
  host.isBiometricAvailable = state.isBiometricAvailable;
  host.isBiometricEnabled = state.isBiometricEnabled;
  host.supportsNativeFileDialogs = state.supportsNativeFileDialogs;
  host.biometricLabel = state.biometricLabel;
  host.platform = state.platform;
  host.message = state.message;
  host.safeAreaTop = state.safeAreaTop;
  host.safeAreaBottom = state.safeAreaBottom;
  host.supportsMinimizeToTray = state.supportsMinimizeToTray;
  host.minimizeToTrayEnabled = state.minimizeToTrayEnabled;
  host.supportsLaunchAtStartup = state.supportsLaunchAtStartup;
  host.launchAtStartupEnabled = state.launchAtStartupEnabled;
  host.supportsExcludeFromRecents = state.supportsExcludeFromRecents;
  host.excludeFromRecentsEnabled = state.excludeFromRecentsEnabled;
  host.supportsAutostartSettingsShortcut = state.supportsAutostartSettingsShortcut;
  host.supportsWebDavSync = state.supportsWebDavSync;
  host.supportsLanSync = state.supportsLanSync;
  applySafeAreaInsets(state.safeAreaTop, state.safeAreaBottom);
}

function resetSelection() {
  selection.active = false;
  selection.ids = [];
}

function openCreateDialog() {
  editorDraft.value = createEmptyPasswordDraft();
  editorVisible.value = true;
}

function buildCurrentSyncSnapshot() {
  const snapshot = vault.getEncryptedSnapshot(syncSettings.deviceName);
  return {
    snapshot,
    text: JSON.stringify(snapshot),
    preview: snapshot.preview || emptyPreview(),
  };
}

async function publishCurrentLanSnapshot(silent = true) {
  if (!host.supportsLanSync || !vault.state.unlocked) {
    return;
  }

  try {
    const current = buildCurrentSyncSnapshot();
    const result = await publishLanSnapshotWithHost({
      deviceName: syncSettings.deviceName,
      snapshotContent: current.text,
      preview: current.preview,
    });

    if (!result.success && !silent) {
      notify(result.message || t("notify.lanPublishFailed"), "warning");
    }
  } catch (error) {
    if (!silent) {
      notify(error.message || t("notify.lanPublishFailed"), "warning");
    }
  }
}

async function unlockVaultWithPassphrase(passphrase) {
  try {
    await vault.submitMasterPassword(passphrase);
    session.masterPassphrase = passphrase;

    if (!settings.onboardingCompleted) {
      onboardingVisible.value = true;
    }

    return true;
  } catch (error) {
    notify(error.message || t("notify.unlockFailed"), "error");
    return false;
  }
}

async function handleMasterSubmit(passphrase) {
  await unlockVaultWithPassphrase(passphrase);
}

async function handleBiometricUnlock() {
  busy.biometricUnlocking = true;

  try {
    const result = await unlockWithBiometric();
    if (!result.success || !result.masterPassword) {
      notify(result.message || t("notify.biometricUnavailable", { label: host.biometricLabel }), "warning");
      await refreshHostState();
      return;
    }

    const success = await unlockVaultWithPassphrase(result.masterPassword);
    if (!success) {
      notify(t("notify.biometricStoredPasswordExpired"), "warning");
    }
  } finally {
    busy.biometricUnlocking = false;
  }
}

function handleLockVault() {
  vault.lockVault();
  editorVisible.value = false;
  deleteDialogVisible.value = false;
  changeMasterPasswordVisible.value = false;
  pendingDeleteIds.value = [];
  pendingDeleteTitle.value = "";
  session.masterPassphrase = "";
  resetSelection();
}

async function handleEditRecord(recordId) {
  itemLoadingState.editingIds[recordId] = true;

  try {
    editorDraft.value = await vault.buildEditableDraft(recordId);
    editorVisible.value = true;
  } catch (error) {
    notify(error.message || t("notify.readDraftFailed"), "error");
  } finally {
    delete itemLoadingState.editingIds[recordId];
  }
}

async function handleSaveDraft(draft) {
  busy.saving = true;

  try {
    const existed = Boolean(draft.id);
    await vault.saveDraft(draft);
    editorVisible.value = false;
    editorDraft.value = createEmptyPasswordDraft();
    notify(existed ? t("notify.recordUpdated") : t("notify.recordCreated"));
  } catch (error) {
    notify(error.message || t("notify.saveFailed"), "error");
  } finally {
    busy.saving = false;
  }
}

async function handleToggleReveal(recordId) {
  if (vault.revealedPasswords[recordId]) {
    vault.hidePassword(recordId);
    return;
  }

  itemLoadingState.revealingIds[recordId] = true;

  try {
    await vault.decryptPasswordById(recordId);
  } catch (error) {
    notify(error.message || t("notify.decryptFailed"), "error");
  } finally {
    delete itemLoadingState.revealingIds[recordId];
  }
}

async function handleToggleFavorite(recordId) {
  itemLoadingState.favoriteIds[recordId] = true;

  try {
    await vault.toggleFavorite(recordId);
  } catch (error) {
    notify(error.message || t("notify.favoriteFailed"), "error");
  } finally {
    delete itemLoadingState.favoriteIds[recordId];
  }
}

async function handleCopyPassword(recordId) {
  try {
    const plainPassword = await vault.decryptPasswordById(recordId);
    await copyTextToClipboard(plainPassword);
    notify(t("notify.passwordCopied"));
  } catch (error) {
    notify(error.message || t("notify.copyPasswordFailed"), "warning");
  }
}

async function handleCopyUsername(username) {
  try {
    await copyTextToClipboard(username);
    notify(t("notify.usernameCopied"));
  } catch (error) {
    notify(error.message || t("notify.copyUsernameFailed"), "warning");
  }
}

function handleAskDelete(recordId) {
  const record = vault.records.value.find((item) => item.id === recordId);
  pendingDeleteIds.value = [recordId];
  pendingDeleteTitle.value = record?.siteName || record?.username || "";
  deleteDialogVisible.value = true;
}

function handleAskBulkDelete() {
  if (!selection.ids.length) {
    return;
  }

  pendingDeleteIds.value = [...selection.ids];
  pendingDeleteTitle.value = "";
  deleteDialogVisible.value = true;
}

async function handleConfirmDelete() {
  if (!pendingDeleteIds.value.length) {
    return;
  }

  busy.deleting = true;

  try {
    if (pendingDeleteIds.value.length === 1) {
      await vault.removeRecord(pendingDeleteIds.value[0]);
      notify(t("notify.deleted"));
    } else {
      await vault.removeRecords(pendingDeleteIds.value);
      notify(t("notify.deletedMany", { count: pendingDeleteIds.value.length }));
    }

    deleteDialogVisible.value = false;
    pendingDeleteIds.value = [];
    pendingDeleteTitle.value = "";
    resetSelection();
  } catch (error) {
    notify(error.message || t("notify.deleteFailed"), "error");
  } finally {
    busy.deleting = false;
  }
}

async function handleRestoreDeleted(recordId) {
  itemLoadingState.deletedBusyIds[recordId] = true;

  try {
    await vault.restoreRecord(recordId);
    notify(t("notify.restored"));
  } catch (error) {
    notify(error.message || t("notify.restoreFailed"), "error");
  } finally {
    delete itemLoadingState.deletedBusyIds[recordId];
  }
}

async function handlePermanentDelete(recordId) {
  itemLoadingState.deletedBusyIds[recordId] = true;

  try {
    await vault.permanentlyDeleteRecord(recordId);
    notify(t("notify.permanentlyDeleted"));
  } catch (error) {
    notify(error.message || t("notify.permanentDeleteFailed"), "error");
  } finally {
    delete itemLoadingState.deletedBusyIds[recordId];
  }
}

async function handleExport(format) {
  busy.exporting = true;

  try {
    const entries = await vault.getExportEntries();
    const content = format === "csv" ? buildCsvContent(entries) : buildTxtContent(entries);
    const extension = format === "csv" ? "csv" : "txt";
    const mimeType =
      format === "csv" ? "text/csv;charset=utf-8" : "text/plain;charset=utf-8";
    const fileName = `passwords-${new Date().toISOString().slice(0, 10)}.${extension}`;

    if (host.supportsNativeFileDialogs) {
      const result = await saveTextFileWithHost({
        fileName,
        content,
        mimeType,
      });

      if (result.cancelled) {
        return;
      }

      if (!result.success) {
        throw new Error(result.message || t("notify.exportFailed"));
      }

      notify(t("notify.exportSaved", { format: format.toUpperCase() }));
      return;
    }

    downloadBlobFile(fileName, content, mimeType);
    notify(t("notify.exportSuccess", { format: format.toUpperCase() }));
  } catch (error) {
    notify(error.message || t("notify.exportFailed"), "error");
  } finally {
    busy.exporting = false;
  }
}

async function handleImport(strategy) {
  busy.importing = true;

  try {
    let text = "";

    if (host.supportsNativeFileDialogs) {
      const result = await pickCsvFileWithHost();
      if (result.cancelled) {
        return;
      }

      if (!result.success) {
        throw new Error(result.message || t("notify.importFailed"));
      }

      text = result.content;
    } else {
      const file = await pickFileFromBrowser({
        accept: ".csv,text/csv",
      });
      text = await readTextFile(file);
    }

    const importResult = await vault.importEntriesFromCsvText(text, strategy);
    notify(t("notify.importDone", importResult));
  } catch (error) {
    const normalizedMessage = String(error?.message || "").toLowerCase();
    if (error.message === "已取消选择文件。" || normalizedMessage.includes("cancel")) {
      return;
    }

    notify(error.message || t("notify.importFailed"), "error");
  } finally {
    busy.importing = false;
  }
}

function applyGeneratedPassword(password) {
  if (!editorVisible.value) {
    editorDraft.value = createEmptyPasswordDraft();
    editorVisible.value = true;
  }

  passwordInjection.value = {
    nonce: Date.now(),
    value: password,
  };
}

async function copyGeneratedPassword(password) {
  try {
    await copyTextToClipboard(password);
    notify(t("notify.generatedCopied"));
  } catch (error) {
    notify(error.message || t("notify.generatedCopyFailed"), "warning");
  }
}

function openListView() {
  currentView.value = "list";
}

function handleToggleSelectionMode(enabled) {
  selection.active = Boolean(enabled);
  if (!selection.active) {
    selection.ids = [];
  }
}

function handleToggleSelected(recordId) {
  if (!selection.active) {
    return;
  }

  const nextIds = new Set(selection.ids);
  if (nextIds.has(recordId)) {
    nextIds.delete(recordId);
  } else {
    nextIds.add(recordId);
  }

  selection.ids = [...nextIds];
}

function handleSelectAllVisible(shouldSelect) {
  selection.ids = shouldSelect ? listRecords.value.map((record) => record.id) : [];
}

async function handleBulkFavorite() {
  if (!selection.ids.length) {
    return;
  }

  busy.bulkActing = true;

  try {
    const shouldFavorite = selectedRecords.value.some((record) => !record.isFavorite);
    await vault.setFavoriteRecords(selection.ids, shouldFavorite);
    notify(
      shouldFavorite
        ? t("notify.bulkFavorite", { count: selection.ids.length })
        : t("notify.bulkUnfavorite", { count: selection.ids.length })
    );
    resetSelection();
  } catch (error) {
    notify(error.message || t("notify.bulkFavoriteFailed"), "error");
  } finally {
    busy.bulkActing = false;
  }
}

async function handleUpdateThemeMode(mode) {
  try {
    applyThemeModePreference(mode);
    await persistSettings();
  } catch {
    notify(t("notify.themeFailed"), "error");
  }
}

async function handleUpdateLanguage(localeCode) {
  try {
    applyLocalePreference(localeCode);
    await persistSettings();
  } catch {
    notify(t("notify.languageFailed"), "error");
  }
}

async function handleCompleteOnboarding() {
  settings.onboardingCompleted = true;

  try {
    await persistSettings();
  } catch {
    notify(t("notify.onboardingSaveFailed"), "warning");
  }
}

async function handleEnableBiometricUnlock() {
  if (!session.masterPassphrase) {
    notify(t("notify.enterMasterPasswordFirst"), "info");
    return;
  }

  busy.biometricConfiguring = true;

  try {
    const result = await enableBiometricUnlock(session.masterPassphrase);
    await refreshHostState();

    if (!result.success) {
      notify(result.message || t("notify.biometricEnableFailed"), "warning");
      return;
    }

    notify(result.message || t("notify.biometricEnabled"));
  } finally {
    busy.biometricConfiguring = false;
  }
}

async function handleDisableBiometricUnlock() {
  busy.biometricConfiguring = true;

  try {
    const result = await disableBiometricUnlock();
    await refreshHostState();

    if (!result.success) {
      notify(result.message || t("notify.biometricDisableFailed"), "warning");
      return;
    }

    notify(result.message || t("notify.biometricDisabled"));
  } finally {
    busy.biometricConfiguring = false;
  }
}

async function handleChangeMasterPassword(payload) {
  busy.changingMasterPassword = true;

  try {
    await vault.changeMasterPassword(payload.currentPassphrase, payload.nextPassphrase);
    session.masterPassphrase = payload.nextPassphrase;

    if (host.isBiometricEnabled) {
      const syncResult = await updateStoredMasterPassword(payload.nextPassphrase);
      if (!syncResult.success) {
        notify(syncResult.message || t("notify.masterPasswordSyncWarning"), "warning");
      }
    }

    await refreshHostState();
    changeMasterPasswordVisible.value = false;
    notify(t("notify.masterPasswordUpdated"));
  } catch (error) {
    notify(error.message || t("notify.masterPasswordChangeFailed"), "error");
  } finally {
    busy.changingMasterPassword = false;
  }
}

async function handleToggleMinimizeToTray(enabled) {
  busy.platformSettings = true;

  try {
    const result = await setMinimizeToTray(enabled);
    await refreshHostState();

    if (!result.success) {
      notify(result.message || t("notify.minimizeToTrayFailed"), "warning");
      return;
    }

    notify(result.message || t("notify.minimizeToTrayUpdated"));
  } finally {
    busy.platformSettings = false;
  }
}

async function handleToggleLaunchAtStartup(enabled) {
  busy.platformSettings = true;

  try {
    const result = await setLaunchAtStartup(enabled);
    await refreshHostState();

    if (!result.success) {
      notify(result.message || t("notify.launchAtStartupFailed"), "warning");
      return;
    }

    notify(result.message || t("notify.launchAtStartupUpdated"));
  } finally {
    busy.platformSettings = false;
  }
}

async function handleToggleExcludeFromRecents(enabled) {
  busy.platformSettings = true;

  try {
    const result = await setExcludeFromRecents(enabled);
    await refreshHostState();

    if (!result.success) {
      notify(result.message || t("notify.excludeFromRecentsFailed"), "warning");
      return;
    }

    notify(result.message || t("notify.excludeFromRecentsUpdated"));
  } finally {
    busy.platformSettings = false;
  }
}

async function handleOpenAutostartSettings() {
  busy.autostartOpening = true;

  try {
    const result = await openAutostartSettings();
    if (!result.success) {
      notify(result.message || t("notify.openSystemSettingsFailed"), "warning");
      return;
    }

    notify(result.message || t("notify.openSystemSettingsSuccess"));
  } finally {
    busy.autostartOpening = false;
  }
}

async function handleSaveWebDavSettings(payload) {
  busy.webDavSaving = true;

  try {
    const result = await saveWebDavSettingsWithHost(payload);
    await loadSyncSettings();

    if (!result.success) {
      notify(result.message || t("notify.webDavSaveFailed"), "warning");
      return;
    }

    notify(result.message || t("notify.webDavSaved"));
  } finally {
    busy.webDavSaving = false;
  }
}

function openSyncConfirmation(snapshotText, sourceLabel) {
  const snapshot = parseEncryptedVaultSnapshot(snapshotText);
  syncState.confirmSourceLabel = sourceLabel;
  syncState.confirmSnapshotText = snapshotText;
  syncState.confirmLocalPreview = vault.getSyncPreview();
  syncState.confirmRemotePreview = snapshot.preview || emptyPreview();
  syncState.confirmVisible = true;
}

async function handleUploadWebDav() {
  busy.webDavTransferring = true;

  try {
    const current = buildCurrentSyncSnapshot();
    const result = await uploadSnapshotToWebDavWithHost(current.text);

    if (!result.success) {
      notify(result.message || t("notify.webDavUploadFailed"), "warning");
      return;
    }

    notify(result.message || t("notify.webDavUploaded"));
  } catch (error) {
    notify(error.message || t("notify.webDavUploadFailed"), "error");
  } finally {
    busy.webDavTransferring = false;
  }
}

async function handleDownloadWebDav() {
  busy.webDavTransferring = true;

  try {
    const result = await downloadSnapshotFromWebDavWithHost();
    if (!result.success) {
      notify(result.message || t("notify.webDavDownloadFailed"), "warning");
      return;
    }

    openSyncConfirmation(result.content, "WebDAV");
  } catch (error) {
    notify(error.message || t("notify.webDavDownloadFailed"), "error");
  } finally {
    busy.webDavTransferring = false;
  }
}

async function handleSaveDeviceName(deviceName) {
  busy.lanSavingDeviceName = true;

  try {
    const result = await setLanDeviceNameWithHost(deviceName);
    await loadSyncSettings();

    if (!result.success) {
      notify(result.message || t("notify.deviceNameSaveFailed"), "warning");
      return;
    }

    await publishCurrentLanSnapshot(false);
    notify(result.message || t("notify.deviceNameSaved"));
  } finally {
    busy.lanSavingDeviceName = false;
  }
}

async function handleScanLanDevices() {
  busy.lanScanning = true;

  try {
    syncState.lanDevices = await scanLanDevicesWithHost();
    if (!syncState.lanDevices.length) {
      notify(t("notify.noLanDevices"), "info");
    }
  } finally {
    busy.lanScanning = false;
  }
}

async function handleSyncLanDevice(device) {
  busy.webDavTransferring = true;

  try {
    const result = await downloadLanSnapshotWithHost(device);
    if (!result.success) {
      notify(result.message || t("notify.lanDownloadFailed"), "warning");
      return;
    }

    openSyncConfirmation(result.content, device.deviceName || t("common.sourceDevice"));
  } catch (error) {
    notify(error.message || t("notify.lanDownloadFailed"), "error");
  } finally {
    busy.webDavTransferring = false;
  }
}

async function handleConfirmSync() {
  if (!syncState.confirmSnapshotText) {
    return;
  }

  busy.lanConfirming = true;

  try {
    const currentPassphrase = session.masterPassphrase;
    await vault.replaceWithEncryptedSnapshot(syncState.confirmSnapshotText);
    syncState.confirmVisible = false;

    if (currentPassphrase) {
      try {
        await vault.submitMasterPassword(currentPassphrase);
        session.masterPassphrase = currentPassphrase;
        await publishCurrentLanSnapshot(true);
        notify(t("notify.syncCompletedUnlocked"));
        return;
      } catch {
        session.masterPassphrase = "";
      }
    }

    notify(t("notify.syncCompletedRelocked"), "warning");
  } catch (error) {
    notify(error.message || t("notify.syncFailed"), "error");
  } finally {
    busy.lanConfirming = false;
  }
}

function handleWindowResize() {
  if (host.platform !== "android") {
    return;
  }

  refreshHostState();
}

function handleColorSchemeChange(event) {
  updateSystemThemePreference(event.matches);
}

watch(
  [currentView, listMode, listRecords],
  () => {
    if (currentView.value !== "list" || listMode.value === "deleted") {
      resetSelection();
      return;
    }

    const visibleIds = new Set(listRecords.value.map((record) => record.id));
    selection.ids = selection.ids.filter((recordId) => visibleIds.has(recordId));
  },
  { deep: false }
);

watch(
  lanPublishKey,
  () => {
    if (pendingLanPublishTimer.value) {
      clearTimeout(pendingLanPublishTimer.value);
      pendingLanPublishTimer.value = null;
    }

    if (!host.supportsLanSync || !vault.state.unlocked) {
      return;
    }

    pendingLanPublishTimer.value = setTimeout(() => {
      publishCurrentLanSnapshot(true);
      pendingLanPublishTimer.value = null;
    }, 300);
  },
  { flush: "post" }
);

onMounted(async () => {
  window.addEventListener("resize", handleWindowResize, { passive: true });

  if (typeof window.matchMedia === "function") {
    colorSchemeMediaQuery = window.matchMedia("(prefers-color-scheme: dark)");
    updateSystemThemePreference(colorSchemeMediaQuery.matches);

    if (typeof colorSchemeMediaQuery.addEventListener === "function") {
      colorSchemeMediaQuery.addEventListener("change", handleColorSchemeChange);
    } else if (typeof colorSchemeMediaQuery.addListener === "function") {
      colorSchemeMediaQuery.addListener(handleColorSchemeChange);
    }
  } else {
    applyResolvedTheme();
  }

  await loadAppSettings();
  await refreshHostState();
  await loadSyncSettings();

  try {
    await vault.bootstrapVault();
  } catch (error) {
    notify(error.message || t("notify.vaultInitFailed"), "error");
  }
});

onBeforeUnmount(() => {
  window.removeEventListener("resize", handleWindowResize);

  if (colorSchemeMediaQuery) {
    if (typeof colorSchemeMediaQuery.removeEventListener === "function") {
      colorSchemeMediaQuery.removeEventListener("change", handleColorSchemeChange);
    } else if (typeof colorSchemeMediaQuery.removeListener === "function") {
      colorSchemeMediaQuery.removeListener(handleColorSchemeChange);
    }
  }

  if (pendingLanPublishTimer.value) {
    clearTimeout(pendingLanPublishTimer.value);
    pendingLanPublishTimer.value = null;
  }
});
</script>

<template>
  <v-app>
    <v-main class="app-main bg-background">
      <div class="app-shell-wrap" :class="{ 'app-shell-wrap--blurred': masterDialogVisible }">
        <div class="app-shell px-3 px-sm-4 px-lg-6">
          <AppTopBar
            v-if="shouldShowSearchBar"
            v-model="searchText"
            :current-view="currentView"
            :disabled="isLocked"
            @create="openCreateDialog"
            @lock="handleLockVault"
          />

          <div
            v-if="vault.state.bootstrapping"
            class="d-flex flex-column align-center justify-center py-16"
          >
            <v-progress-circular indeterminate color="primary" size="42" />
            <div class="text-body-1 text-medium-emphasis mt-4">{{ t("app.bootstrapping") }}</div>
          </div>

          <template v-else>
            <Transition name="vault-page" mode="out-in">
              <div
                :key="`${currentView}-${listMode}-${preferences.themeMode}-${preferences.locale}`"
                :class="shouldShowSearchBar ? 'mt-4' : 'mt-0'"
              >
                <VaultHomeView
                  v-if="currentView === 'home'"
                  :summary="summary"
                  :recent-items="recentRecords"
                  :favorite-items="homeFavoriteRecords"
                  :revealed-passwords="vault.revealedPasswords"
                  :revealing-ids="itemLoadingState.revealingIds"
                  :favorite-ids="itemLoadingState.favoriteIds"
                  :search-text="searchText"
                  @open-list="openListView"
                  @toggle-reveal="handleToggleReveal"
                  @toggle-favorite="handleToggleFavorite"
                  @copy-password="handleCopyPassword"
                  @copy-username="handleCopyUsername"
                  @edit="handleEditRecord"
                  @apply-generated="applyGeneratedPassword"
                  @copy-generated="copyGeneratedPassword"
                />

                <VaultListView
                  v-else-if="currentView === 'list'"
                  :items="listMode === 'deleted' ? [] : listRecords"
                  :deleted-items="searchedDeletedRecords"
                  :total-count="summary.total"
                  :favorite-count="favoriteRecords.length"
                  :search-text="searchText"
                  :list-mode="listMode"
                  :revealed-passwords="vault.revealedPasswords"
                  :revealing-ids="itemLoadingState.revealingIds"
                  :editing-ids="itemLoadingState.editingIds"
                  :favorite-ids="itemLoadingState.favoriteIds"
                  :deleted-busy-ids="itemLoadingState.deletedBusyIds"
                  :selection-mode="selection.active"
                  :selected-ids="selection.ids"
                  :bulk-loading="busy.bulkActing || busy.deleting"
                  @toggle-reveal="handleToggleReveal"
                  @toggle-favorite="handleToggleFavorite"
                  @edit="handleEditRecord"
                  @delete="handleAskDelete"
                  @copy-password="handleCopyPassword"
                  @copy-username="handleCopyUsername"
                  @restore="handleRestoreDeleted"
                  @permanent-delete="handlePermanentDelete"
                  @update:list-mode="listMode = $event"
                  @toggle-selection-mode="handleToggleSelectionMode"
                  @toggle-select="handleToggleSelected"
                  @select-all="handleSelectAllVisible"
                  @bulk-favorite="handleBulkFavorite"
                  @bulk-delete="handleAskBulkDelete"
                />

                <VaultSettingsView
                  v-else
                  :import-strategy="importStrategy"
                  :record-count="summary.total"
                  :deleted-items="deletedRecords"
                  :deleted-busy-ids="itemLoadingState.deletedBusyIds"
                  :native-file-dialogs-available="host.supportsNativeFileDialogs"
                  :busy="busy.importing || busy.exporting || isLocked"
                  :theme-mode="preferences.themeMode"
                  :resolved-theme="resolvedTheme"
                  :locale="preferences.locale"
                  :changing-master-password="busy.changingMasterPassword"
                  :biometric-supported="host.isSupported"
                  :biometric-available="host.isBiometricAvailable"
                  :biometric-enabled="host.isBiometricEnabled"
                  :biometric-label="host.biometricLabel"
                  :biometric-message="host.message"
                  :biometric-loading="busy.biometricConfiguring"
                  :platform="host.platform"
                  :supports-minimize-to-tray="host.supportsMinimizeToTray"
                  :minimize-to-tray-enabled="host.minimizeToTrayEnabled"
                  :supports-launch-at-startup="host.supportsLaunchAtStartup"
                  :launch-at-startup-enabled="host.launchAtStartupEnabled"
                  :supports-exclude-from-recents="host.supportsExcludeFromRecents"
                  :exclude-from-recents-enabled="host.excludeFromRecentsEnabled"
                  :supports-autostart-settings-shortcut="host.supportsAutostartSettingsShortcut"
                  :platform-settings-loading="busy.platformSettings"
                  :autostart-opening="busy.autostartOpening"
                  :supports-web-dav-sync="host.supportsWebDavSync"
                  :supports-lan-sync="host.supportsLanSync"
                  :sync-settings="syncSettings"
                  :lan-devices="syncState.lanDevices"
                  :web-dav-saving="busy.webDavSaving"
                  :web-dav-transfering="busy.webDavTransferring"
                  :lan-scanning="busy.lanScanning"
                  :lan-saving-device-name="busy.lanSavingDeviceName"
                  @update:import-strategy="importStrategy = $event"
                  @export="handleExport"
                  @import="handleImport"
                  @lock="handleLockVault"
                  @update-theme-mode="handleUpdateThemeMode"
                  @update-language="handleUpdateLanguage"
                  @change-master-password="changeMasterPasswordVisible = true"
                  @enable-biometric="handleEnableBiometricUnlock"
                  @disable-biometric="handleDisableBiometricUnlock"
                  @toggle-minimize-to-tray="handleToggleMinimizeToTray"
                  @toggle-launch-at-startup="handleToggleLaunchAtStartup"
                  @toggle-exclude-from-recents="handleToggleExcludeFromRecents"
                  @open-autostart-settings="handleOpenAutostartSettings"
                  @save-webdav-settings="handleSaveWebDavSettings"
                  @upload-webdav="handleUploadWebDav"
                  @download-webdav="handleDownloadWebDav"
                  @save-device-name="handleSaveDeviceName"
                  @scan-lan="handleScanLanDevices"
                  @sync-lan-device="handleSyncLanDevice"
                  @restore="handleRestoreDeleted"
                  @permanent-delete="handlePermanentDelete"
                />
              </div>
            </Transition>
          </template>
        </div>

        <AppBottomNav v-model="currentView" />
      </div>
    </v-main>

    <MasterKeyDialog
      :model-value="masterDialogVisible"
      :mode="masterDialogMode"
      :loading="vault.state.unlocking"
      :biometric-enabled="biometricUnlockReady"
      :biometric-label="host.biometricLabel"
      :biometric-loading="busy.biometricUnlocking"
      @submit="handleMasterSubmit"
      @biometric-unlock="handleBiometricUnlock"
    />

    <OnboardingDialog v-model="onboardingVisible" @complete="handleCompleteOnboarding" />

    <ChangeMasterPasswordDialog
      v-model="changeMasterPasswordVisible"
      :loading="busy.changingMasterPassword"
      @submit="handleChangeMasterPassword"
    />

    <PasswordEditorDialog
      v-model="editorVisible"
      :initial-draft="editorDraft"
      :loading="busy.saving"
      :password-injection="passwordInjection"
      @save="handleSaveDraft"
    />

    <DeleteConfirmDialog
      v-model="deleteDialogVisible"
      :title="pendingDeleteTitle"
      :count="deleteDialogCount"
      :loading="busy.deleting"
      @confirm="handleConfirmDelete"
    />

    <SyncConfirmDialog
      v-model="syncState.confirmVisible"
      :loading="busy.lanConfirming"
      :source-label="syncState.confirmSourceLabel"
      :local-preview="syncState.confirmLocalPreview"
      :remote-preview="syncState.confirmRemotePreview"
      @confirm="handleConfirmSync"
    />

    <AppSnackbar v-model="snackbar.show" :text="snackbar.text" :color="snackbar.color" />
  </v-app>
</template>
