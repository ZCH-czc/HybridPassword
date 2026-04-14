<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from "vue";
import { useLocale, useTheme } from "vuetify";
import AppBottomNav from "@/components/AppBottomNav.vue";
import AppSnackbar from "@/components/AppSnackbar.vue";
import AppTopBar from "@/components/AppTopBar.vue";
import ChangeMasterPasswordDialog from "@/components/ChangeMasterPasswordDialog.vue";
import ClearAllDataDialog from "@/components/ClearAllDataDialog.vue";
import DeleteConfirmDialog from "@/components/DeleteConfirmDialog.vue";
import FirstUseGuideOverlay from "@/components/FirstUseGuideOverlay.vue";
import ImportReviewDialog from "@/components/ImportReviewDialog.vue";
import IncrementalSyncDialog from "@/components/IncrementalSyncDialog.vue";
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
import {
  formatAppLogsAsText,
  installGlobalAppLogCapture,
  loadAppLogs as loadStoredAppLogs,
  mergeAppLogEntry,
  persistAppLogs,
} from "@/utils/app-logs";
import { usePasskeyVault } from "@/composables/usePasskeyVault";
import { usePasswordVault } from "@/composables/usePasswordVault";
import { createEmptyPasswordDraft } from "@/models/password-item";
import packageInfo from "../package.json";
import {
  copyTextToClipboard,
  downloadBlobFile,
  pickFileFromBrowser,
  readFileAsBytes,
} from "@/utils/browser-utils";
import { buildCsvContent, buildTxtContent } from "@/utils/csv-utils";
import { parseImportedFileData } from "@/utils/import-utils";
import {
  disableBiometricUnlock,
  decodeHostFileBytes,
  deletePasskeyWithHost,
  downloadLanSnapshotWithHost,
  downloadSnapshotFromWebDavWithHost,
  enableBiometricUnlock,
  getHostBridgeState,
  getPasskeyState,
  getSyncSettingsWithHost,
  launchPasskeyCompanionWithHost,
  restartPasskeyCompanionWithHost,
  listPasskeysWithHost,
  openAutostartSettings,
  pickImportFileWithHost,
  publishLanSnapshotWithHost,
  refreshPasskeyMetadataWithHost,
  resolvePasskeyOperationWithHost,
  saveTextFileWithHost,
  saveWebDavSettingsWithHost,
  scanLanDevicesWithHost,
  clearHostStoredData,
  setBackgroundAutoLockMinutes,
  setExcludeFromRecents,
  setLanDeviceNameWithHost,
  setLaunchAtStartup,
  setMinimizeToTray,
  setPasskeyCompanionAutoLaunchWithHost,
  setTrayAutoLockMinutes,
  unlockWithBiometric,
  updateStoredMasterPassword,
  uploadLanMergedRecordsWithHost,
  uploadSnapshotToWebDavWithHost,
} from "@/utils/native-bridge";
import { parseEncryptedVaultSnapshot } from "@/utils/vault-sync";
import {
  getAppSettingsRecord,
  getPendingImportReviewRecord,
  saveAppSettingsRecord,
  savePendingImportReviewRecord,
} from "@/utils/indexed-db";

const DEFAULT_BIOMETRIC_REAUTH_HOURS = 72;

const theme = useTheme();
const vuetifyLocale = useLocale();
const vault = usePasswordVault();
const passkeyVault = usePasskeyVault();
const {
  state: preferences,
  t,
  setLocale,
  setThemeMode,
  setNavAlignment,
  setSystemPrefersDark,
  getVuetifyLocale,
  resolvedTheme,
} = useAppPreferences();

const currentView = ref("home");
const listMode = ref("all");
const searchText = ref("");
const editorVisible = ref(false);
const deleteDialogVisible = ref(false);
const importReviewVisible = ref(false);
const onboardingVisible = ref(false);
const changeMasterPasswordVisible = ref(false);
const clearAllDataVisible = ref(false);
const editorDraft = ref(createEmptyPasswordDraft());
const importStrategy = ref("overwrite");
const passwordInjection = ref({
  nonce: 0,
  value: "",
});
const pendingDeleteIds = ref([]);
const pendingDeleteTitle = ref("");
const pendingImportedReviewItems = ref([]);
const pendingImportedReviewUpdatedAt = ref(0);
const pendingImportReviewManualQueue = ref([]);
const activePendingImportReviewId = ref("");
const pendingLanPublishTimer = ref(null);
const pendingBottomNavHideTimer = ref(null);
const pendingBottomNavShowTimer = ref(null);
let colorSchemeMediaQuery = null;
let disposeAppLogCapture = null;
let appLogPersistTask = Promise.resolve();

const appVersion = packageInfo.version || "0.0.0";

const settings = reactive({
  onboardingCompleted: false,
  biometricReauthHours: DEFAULT_BIOMETRIC_REAUTH_HOURS,
  onboardingInProgress: false,
  onboardingStep: "appearance",
  onboardingStartedWithSetup: false,
  firstUseGuideCompleted: false,
  firstUseGuideInProgress: false,
  firstUseGuideStep: "create",
});

const unlockState = reactive({
  lastMethod: "",
  biometricAttempted: false,
});

const securityState = reactive({
  secretKey: "",
  secretKeyLoading: false,
});

const passkeyBridge = reactive({
  isSupported: false,
  supportsMetadataManagement: false,
  supportsPluginManager: false,
  requiresCompanionApp: true,
  companionAppIntegrated: false,
  canLaunchCompanionApp: false,
  supportsCompanionAutoLaunch: false,
  companionAutoLaunchEnabled: false,
  apiVersion: 0,
  hasPlatformAuthenticator: false,
  platform: "web",
  message: "",
  pluginStatus: "",
  companionCheckedAtUnixTimeMs: 0,
  companionBuildNumber: 0,
  companionUbr: 0,
  companionMeetsPluginBuildRequirement: false,
  companionWebAuthnLibraryAvailable: false,
  companionPluginExportsAvailable: false,
  companionIsPackagedProcess: false,
  companionStatusSummary: "",
  companionDetailMessage: "",
  companionWorkflowMode: "skeleton",
  companionRegistrationAttempted: false,
  companionRegistrationPrepared: false,
  companionRegistrationEnvironmentReady: false,
  companionRegistrationCompleted: false,
  companionLastRegistrationAttemptUnixTimeMs: 0,
  companionRegistrationStatus: "",
  companionLastRegistrationMessage: "",
  companionLastRegistrationHResultHex: "",
  companionAuthenticatorStateCode: 0,
  companionAuthenticatorStateLabel: "unknown",
  companionHasOperationSigningPublicKey: false,
  companionOperationSigningPublicKeyStoredAtUnixTimeMs: 0,
  companionComSkeletonReady: false,
  companionComClassIdMatchesManifest: false,
  companionComFactoryReady: false,
  companionComAuthenticatorReady: false,
  companionComLastProbeUnixTimeMs: 0,
  companionComLastProbeMessage: "",
  companionComAuthenticatorTypeName: "",
  companionComClassFactoryRegistered: false,
  companionComClassFactoryRegistrationCookie: 0,
  companionComClassFactoryLastRegistrationUnixTimeMs: 0,
  companionComClassFactoryLastMessage: "",
  companionComClassFactoryLastHResultHex: "",
  companionCallbackTotalCount: 0,
  companionCallbackMakeCredentialCount: 0,
  companionCallbackGetAssertionCount: 0,
  companionCallbackCancelOperationCount: 0,
  companionCallbackGetLockStatusCount: 0,
  companionCallbackLastUnixTimeMs: 0,
  companionCallbackLastKind: "",
  companionCallbackLastMessage: "",
  companionCallbackLastHResultHex: "",
  companionLatestOperationId: "",
  companionLatestOperationKind: "",
  companionLatestOperationState: "idle",
  companionLatestOperationSource: "",
  companionLatestOperationCreatedAtUnixTimeMs: 0,
  companionLatestOperationUpdatedAtUnixTimeMs: 0,
  companionLatestOperationRequestPointerPresent: false,
  companionLatestOperationResponsePointerPresent: false,
  companionLatestOperationCancelPointerPresent: false,
  companionLatestOperationMessage: "",
  companionLatestOperationHResultHex: "",
  companionActivationCount: 0,
  companionLastActivationUnixTimeMs: 0,
  companionLastActivationSource: "",
  companionStartedFromPluginActivation: false,
  companionCreateRequestCount: 0,
  companionLastCreateRequestUnixTimeMs: 0,
  companionLastCreateRequestRpId: "",
  companionLastCreateRequestUsername: "",
  companionLastCreateRequestMessage: "",
  recentLogs: [],
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
  trayAutoLockMinutes: 0,
  supportsExcludeFromRecents: false,
  excludeFromRecentsEnabled: false,
  backgroundAutoLockMinutes: 0,
  supportsAutostartSettingsShortcut: false,
  supportsWebDavSync: false,
  supportsLanSync: false,
  supportsPasskeys: false,
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
  importReviewActing: false,
  lanScanning: false,
  lanSavingDeviceName: false,
  lanConfirming: false,
  clearingData: false,
  deletedBatchActing: false,
  appLogExporting: false,
  passkeyRefreshing: false,
  passkeyLaunching: false,
  passkeyAutoLaunchSaving: false,
  passkeyOperationResolving: false,
});

const itemLoadingState = reactive({
  revealingIds: {},
  editingIds: {},
  favoriteIds: {},
  deletedBusyIds: {},
  passkeyBusyIds: {},
});

const selection = reactive({
  active: false,
  ids: [],
});
const shouldRenderBottomNav = ref(false);
const bottomNavVisible = ref(false);
const unlockScreenSettled = ref(false);
const firstUseGuideVisible = ref(false);

const ONBOARDING_STEPS = ["appearance", "setup", "security", "sharing"];
const FIRST_USE_GUIDE_STEPS = ["create", "summary", "nav", "search", "settings"];

const snackbar = reactive({
  show: false,
  text: "",
  color: "success",
});

const appLogState = reactive({
  entries: [],
  updatedAt: 0,
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
  incrementalVisible: false,
  incrementalSourceLabel: "",
  incrementalPlan: null,
  incrementalTargetKind: "none",
  incrementalTargetDevice: null,
});

const isLocked = computed(() => !vault.state.unlocked);
const masterDialogVisible = computed(() => !vault.state.bootstrapping && !vault.state.unlocked);
const masterDialogMode = computed(() => (vault.state.requiresSetup ? "setup" : "unlock"));
const shouldShowSearchBar = computed(
  () => currentView.value === "home" || currentView.value === "list"
);
const onboardingFlowVisible = computed(
  () => !vault.state.bootstrapping && !settings.onboardingCompleted && onboardingVisible.value
);
const shouldShowMasterUnlockScreen = computed(
  () => masterDialogVisible.value && !(masterDialogMode.value === "setup" && onboardingFlowVisible.value)
);
const shouldShowBottomNav = computed(
  () =>
    !vault.state.bootstrapping &&
    vault.state.unlocked &&
    unlockScreenSettled.value &&
    !onboardingFlowVisible.value
);
const shouldShowFirstUseGuide = computed(
  () =>
    !vault.state.bootstrapping &&
    vault.state.unlocked &&
    !onboardingFlowVisible.value &&
    !settings.firstUseGuideCompleted &&
    firstUseGuideVisible.value
);
const biometricUnlockReady = computed(
  () =>
    host.isSupported &&
    host.isBiometricAvailable &&
    host.isBiometricEnabled &&
    masterDialogMode.value === "unlock"
);

function normalizeOnboardingStep(step, includeSetup = settings.onboardingStartedWithSetup) {
  if (!ONBOARDING_STEPS.includes(step)) {
    return includeSetup ? "appearance" : "security";
  }

  if ((step === "appearance" || step === "setup") && !includeSetup) {
    return "security";
  }

  return step;
}

function normalizeFirstUseGuideStep(step) {
  return FIRST_USE_GUIDE_STEPS.includes(step) ? step : "create";
}

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
const passkeyRecords = computed(() => passkeyVault.records.value);
const deletedPasskeyRecords = computed(() => passkeyVault.deletedRecords.value);
const passkeyUiSupported = computed(() => host.supportsPasskeys || host.platform === "windows");
const favoriteRecords = computed(() =>
  searchedRecords.value.filter((record) => record.isFavorite)
);
const searchedPasskeyRecords = computed(() => {
  const keyword = searchText.value.trim().toLowerCase();
  if (!keyword) {
    return passkeyRecords.value;
  }

  return passkeyRecords.value.filter((record) => {
    const searchArea = [
      record.rpId,
      record.username,
      record.displayName,
      record.origin,
      record.credentialId,
    ]
      .join(" ")
      .toLowerCase();
    return searchArea.includes(keyword);
  });
});

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
const secretKeyHint = computed(() => vault.vaultConfig.value?.secretKeyHint || "");

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

function getAppLogCopy() {
  const isZh = String(preferences.locale || "").toLowerCase().startsWith("zh");
  return isZh
    ? {
        exportDone: "应用日志已导出。",
        exportFailed: "导出应用日志失败。",
        sessionStarted: "应用会话已启动。",
      }
    : {
        exportDone: "Application logs exported.",
        exportFailed: "Unable to export application logs.",
        sessionStarted: "Application session started.",
      };
}

function getLogLevelFromTone(color) {
  if (color === "error") {
    return "error";
  }

  if (color === "warning") {
    return "warning";
  }

  return "info";
}

function queuePersistAppLogs() {
  const entriesSnapshot = appLogState.entries.map((entry) => ({ ...entry }));
  const updatedAt = appLogState.updatedAt;

  appLogPersistTask = appLogPersistTask
    .catch(() => {})
    .then(() => persistAppLogs(entriesSnapshot, updatedAt))
    .catch((error) => {
      console.warn("Unable to persist application logs.", error);
    });
}

function addAppLog(entry) {
  if (!entry?.message) {
    return;
  }

  appLogState.entries = mergeAppLogEntry(appLogState.entries, entry);
  appLogState.updatedAt = Date.now();
  queuePersistAppLogs();
}

async function hydrateAppLogs() {
  try {
    const record = await loadStoredAppLogs();
    appLogState.entries = Array.isArray(record.entries) ? record.entries : [];
    appLogState.updatedAt = Number(record.updatedAt || 0);
  } catch (error) {
    console.warn("Unable to load persisted application logs.", error);
  }
}

function notify(text, color = "success") {
  snackbar.text = text;
  snackbar.color = color;
  snackbar.show = true;
  addAppLog({
    level: getLogLevelFromTone(color),
    source: "ui",
    message: text,
  });
}

function getPasskeyUiCopy() {
  const isZh = String(preferences.locale || "").toLowerCase().startsWith("zh");
  return isZh
    ? {
        refreshDone: "Passkey 元数据已刷新。",
        refreshEmpty: "当前还没有可显示的 passkey 元数据。",
        refreshFailed: "刷新 passkey 元数据失败。",
        removeDone: "Passkey 元数据已移入最近删除。",
        removeFailed: "移入最近删除失败。",
        restoreDone: "Passkey 元数据已恢复。",
        restoreFailed: "恢复 passkey 元数据失败。",
        deleteDone: "Passkey 元数据已彻底删除。",
        deleteFailed: "彻底删除 passkey 元数据失败。",
      }
    : {
        refreshDone: "Passkey metadata refreshed.",
        refreshEmpty: "No passkey metadata is available yet.",
        refreshFailed: "Unable to refresh passkey metadata.",
        removeDone: "Passkey metadata moved to recently deleted.",
        removeFailed: "Unable to move the passkey metadata to deleted.",
        restoreDone: "Passkey metadata restored.",
        restoreFailed: "Unable to restore the passkey metadata.",
        deleteDone: "Passkey metadata was permanently deleted.",
        deleteFailed: "Unable to permanently delete the passkey metadata.",
      };
}

function hasPendingOnboarding() {
  return !settings.onboardingCompleted && settings.onboardingInProgress;
}

function shouldResumeOnboardingAfterUnlock() {
  const resumeStep = normalizeOnboardingStep(settings.onboardingStep, true);
  return hasPendingOnboarding() && resumeStep !== "appearance" && resumeStep !== "setup";
}

function hasPendingFirstUseGuide() {
  return settings.onboardingCompleted && !settings.firstUseGuideCompleted && settings.firstUseGuideInProgress;
}

function applyFirstUseGuideView(step) {
  if (step === "settings") {
    currentView.value = "settings";
    return;
  }

  if (step === "search") {
    currentView.value = "list";
    return;
  }

  currentView.value = "home";
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

function applyNavAlignmentPreference(alignment) {
  setNavAlignment(alignment || "center");
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
    navAlignment: preferences.navAlignment,
    locale: preferences.locale,
    onboardingCompleted: settings.onboardingCompleted,
    biometricReauthHours: settings.biometricReauthHours,
    onboardingInProgress: settings.onboardingInProgress,
    onboardingStep: settings.onboardingStep,
    onboardingStartedWithSetup: settings.onboardingStartedWithSetup,
    firstUseGuideCompleted: settings.firstUseGuideCompleted,
    firstUseGuideInProgress: settings.firstUseGuideInProgress,
    firstUseGuideStep: settings.firstUseGuideStep,
  });
}

async function persistPendingImportReviewItems() {
  await savePendingImportReviewRecord({
    items: pendingImportedReviewItems.value,
    updatedAt: pendingImportedReviewUpdatedAt.value,
  });
}

async function loadPendingImportReviewItems() {
  const record = await getPendingImportReviewRecord();
  pendingImportedReviewItems.value = Array.isArray(record?.items) ? record.items : [];
  pendingImportedReviewUpdatedAt.value = Number(record?.updatedAt || 0);
}

async function replacePendingImportReviewItems(items, updatedAt = Date.now()) {
  pendingImportedReviewItems.value = Array.isArray(items) ? items : [];
  pendingImportedReviewUpdatedAt.value = pendingImportedReviewItems.value.length ? updatedAt : 0;
  await persistPendingImportReviewItems();
}

async function appendPendingImportReviewItems(items) {
  if (!Array.isArray(items) || !items.length) {
    return;
  }

  const mergedItems = [...pendingImportedReviewItems.value, ...items];
  await replacePendingImportReviewItems(mergedItems, Date.now());
}

async function removePendingImportReviewItems(ids) {
  const idSet = new Set(Array.isArray(ids) ? ids.filter(Boolean) : []);
  if (!idSet.size) {
    return;
  }

  await replacePendingImportReviewItems(
    pendingImportedReviewItems.value.filter((item) => !idSet.has(item.id)),
    Date.now()
  );
}

async function loadAppSettings() {
  try {
    const record = await getAppSettingsRecord();
    applyLocalePreference(record?.locale || getDefaultLocale());
    applyThemeModePreference(record?.themeMode || "system");
    applyNavAlignmentPreference(record?.navAlignment || "center");
    settings.onboardingCompleted = Boolean(record?.onboardingCompleted);
    settings.biometricReauthHours = Number(
      record?.biometricReauthHours ?? DEFAULT_BIOMETRIC_REAUTH_HOURS
    );
    settings.onboardingInProgress = Boolean(record?.onboardingInProgress);
    settings.onboardingStartedWithSetup = Boolean(record?.onboardingStartedWithSetup);
    settings.onboardingStep = normalizeOnboardingStep(
      record?.onboardingStep,
      settings.onboardingStartedWithSetup
    );
    settings.firstUseGuideCompleted = Boolean(record?.firstUseGuideCompleted);
    settings.firstUseGuideInProgress = Boolean(record?.firstUseGuideInProgress);
    settings.firstUseGuideStep = normalizeFirstUseGuideStep(record?.firstUseGuideStep);
  } catch {
    applyLocalePreference(getDefaultLocale());
    applyThemeModePreference("system");
    applyNavAlignmentPreference("center");
    settings.biometricReauthHours = DEFAULT_BIOMETRIC_REAUTH_HOURS;
    settings.onboardingInProgress = false;
    settings.onboardingStartedWithSetup = false;
    settings.onboardingStep = "appearance";
    settings.firstUseGuideCompleted = false;
    settings.firstUseGuideInProgress = false;
    settings.firstUseGuideStep = "create";
  }
}

async function persistOnboardingState() {
  try {
    await persistSettings();
  } catch {
    notify(t("notify.onboardingSaveFailed"), "warning");
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

async function hydrateSyncSettingsInBackground() {
  try {
    await loadSyncSettings();
  } catch (error) {
    console.warn("Failed to load sync settings during startup.", error);
  }
}

async function hydratePasskeysInBackground() {
  try {
    await Promise.all([passkeyVault.loadRecords(), refreshPasskeyBridgeState()]);

    if (!passkeyBridge.isSupported) {
      return;
    }

    const hostItems = await listPasskeysWithHost();
    await passkeyVault.syncFromHostMetadata(hostItems);
  } catch (error) {
    console.warn("Failed to load passkey metadata during startup.", error);
  }
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
  host.trayAutoLockMinutes = state.trayAutoLockMinutes;
  host.supportsExcludeFromRecents = state.supportsExcludeFromRecents;
  host.excludeFromRecentsEnabled = state.excludeFromRecentsEnabled;
  host.backgroundAutoLockMinutes = state.backgroundAutoLockMinutes;
  host.supportsAutostartSettingsShortcut = state.supportsAutostartSettingsShortcut;
  host.supportsWebDavSync = state.supportsWebDavSync;
  host.supportsLanSync = state.supportsLanSync;
  host.supportsPasskeys = state.supportsPasskeys;
  if (!host.isBiometricEnabled) {
    unlockState.biometricAttempted = false;
  }
  applySafeAreaInsets(state.safeAreaTop, state.safeAreaBottom);
}

async function refreshPasskeyBridgeState() {
  const state = await getPasskeyState();
  passkeyBridge.isSupported = state.isSupported;
  passkeyBridge.supportsMetadataManagement = state.supportsMetadataManagement;
  passkeyBridge.supportsPluginManager = state.supportsPluginManager;
  passkeyBridge.requiresCompanionApp = state.requiresCompanionApp;
  passkeyBridge.companionAppIntegrated = state.companionAppIntegrated;
  passkeyBridge.canLaunchCompanionApp = state.canLaunchCompanionApp;
  passkeyBridge.supportsCompanionAutoLaunch = state.supportsCompanionAutoLaunch;
  passkeyBridge.companionAutoLaunchEnabled = state.companionAutoLaunchEnabled;
  passkeyBridge.apiVersion = state.apiVersion;
  passkeyBridge.hasPlatformAuthenticator = state.hasPlatformAuthenticator;
  passkeyBridge.platform = state.platform;
  passkeyBridge.message = state.message;
  passkeyBridge.pluginStatus = state.pluginStatus;
  passkeyBridge.companionCheckedAtUnixTimeMs = state.companionCheckedAtUnixTimeMs;
  passkeyBridge.companionBuildNumber = state.companionBuildNumber;
  passkeyBridge.companionUbr = state.companionUbr;
  passkeyBridge.companionMeetsPluginBuildRequirement = state.companionMeetsPluginBuildRequirement;
  passkeyBridge.companionWebAuthnLibraryAvailable = state.companionWebAuthnLibraryAvailable;
  passkeyBridge.companionPluginExportsAvailable = state.companionPluginExportsAvailable;
  passkeyBridge.companionIsPackagedProcess = state.companionIsPackagedProcess;
  passkeyBridge.companionStatusSummary = state.companionStatusSummary;
  passkeyBridge.companionDetailMessage = state.companionDetailMessage;
  passkeyBridge.companionWorkflowMode = state.companionWorkflowMode;
  passkeyBridge.companionRegistrationAttempted = state.companionRegistrationAttempted;
  passkeyBridge.companionRegistrationPrepared = state.companionRegistrationPrepared;
  passkeyBridge.companionRegistrationEnvironmentReady = state.companionRegistrationEnvironmentReady;
  passkeyBridge.companionRegistrationCompleted = state.companionRegistrationCompleted;
  passkeyBridge.companionLastRegistrationAttemptUnixTimeMs =
    state.companionLastRegistrationAttemptUnixTimeMs;
  passkeyBridge.companionRegistrationStatus = state.companionRegistrationStatus;
  passkeyBridge.companionLastRegistrationMessage = state.companionLastRegistrationMessage;
  passkeyBridge.companionLastRegistrationHResultHex = state.companionLastRegistrationHResultHex;
  passkeyBridge.companionAuthenticatorStateCode = state.companionAuthenticatorStateCode;
  passkeyBridge.companionAuthenticatorStateLabel = state.companionAuthenticatorStateLabel;
  passkeyBridge.companionHasOperationSigningPublicKey = state.companionHasOperationSigningPublicKey;
  passkeyBridge.companionOperationSigningPublicKeyStoredAtUnixTimeMs =
    state.companionOperationSigningPublicKeyStoredAtUnixTimeMs;
  passkeyBridge.companionActivationCount = state.companionActivationCount;
  passkeyBridge.companionLastActivationUnixTimeMs = state.companionLastActivationUnixTimeMs;
  passkeyBridge.companionLastActivationSource = state.companionLastActivationSource;
  passkeyBridge.companionStartedFromPluginActivation = state.companionStartedFromPluginActivation;
  passkeyBridge.companionCreateRequestCount = state.companionCreateRequestCount;
  passkeyBridge.companionLastCreateRequestUnixTimeMs = state.companionLastCreateRequestUnixTimeMs;
  passkeyBridge.companionLastCreateRequestRpId = state.companionLastCreateRequestRpId;
  passkeyBridge.companionLastCreateRequestUsername = state.companionLastCreateRequestUsername;
  passkeyBridge.companionLastCreateRequestMessage = state.companionLastCreateRequestMessage;
  passkeyBridge.recentLogs = Array.isArray(state.recentLogs) ? state.recentLogs : [];
}

async function syncBiometricState(markManualUnlock = false, overrideVaultKeyBase64 = "") {
  if (!host.isBiometricEnabled) {
    return { success: true };
  }

  const vaultKeyBase64 =
    overrideVaultKeyBase64 || (vault.state.unlocked ? vault.getVaultKeyBase64() : "");

  const result = await updateStoredMasterPassword(
    vaultKeyBase64,
    settings.biometricReauthHours,
    markManualUnlock
  );
  await refreshHostState();
  return result;
}

function resetSelection() {
  selection.active = false;
  selection.ids = [];
}

function openCreateDialog() {
  editorDraft.value = createEmptyPasswordDraft();
  editorVisible.value = true;
}

function buildImportReviewDraft(item) {
  return {
    id: "",
    siteName: item?.siteName || "",
    username: item?.username || "",
    password: item?.password || "",
    notes:
      Array.isArray(item?.notes) && item.notes.length ? [...item.notes] : [item?.reason || ""],
    isFavorite: false,
  };
}

function openPendingImportReviewItem(item) {
  if (!item) {
    return;
  }

  activePendingImportReviewId.value = item.id;
  editorDraft.value = buildImportReviewDraft(item);
  editorVisible.value = true;
}

function advancePendingImportReviewQueue() {
  while (pendingImportReviewManualQueue.value.length) {
    const nextId = pendingImportReviewManualQueue.value.shift();
    const nextItem = pendingImportedReviewItems.value.find((item) => item.id === nextId);
    if (nextItem) {
      openPendingImportReviewItem(nextItem);
      return;
    }
  }

  activePendingImportReviewId.value = "";
}

function startPendingImportReviewManualAdd(items) {
  const manualItems = Array.isArray(items) ? items.filter(Boolean) : [];
  if (!manualItems.length) {
    return;
  }

  importReviewVisible.value = false;
  pendingImportReviewManualQueue.value = manualItems.map((item) => item.id);
  advancePendingImportReviewQueue();
}

function handleEditorVisibilityChange(value) {
  editorVisible.value = value;

  if (!value && activePendingImportReviewId.value) {
    activePendingImportReviewId.value = "";
    pendingImportReviewManualQueue.value = [];
  }
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

async function unlockVaultWithPassphrase(passphrase, secretKey = "") {
  try {
    const unlockResult = await vault.submitMasterPassword(passphrase, secretKey);
    unlockState.lastMethod = "password";
    securityState.secretKey = unlockResult.generatedSecretKey || "";

    if (host.isBiometricEnabled) {
      const syncResult = await syncBiometricState(true);
      if (!syncResult.success) {
        notify(syncResult.message || t("notify.biometricKeySyncFailed"), "warning");
      }
    }

    if (unlockResult.generatedSecretKey) {
      notify(t("notify.secretKeyCreated"), "info");
    }

    if (shouldResumeOnboardingAfterUnlock()) {
      onboardingVisible.value = true;
    }

    return true;
  } catch (error) {
    notify(error.message || t("notify.unlockFailed"), "error");
    return false;
  }
}

async function handleMasterSubmit(payload) {
  await unlockVaultWithPassphrase(payload.passphrase, payload.secretKey);
}

async function handleBiometricUnlock() {
  busy.biometricUnlocking = true;

  try {
    const result = await unlockWithBiometric();
    if (!result.success || !result.vaultKeyBase64) {
      notify(
        result.message ||
          (result.requiresManualUnlock
            ? t("notify.biometricManualUnlockRequired")
            : t("notify.biometricUnavailable", { label: host.biometricLabel })),
        result.requiresManualUnlock ? "info" : "warning"
      );
      await refreshHostState();
      return;
    }

    try {
      await vault.unlockWithVaultKeyBase64(result.vaultKeyBase64);
      unlockState.lastMethod = "biometric";
      securityState.secretKey = "";
      if (shouldResumeOnboardingAfterUnlock()) {
        onboardingVisible.value = true;
      }
    } catch (error) {
      await disableBiometricUnlock();
      await refreshHostState();
      notify(error.message || t("notify.biometricStoredKeyInvalid"), "warning");
    }
  } finally {
    busy.biometricUnlocking = false;
  }
}

function handleLockVault() {
  vault.lockVault();
  passkeyVault.clearRecords();
  editorVisible.value = false;
  deleteDialogVisible.value = false;
  changeMasterPasswordVisible.value = false;
  clearAllDataVisible.value = false;
  syncState.incrementalVisible = false;
  syncState.incrementalPlan = null;
  syncState.incrementalSourceLabel = "";
  syncState.incrementalTargetKind = "none";
  syncState.incrementalTargetDevice = null;
  pendingDeleteIds.value = [];
  pendingDeleteTitle.value = "";
  unlockState.lastMethod = "";
  unlockState.biometricAttempted = false;
  securityState.secretKey = "";
  unlockScreenSettled.value = false;
  onboardingVisible.value = false;
  firstUseGuideVisible.value = false;
  resetSelection();
}

function resetInMemoryAppStateAfterWipe() {
  applyLocalePreference(getDefaultLocale());
  applyThemeModePreference("system");
  applyNavAlignmentPreference("center");
  passkeyVault.clearRecords();
  passkeyBridge.isSupported = false;
  passkeyBridge.supportsMetadataManagement = false;
  passkeyBridge.supportsPluginManager = false;
  passkeyBridge.requiresCompanionApp = true;
  passkeyBridge.companionAppIntegrated = false;
  passkeyBridge.canLaunchCompanionApp = false;
  passkeyBridge.supportsCompanionAutoLaunch = false;
  passkeyBridge.companionAutoLaunchEnabled = false;
  passkeyBridge.apiVersion = 0;
  passkeyBridge.hasPlatformAuthenticator = false;
  passkeyBridge.platform = "web";
  passkeyBridge.message = "";
  passkeyBridge.pluginStatus = "";
  passkeyBridge.companionCheckedAtUnixTimeMs = 0;
  passkeyBridge.companionBuildNumber = 0;
  passkeyBridge.companionUbr = 0;
  passkeyBridge.companionMeetsPluginBuildRequirement = false;
  passkeyBridge.companionWebAuthnLibraryAvailable = false;
  passkeyBridge.companionPluginExportsAvailable = false;
  passkeyBridge.companionIsPackagedProcess = false;
  passkeyBridge.companionStatusSummary = "";
  passkeyBridge.companionDetailMessage = "";
  passkeyBridge.companionWorkflowMode = "skeleton";
  passkeyBridge.companionRegistrationAttempted = false;
  passkeyBridge.companionRegistrationPrepared = false;
  passkeyBridge.companionRegistrationEnvironmentReady = false;
  passkeyBridge.companionRegistrationCompleted = false;
  passkeyBridge.companionLastRegistrationAttemptUnixTimeMs = 0;
  passkeyBridge.companionRegistrationStatus = "";
  passkeyBridge.companionLastRegistrationMessage = "";
  passkeyBridge.companionLastRegistrationHResultHex = "";
  passkeyBridge.companionAuthenticatorStateCode = 0;
  passkeyBridge.companionAuthenticatorStateLabel = "unknown";
  passkeyBridge.companionHasOperationSigningPublicKey = false;
  passkeyBridge.companionOperationSigningPublicKeyStoredAtUnixTimeMs = 0;
  passkeyBridge.companionActivationCount = 0;
  passkeyBridge.companionLastActivationUnixTimeMs = 0;
  passkeyBridge.companionLastActivationSource = "";
  passkeyBridge.companionStartedFromPluginActivation = false;
  passkeyBridge.companionCreateRequestCount = 0;
  passkeyBridge.companionLastCreateRequestUnixTimeMs = 0;
  passkeyBridge.companionLastCreateRequestRpId = "";
  passkeyBridge.companionLastCreateRequestUsername = "";
  passkeyBridge.companionLastCreateRequestMessage = "";
  passkeyBridge.recentLogs = [];

  currentView.value = "home";
  listMode.value = "all";
  searchText.value = "";
  importStrategy.value = "overwrite";
  editorDraft.value = createEmptyPasswordDraft();
  passwordInjection.value = {
    nonce: 0,
    value: "",
  };

  settings.onboardingCompleted = false;
  settings.biometricReauthHours = DEFAULT_BIOMETRIC_REAUTH_HOURS;
  settings.onboardingInProgress = true;
  settings.onboardingStep = "appearance";
  settings.onboardingStartedWithSetup = true;
  settings.firstUseGuideCompleted = false;
  settings.firstUseGuideInProgress = false;
  settings.firstUseGuideStep = "create";

  syncSettings.deviceId = "";
  syncSettings.deviceName = "";
  syncSettings.webDav.baseUrl = "";
  syncSettings.webDav.remotePath = "";
  syncSettings.webDav.username = "";
  syncSettings.webDav.hasPassword = false;
  syncState.lanDevices = [];
  syncState.confirmVisible = false;
  syncState.confirmSourceLabel = "";
  syncState.confirmSnapshotText = "";
  syncState.confirmLocalPreview = emptyPreview();
  syncState.confirmRemotePreview = emptyPreview();
  syncState.incrementalVisible = false;
  syncState.incrementalSourceLabel = "";
  syncState.incrementalPlan = null;
  syncState.incrementalTargetKind = "none";
  syncState.incrementalTargetDevice = null;

  pendingDeleteIds.value = [];
  pendingDeleteTitle.value = "";
  pendingImportedReviewItems.value = [];
  pendingImportedReviewUpdatedAt.value = 0;
  pendingImportReviewManualQueue.value = [];
  activePendingImportReviewId.value = "";
  unlockState.lastMethod = "";
  unlockState.biometricAttempted = false;
  securityState.secretKey = "";
  securityState.secretKeyLoading = false;
  unlockScreenSettled.value = false;
  firstUseGuideVisible.value = false;
  onboardingVisible.value = true;
  importReviewVisible.value = false;
  clearAllDataVisible.value = false;
  resetSelection();
}

function handleUnlockScreenClosed() {
  if (!vault.state.unlocked) {
    return;
  }

  unlockScreenSettled.value = true;
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
    const completedPendingImportReviewId = activePendingImportReviewId.value;
    await vault.saveDraft(draft);
    editorVisible.value = false;
    editorDraft.value = createEmptyPasswordDraft();

    if (completedPendingImportReviewId) {
      await removePendingImportReviewItems([completedPendingImportReviewId]);
      activePendingImportReviewId.value = "";
    }

    notify(existed ? t("notify.recordUpdated") : t("notify.recordCreated"));

    if (pendingImportReviewManualQueue.value.length) {
      await nextTick();
      advancePendingImportReviewQueue();
    }
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

async function handleRestoreDeletedMany(recordIds) {
  if (!recordIds?.length) {
    return;
  }

  busy.deletedBatchActing = true;
  recordIds.forEach((recordId) => {
    itemLoadingState.deletedBusyIds[recordId] = true;
  });

  try {
    for (const recordId of recordIds) {
      await vault.restoreRecord(recordId);
    }
    notify(t("notify.restoredMany", { count: recordIds.length }));
  } catch (error) {
    notify(error.message || t("notify.restoreFailed"), "error");
  } finally {
    recordIds.forEach((recordId) => {
      delete itemLoadingState.deletedBusyIds[recordId];
    });
    busy.deletedBatchActing = false;
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

async function handlePermanentDeleteMany(recordIds) {
  if (!recordIds?.length) {
    return;
  }

  busy.deletedBatchActing = true;
  recordIds.forEach((recordId) => {
    itemLoadingState.deletedBusyIds[recordId] = true;
  });

  try {
    for (const recordId of recordIds) {
      await vault.permanentlyDeleteRecord(recordId);
    }
    notify(t("notify.permanentlyDeletedMany", { count: recordIds.length }));
  } catch (error) {
    notify(error.message || t("notify.permanentDeleteFailed"), "error");
  } finally {
    recordIds.forEach((recordId) => {
      delete itemLoadingState.deletedBusyIds[recordId];
    });
    busy.deletedBatchActing = false;
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
    let fileName = "";
    let bytes = new Uint8Array();

    if (host.supportsNativeFileDialogs) {
      const result = await pickImportFileWithHost();
      if (result.cancelled) {
        return;
      }

      if (!result.success) {
        throw new Error(result.message || t("notify.importFailed"));
      }

      fileName = result.fileName;
      bytes = decodeHostFileBytes(result);
      if (!bytes.length && result.content) {
        bytes = new TextEncoder().encode(result.content);
      }
    } else {
      const file = await pickFileFromBrowser({
        accept: ".csv,.1pux,.zip,text/csv,application/zip,application/x-zip-compressed",
      });
      fileName = file.name;
      bytes = await readFileAsBytes(file);
    }

    const parsedImport = parseImportedFileData({
      fileName,
      bytes,
    });
    const importResult = await vault.importEntriesFromExternalSource(parsedImport, strategy);
    notify(t("notify.importDone", importResult));

    if (Array.isArray(importResult.ignoredItems) && importResult.ignoredItems.length) {
      await appendPendingImportReviewItems(importResult.ignoredItems);
      importReviewVisible.value = true;
    }
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

function handleManualAddImportedItem(item) {
  startPendingImportReviewManualAdd([item]);
}

function handleManualAddImportedItems(items) {
  startPendingImportReviewManualAdd(items);
}

async function handleDismissImportedReviewItem(itemId) {
  busy.importReviewActing = true;

  try {
    await removePendingImportReviewItems([itemId]);
  } catch (error) {
    notify(error.message || t("notify.importFailed"), "error");
  } finally {
    busy.importReviewActing = false;
  }
}

async function handleDismissImportedReviewItems(itemIds) {
  busy.importReviewActing = true;

  try {
    await removePendingImportReviewItems(itemIds);
  } catch (error) {
    notify(error.message || t("notify.importFailed"), "error");
  } finally {
    busy.importReviewActing = false;
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
  if (listMode.value === "passkeys") {
    selection.active = false;
    selection.ids = [];
    return;
  }

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

async function handleUpdateNavAlignment(alignment) {
  try {
    applyNavAlignmentPreference(alignment);
    await persistSettings();
  } catch {
    notify(t("notify.themeFailed"), "error");
  }
}

async function handleCompleteOnboarding() {
  settings.onboardingCompleted = true;
  settings.onboardingInProgress = false;
  settings.onboardingStep = "sharing";
  settings.onboardingStartedWithSetup = false;
  onboardingVisible.value = false;

  if (vault.state.unlocked) {
    unlockScreenSettled.value = true;
  }

  await persistOnboardingState();

  if (!settings.firstUseGuideCompleted) {
    settings.firstUseGuideInProgress = true;
    settings.firstUseGuideStep = "create";
    applyFirstUseGuideView("create");
    firstUseGuideVisible.value = true;
    await persistOnboardingState();
  }
}

async function handleOnboardingSetupSubmit(payload) {
  settings.onboardingInProgress = true;
  settings.onboardingStartedWithSetup = true;
  settings.onboardingStep = "setup";
  onboardingVisible.value = true;
  await persistOnboardingState();

  const unlocked = await unlockVaultWithPassphrase(payload.passphrase, payload.secretKey || "");
  if (!unlocked) {
    return;
  }

  settings.onboardingStep = "security";
  onboardingVisible.value = true;
  await persistOnboardingState();
}

async function handleUpdateOnboardingStep(step) {
  const includeSetup = settings.onboardingStartedWithSetup || vault.state.requiresSetup;
  const normalizedStep = normalizeOnboardingStep(step, includeSetup);

  settings.onboardingInProgress = true;
  settings.onboardingStartedWithSetup = includeSetup;
  settings.onboardingStep = normalizedStep;
  onboardingVisible.value = true;
  await persistOnboardingState();
}

async function handleUpdateFirstUseGuideStep(step) {
  const normalizedStep = normalizeFirstUseGuideStep(step);
  settings.firstUseGuideInProgress = true;
  settings.firstUseGuideStep = normalizedStep;
  applyFirstUseGuideView(normalizedStep);
  firstUseGuideVisible.value = true;
  await persistOnboardingState();
}

async function handleCompleteFirstUseGuide() {
  settings.firstUseGuideCompleted = true;
  settings.firstUseGuideInProgress = false;
  settings.firstUseGuideStep = "create";
  firstUseGuideVisible.value = false;
  currentView.value = "home";
  await persistOnboardingState();
}

async function handleSkipFirstUseGuide() {
  await handleCompleteFirstUseGuide();
}

async function handleEnableBiometricUnlock() {
  if (!vault.state.unlocked || unlockState.lastMethod !== "password") {
    notify(t("notify.enterMasterPasswordFirst"), "info");
    return;
  }

  busy.biometricConfiguring = true;

  try {
    const result = await enableBiometricUnlock(
      vault.getVaultKeyBase64(),
      settings.biometricReauthHours
    );
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
    unlockState.lastMethod = "password";
    securityState.secretKey = "";

    if (host.isBiometricEnabled) {
      const syncResult = await syncBiometricState(true);
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

async function handleClearAllData(payload) {
  busy.clearingData = true;

  try {
    await vault.verifyCurrentPassphrase(payload.currentPassphrase);

    const hostResult = await clearHostStoredData();
    if (!hostResult.success) {
      notify(hostResult.message || t("notify.clearAllDataFailed"), "error");
      return;
    }

    await vault.clearAllData(payload.currentPassphrase);
    resetInMemoryAppStateAfterWipe();
    await refreshHostState();
    await loadSyncSettings();
    notify(t("notify.clearAllDataDone"));
  } catch (error) {
    notify(error.message || t("notify.clearAllDataFailed"), "error");
  } finally {
    busy.clearingData = false;
  }
}

async function handleUpdateBiometricReauthHours(hours) {
  const normalizedHours = Number(hours);
  if (Number.isNaN(normalizedHours)) {
    return;
  }

  busy.biometricConfiguring = true;

  try {
    settings.biometricReauthHours = normalizedHours;
    await persistSettings();

    if (host.isBiometricEnabled) {
      const result = await syncBiometricState(false, "");
      if (!result.success) {
        notify(result.message || t("notify.biometricReauthUpdateFailed"), "warning");
        return;
      }
    }

    notify(t("notify.biometricReauthUpdated"));
  } catch (error) {
    notify(error.message || t("notify.biometricReauthUpdateFailed"), "warning");
  } finally {
    busy.biometricConfiguring = false;
  }
}

async function handleRevealSecretKey() {
  if (!vault.state.unlocked) {
    notify(t("notify.unlockRequiredForSecretKey"), "info");
    return;
  }

  securityState.secretKeyLoading = true;

  try {
    securityState.secretKey = await vault.getSecretKey();
  } catch (error) {
    notify(error.message || t("notify.secretKeyRevealFailed"), "warning");
  } finally {
    securityState.secretKeyLoading = false;
  }
}

async function handleCopySecretKey() {
  try {
    if (!securityState.secretKey) {
      await handleRevealSecretKey();
    }

    if (!securityState.secretKey) {
      return;
    }

    await copyTextToClipboard(securityState.secretKey);
    notify(t("notify.secretKeyCopied"));
  } catch (error) {
    notify(error.message || t("notify.secretKeyCopyFailed"), "warning");
  }
}

async function handleExportAppLogs() {
  const copy = getAppLogCopy();
  busy.appLogExporting = true;

  try {
    const timestampLabel = new Date().toISOString().replace(/[:.]/g, "-");
    const fileName = `password-vault-logs-${timestampLabel}.txt`;
    const content = formatAppLogsAsText({
      appName: "Password Vault",
      appVersion,
      platform: host.platform,
      locale: preferences.locale,
      entries: appLogState.entries,
    });

    if (host.supportsNativeFileDialogs) {
      const result = await saveTextFileWithHost({
        fileName,
        content,
        mimeType: "text/plain;charset=utf-8",
      });

      if (result.cancelled) {
        return;
      }

      if (!result.success) {
        notify(result.message || copy.exportFailed, "warning");
        return;
      }
    } else {
      downloadBlobFile(fileName, content, "text/plain;charset=utf-8");
    }

    notify(copy.exportDone);
  } catch (error) {
    notify(error.message || copy.exportFailed, "error");
  } finally {
    busy.appLogExporting = false;
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

async function handleUpdateTrayAutoLockMinutes(minutes) {
  busy.platformSettings = true;

  try {
    const result = await setTrayAutoLockMinutes(minutes);
    await refreshHostState();

    if (!result.success) {
      notify(result.message || t("notify.trayAutoLockFailed"), "warning");
      return;
    }

    notify(result.message || t("notify.trayAutoLockUpdated"));
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

async function handleUpdateBackgroundAutoLockMinutes(minutes) {
  busy.platformSettings = true;

  try {
    const result = await setBackgroundAutoLockMinutes(minutes);
    await refreshHostState();

    if (!result.success) {
      notify(result.message || t("notify.backgroundAutoLockFailed"), "warning");
      return;
    }

    notify(result.message || t("notify.backgroundAutoLockUpdated"));
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

async function handleRefreshPasskeys() {
  const copy = getPasskeyUiCopy();
  busy.passkeyRefreshing = true;

  try {
    await refreshPasskeyBridgeState();

    if (!passkeyBridge.isSupported || !passkeyBridge.supportsMetadataManagement) {
      notify(passkeyBridge.message || copy.refreshFailed, "info");
      return;
    }

    const refreshResult = await refreshPasskeyMetadataWithHost();
    const hostItems = await listPasskeysWithHost();
    await passkeyVault.syncFromHostMetadata(hostItems);

    if (refreshResult.success) {
      notify(hostItems.length ? copy.refreshDone : copy.refreshEmpty);
      return;
    }

    notify(refreshResult.message || copy.refreshFailed, hostItems.length ? "warning" : "info");
  } catch (error) {
    notify(error.message || copy.refreshFailed, "error");
  } finally {
    busy.passkeyRefreshing = false;
  }
}

async function handleLaunchPasskeyCompanion() {
  const isZh = String(preferences.locale || "").toLowerCase().startsWith("zh");
  busy.passkeyLaunching = true;

  try {
    const result = await launchPasskeyCompanionWithHost();
    if (!result.success) {
      notify(result.message || (isZh ? "打开 Windows passkey companion 失败。" : "Unable to open the Windows passkey companion."), "warning");
      return;
    }

    notify(result.message || (isZh ? "Windows passkey companion 已打开。" : "The Windows passkey companion was opened."));

    await new Promise((resolve) => window.setTimeout(resolve, 650));
    await refreshPasskeyBridgeState();
  } finally {
    busy.passkeyLaunching = false;
  }
}

async function handleRestartPasskeyCompanion() {
  const isZh = String(preferences.locale || "").toLowerCase().startsWith("zh");
  busy.passkeyLaunching = true;

  try {
    const result = await restartPasskeyCompanionWithHost();
    await refreshPasskeyBridgeState();

    if (!result.success) {
      notify(
        result.message ||
          (isZh
            ? "重启 Windows passkey companion 失败。"
            : "Unable to restart the Windows passkey companion."),
        "warning"
      );
      return;
    }

    notify(result.message);
  } finally {
    busy.passkeyLaunching = false;
  }
}

async function handleTogglePasskeyCompanionAutoLaunch(enabled) {
  const isZh = String(preferences.locale || "").toLowerCase().startsWith("zh");
  busy.passkeyAutoLaunchSaving = true;

  try {
    const result = await setPasskeyCompanionAutoLaunchWithHost(enabled);
    await refreshPasskeyBridgeState();

    if (!result.success) {
      notify(
        result.message ||
          (isZh
            ? "更新 Windows passkey companion 自动启动设置失败。"
            : "Unable to update the Windows passkey companion auto-launch setting."),
        "warning"
      );
      return;
    }

    notify(result.message);
  } finally {
    busy.passkeyAutoLaunchSaving = false;
  }
}

async function handleResolvePasskeyOperation(resolution) {
  const isZh = String(preferences.locale || "").toLowerCase().startsWith("zh");
  const resolutionMessageMap = {
    approve: isZh
      ? "已在主应用里将当前 passkey 插件操作标记为通过。"
      : "The latest plugin operation was approved from the host UI.",
    reject: isZh
      ? "已在主应用里将当前 passkey 插件操作标记为拒绝。"
      : "The latest plugin operation was rejected from the host UI.",
    clear: isZh
      ? "已在主应用里清空当前 passkey 插件操作。"
      : "The latest plugin operation was cleared from the host UI.",
  };

  busy.passkeyOperationResolving = true;

  try {
    const result = await resolvePasskeyOperationWithHost({
      resolution,
      message: resolutionMessageMap[resolution] || "",
    });
    await refreshPasskeyBridgeState();

    if (!result.success) {
      notify(
        result.message ||
          (isZh
            ? "更新当前 passkey 插件操作状态失败。"
            : "Unable to update the current passkey plugin operation."),
        "warning"
      );
      return;
    }

    notify(result.message);
  } finally {
    busy.passkeyOperationResolving = false;
  }
}

async function handleRemovePasskeyMetadata(recordId) {
  const copy = getPasskeyUiCopy();
  itemLoadingState.passkeyBusyIds[recordId] = true;

  try {
    await passkeyVault.removeRecord(recordId);
    notify(copy.removeDone);
  } catch (error) {
    notify(error.message || copy.removeFailed, "error");
  } finally {
    delete itemLoadingState.passkeyBusyIds[recordId];
  }
}

async function handleRestorePasskeyMetadata(recordId) {
  const copy = getPasskeyUiCopy();
  itemLoadingState.passkeyBusyIds[recordId] = true;

  try {
    await passkeyVault.restoreRecord(recordId);
    notify(copy.restoreDone);
  } catch (error) {
    notify(error.message || copy.restoreFailed, "error");
  } finally {
    delete itemLoadingState.passkeyBusyIds[recordId];
  }
}

async function handlePermanentlyDeletePasskeyMetadata(recordId) {
  const copy = getPasskeyUiCopy();
  itemLoadingState.passkeyBusyIds[recordId] = true;

  try {
    const targetRecord =
      passkeyRecords.value.find((record) => record.id === recordId) ||
      deletedPasskeyRecords.value.find((record) => record.id === recordId) ||
      null;

    if (targetRecord?.nativeProviderRecordId) {
      const deleteResult = await deletePasskeyWithHost({
        nativeProviderRecordId: targetRecord.nativeProviderRecordId,
      });

      if (!deleteResult.success) {
        notify(deleteResult.message || copy.deleteFailed, "warning");
        return;
      }
    }

    await passkeyVault.permanentlyDeleteRecord(recordId);
    notify(copy.deleteDone);
  } catch (error) {
    notify(error.message || copy.deleteFailed, "error");
  } finally {
    delete itemLoadingState.passkeyBusyIds[recordId];
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

async function openIncrementalSyncReview(
  snapshotText,
  sourceLabel,
  targetKind = "none",
  targetDevice = null
) {
  const plan = await vault.buildIncrementalSyncPlan(snapshotText);
  if (
    !plan?.summary?.localOnlyCount &&
    !plan?.summary?.remoteOnlyCount &&
    !plan?.summary?.conflictCount
  ) {
    notify(t("notify.incrementalSyncNothingToMerge"), "info");
    return false;
  }

  syncState.incrementalSourceLabel = sourceLabel;
  syncState.incrementalPlan = plan;
  syncState.incrementalTargetKind = targetKind;
  syncState.incrementalTargetDevice = targetDevice;
  syncState.incrementalVisible = true;
  return true;
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

    const opened = await openIncrementalSyncReview(result.content, "WebDAV", "webdav");
    if (opened) {
      notify(t("notify.incrementalSyncReady"), "info");
    }
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

    const opened = await openIncrementalSyncReview(
      result.content,
      device.deviceName || t("common.sourceDevice"),
      "lan",
      device
    );
    if (opened) {
      notify(t("notify.incrementalSyncReady"), "info");
    }
  } catch (error) {
    notify(
      error.message || t("notify.incrementalSyncPrepareFailed") || t("notify.lanDownloadFailed"),
      "error"
    );
  } finally {
    busy.webDavTransferring = false;
  }
}

function handleToggleIncrementalItem({ groupId, selected }) {
  const group = syncState.incrementalPlan?.groups?.find((entry) => entry.id === groupId);
  if (!group) {
    return;
  }

  group.selected = Boolean(selected);
}

function handleResolveIncrementalConflict({ groupId, resolution }) {
  const group = syncState.incrementalPlan?.groups?.find((entry) => entry.id === groupId);
  if (!group || group.type !== "conflict") {
    return;
  }

  group.resolution = resolution === "remote" ? "remote" : "local";
}

async function handleConfirmSync() {
  if (!syncState.confirmSnapshotText) {
    return;
  }

  busy.lanConfirming = true;

  try {
    await vault.replaceWithEncryptedSnapshot(syncState.confirmSnapshotText);
    syncState.confirmVisible = false;

    if (host.isBiometricEnabled) {
      await disableBiometricUnlock();
      await refreshHostState();
    }

    unlockState.lastMethod = "";
    unlockState.biometricAttempted = false;
    securityState.secretKey = "";
    notify(t("notify.syncCompletedRelockedRecovery"), "warning");
  } catch (error) {
    notify(error.message || t("notify.syncFailed"), "error");
  } finally {
    busy.lanConfirming = false;
  }
}

async function handleConfirmIncrementalSync() {
  if (!syncState.incrementalPlan) {
    notify(t("notify.incrementalSyncNothingToMerge"), "info");
    return;
  }

  busy.lanConfirming = true;

  try {
    const applyResult = await vault.applyIncrementalSyncPlan(syncState.incrementalPlan);
    let pushFailureMessage = "";

    if (syncState.incrementalTargetKind === "lan" && syncState.incrementalTargetDevice) {
      const pushResult = await uploadLanMergedRecordsWithHost(
        syncState.incrementalTargetDevice,
        applyResult.records
      );

      if (!pushResult.success) {
        pushFailureMessage = pushResult.message || t("notify.incrementalSyncPushFailed");
      }
    }

    if (syncState.incrementalTargetKind === "webdav") {
      const mergedSnapshot = buildCurrentSyncSnapshot();
      const pushResult = await uploadSnapshotToWebDavWithHost(mergedSnapshot.text);
      if (!pushResult.success) {
        pushFailureMessage = pushResult.message || t("notify.webDavUploadFailed");
      }
    }

    syncState.incrementalVisible = false;
    syncState.incrementalPlan = null;
    syncState.incrementalSourceLabel = "";
    syncState.incrementalTargetKind = "none";
    syncState.incrementalTargetDevice = null;

    if (pushFailureMessage) {
      notify(pushFailureMessage, "warning");
    } else {
      notify(t("notify.incrementalSyncApplied"));
    }
  } catch (error) {
    notify(error.message || t("notify.incrementalSyncApplyFailed"), "error");
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

function handleHostLockRequested(event) {
  if (!vault.state.unlocked) {
    return;
  }

  handleLockVault();
  notify(event?.detail?.message || t("notify.autoLocked"), "info");
}

async function handleHostIncrementalSyncApply(event) {
  const recordsPayload = event?.detail?.records;
  if (!Array.isArray(recordsPayload)) {
    return;
  }

  try {
    await vault.applyExternalEncryptedRecords(recordsPayload);
    notify(
      t("notify.incrementalSyncReceived", {
        source: event?.detail?.sourceLabel || t("common.sourceDevice"),
      }),
      "success"
    );
  } catch (error) {
    notify(error.message || t("notify.incrementalSyncApplyFailed"), "error");
  }
}

function handleColorSchemeChange(event) {
  updateSystemThemePreference(event.matches);
}

watch(
  [currentView, listMode, listRecords],
  () => {
    if (
      currentView.value !== "list" ||
      listMode.value === "deleted" ||
      listMode.value === "passkeys"
    ) {
      resetSelection();
      return;
    }

    const visibleIds = new Set(listRecords.value.map((record) => record.id));
    selection.ids = selection.ids.filter((recordId) => visibleIds.has(recordId));
  },
  { deep: false }
);

watch(
  passkeyUiSupported,
  (supported) => {
    if (!supported && listMode.value === "passkeys") {
      listMode.value = "all";
    }
  },
  { immediate: true }
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

watch(
  [masterDialogVisible, biometricUnlockReady],
  ([visible, ready]) => {
    if (visible) {
      unlockScreenSettled.value = false;
    }

    if (
      !visible ||
      !ready ||
      busy.biometricUnlocking ||
      vault.state.unlocking ||
      unlockState.biometricAttempted
    ) {
      return;
    }

    unlockState.biometricAttempted = true;
    void handleBiometricUnlock();
  },
  { flush: "post" }
);

watch(
  () => vault.state.requiresSetup,
  (requiresSetup) => {
    if (requiresSetup && !settings.onboardingCompleted) {
      settings.onboardingInProgress = true;
      settings.onboardingStartedWithSetup = true;
      if (!["appearance", "setup"].includes(settings.onboardingStep)) {
        settings.onboardingStep = "appearance";
      }
      onboardingVisible.value = true;
    }
  },
  { immediate: true }
);

watch(
  () => vault.state.unlocked,
  (unlocked) => {
    if (!unlocked) {
      firstUseGuideVisible.value = false;
      return;
    }

    if (hasPendingFirstUseGuide() && !onboardingFlowVisible.value) {
      applyFirstUseGuideView(settings.firstUseGuideStep);
      firstUseGuideVisible.value = true;
    }
  },
  { immediate: true }
);

watch(
  shouldShowBottomNav,
  async (ready) => {
    if (pendingBottomNavHideTimer.value) {
      clearTimeout(pendingBottomNavHideTimer.value);
      pendingBottomNavHideTimer.value = null;
    }

    if (pendingBottomNavShowTimer.value) {
      clearTimeout(pendingBottomNavShowTimer.value);
      pendingBottomNavShowTimer.value = null;
    }

    if (!ready) {
      bottomNavVisible.value = false;
      if (shouldRenderBottomNav.value) {
        pendingBottomNavHideTimer.value = window.setTimeout(() => {
          if (!shouldShowBottomNav.value) {
            shouldRenderBottomNav.value = false;
          }
          pendingBottomNavHideTimer.value = null;
        }, 220);
      } else {
        shouldRenderBottomNav.value = false;
      }
      return;
    }

    shouldRenderBottomNav.value = true;
    bottomNavVisible.value = false;
    await nextTick();
    pendingBottomNavShowTimer.value = window.setTimeout(() => {
      window.requestAnimationFrame(() => {
        if (shouldShowBottomNav.value) {
          bottomNavVisible.value = true;
        }
        pendingBottomNavShowTimer.value = null;
      });
    }, 420);
  },
  { immediate: true, flush: "post" }
);

onMounted(async () => {
  await hydrateAppLogs();
  disposeAppLogCapture = installGlobalAppLogCapture(addAppLog);
  addAppLog({
    level: "info",
    source: "app",
    message: getAppLogCopy().sessionStarted,
  });

  window.addEventListener("resize", handleWindowResize, { passive: true });
  window.addEventListener("password-vault-host-lock", handleHostLockRequested);
  window.addEventListener("password-vault-host-sync-apply", handleHostIncrementalSyncApply);

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

  const appSettingsTask = loadAppSettings();
  const pendingImportReviewTask = loadPendingImportReviewItems();
  const hostStateTask = refreshHostState();
  const vaultBootstrapTask = vault.bootstrapVault().catch((error) => {
    notify(error.message || t("notify.vaultInitFailed"), "error");
  });

  await Promise.allSettled([
    appSettingsTask,
    pendingImportReviewTask,
    hostStateTask,
    vaultBootstrapTask,
  ]);

  unlockScreenSettled.value = !masterDialogVisible.value;
  if (!settings.onboardingCompleted) {
    let onboardingStateChanged = false;

    if (vault.state.requiresSetup) {
      onboardingVisible.value = true;

      if (!settings.onboardingInProgress) {
        settings.onboardingInProgress = true;
        onboardingStateChanged = true;
      }

      if (!settings.onboardingStartedWithSetup) {
        settings.onboardingStartedWithSetup = true;
        onboardingStateChanged = true;
      }

      const resumeSetupStep = ["appearance", "setup"].includes(settings.onboardingStep)
        ? settings.onboardingStep
        : "appearance";
      if (settings.onboardingStep !== resumeSetupStep) {
        settings.onboardingStep = resumeSetupStep;
        onboardingStateChanged = true;
      }
    } else {
      onboardingVisible.value = false;

      if (!settings.onboardingInProgress) {
        settings.onboardingInProgress = true;
        onboardingStateChanged = true;
      }

      if (!settings.onboardingStartedWithSetup) {
        settings.onboardingStartedWithSetup = true;
        onboardingStateChanged = true;
      }

      const normalizedStep = normalizeOnboardingStep(settings.onboardingStep, true);
      const resumeStep = normalizedStep === "setup" ? "security" : normalizedStep;
      if (settings.onboardingStep !== resumeStep) {
        settings.onboardingStep = resumeStep;
        onboardingStateChanged = true;
      }
    }

    if (onboardingStateChanged) {
      await persistOnboardingState();
    }
  }

  if (hasPendingFirstUseGuide() && vault.state.unlocked && !onboardingFlowVisible.value) {
    applyFirstUseGuideView(settings.firstUseGuideStep);
    firstUseGuideVisible.value = true;
  }

  void hydrateSyncSettingsInBackground();
  void hydratePasskeysInBackground();
});

onBeforeUnmount(() => {
  if (typeof disposeAppLogCapture === "function") {
    disposeAppLogCapture();
    disposeAppLogCapture = null;
  }

  window.removeEventListener("resize", handleWindowResize);
  window.removeEventListener("password-vault-host-lock", handleHostLockRequested);
  window.removeEventListener("password-vault-host-sync-apply", handleHostIncrementalSyncApply);

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

  if (pendingBottomNavHideTimer.value) {
    clearTimeout(pendingBottomNavHideTimer.value);
    pendingBottomNavHideTimer.value = null;
  }

  if (pendingBottomNavShowTimer.value) {
    clearTimeout(pendingBottomNavShowTimer.value);
    pendingBottomNavShowTimer.value = null;
  }
});
</script>

<template>
  <v-app>
    <v-main class="app-main bg-background">
      <div
        class="app-shell-wrap"
        :class="{ 'app-shell-wrap--blurred': masterDialogVisible || onboardingFlowVisible }"
      >
        <div class="app-shell px-3 px-sm-4 px-lg-6">
          <div
            v-if="vault.state.bootstrapping"
            class="d-flex flex-column align-center justify-center py-16"
          >
            <v-progress-circular indeterminate color="primary" size="42" />
            <div class="text-body-1 text-medium-emphasis mt-4">{{ t("app.bootstrapping") }}</div>
          </div>

          <template v-else>
            <Transition name="vault-page" mode="out-in">
              <section
                :key="`${currentView}-${listMode}-${preferences.themeMode}-${preferences.locale}`"
                class="vault-scene"
              >
                <AppTopBar
                  v-if="shouldShowSearchBar"
                  v-model="searchText"
                  :current-view="currentView"
                  :disabled="isLocked"
                  @create="openCreateDialog"
                  @lock="handleLockVault"
                />

                <div :class="shouldShowSearchBar ? 'mt-4' : 'mt-0'">
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
                  :items="listMode === 'deleted' || listMode === 'passkeys' ? [] : listRecords"
                  :deleted-items="searchedDeletedRecords"
                  :total-count="summary.total"
                  :favorite-count="favoriteRecords.length"
                  :passkey-count="passkeyRecords.length"
                  :search-text="searchText"
                  :list-mode="listMode"
                  :locale="preferences.locale"
                  :supports-passkeys="passkeyUiSupported"
                  :passkey-items="searchedPasskeyRecords"
                  :passkey-refreshing="busy.passkeyRefreshing"
                  :passkey-busy-ids="itemLoadingState.passkeyBusyIds"
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
                  @refresh-passkeys="handleRefreshPasskeys"
                  @remove-passkey="handleRemovePasskeyMetadata"
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
                  :deleted-batch-loading="busy.deletedBatchActing"
                  :pending-import-review-items="pendingImportedReviewItems"
                  :pending-import-review-updated-at="pendingImportedReviewUpdatedAt"
                  :pending-import-review-busy="busy.importReviewActing"
                  :app-logs="appLogState.entries"
                  :app-logs-updated-at="appLogState.updatedAt"
                  :app-log-exporting="busy.appLogExporting"
                  :native-file-dialogs-available="host.supportsNativeFileDialogs"
                  :busy="busy.importing || busy.exporting || isLocked"
                  :theme-mode="preferences.themeMode"
                  :resolved-theme="resolvedTheme"
                  :nav-alignment="preferences.navAlignment"
                  :locale="preferences.locale"
                  :changing-master-password="busy.changingMasterPassword"
                  :clearing-data="busy.clearingData"
                  :biometric-supported="host.isSupported"
                  :biometric-available="host.isBiometricAvailable"
                  :biometric-enabled="host.isBiometricEnabled"
                  :biometric-label="host.biometricLabel"
                  :biometric-message="host.message"
                  :biometric-loading="busy.biometricConfiguring"
                  :biometric-reauth-hours="settings.biometricReauthHours"
                  :secret-key-hint="secretKeyHint"
                  :secret-key-value="securityState.secretKey"
                  :secret-key-loading="securityState.secretKeyLoading"
                  :platform="host.platform"
                  :supports-minimize-to-tray="host.supportsMinimizeToTray"
                  :minimize-to-tray-enabled="host.minimizeToTrayEnabled"
                  :supports-launch-at-startup="host.supportsLaunchAtStartup"
                  :launch-at-startup-enabled="host.launchAtStartupEnabled"
                  :tray-auto-lock-minutes="host.trayAutoLockMinutes"
                  :supports-exclude-from-recents="host.supportsExcludeFromRecents"
                  :exclude-from-recents-enabled="host.excludeFromRecentsEnabled"
                  :background-auto-lock-minutes="host.backgroundAutoLockMinutes"
                  :supports-autostart-settings-shortcut="host.supportsAutostartSettingsShortcut"
                  :platform-settings-loading="busy.platformSettings"
                  :autostart-opening="busy.autostartOpening"
                  :supports-web-dav-sync="host.supportsWebDavSync"
                  :supports-lan-sync="host.supportsLanSync"
                  :supports-passkeys="host.supportsPasskeys || host.platform === 'windows'"
                  :passkey-state="passkeyBridge"
                  :passkey-items="passkeyRecords"
                  :deleted-passkey-items="deletedPasskeyRecords"
                  :passkey-refreshing="busy.passkeyRefreshing"
                  :passkey-launching="busy.passkeyLaunching"
                  :passkey-auto-launch-saving="busy.passkeyAutoLaunchSaving"
                  :passkey-operation-resolving="busy.passkeyOperationResolving"
                  :passkey-busy-ids="itemLoadingState.passkeyBusyIds"
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
                  @update-nav-alignment="handleUpdateNavAlignment"
                  @update-language="handleUpdateLanguage"
                  @change-master-password="changeMasterPasswordVisible = true"
                  @enable-biometric="handleEnableBiometricUnlock"
                  @disable-biometric="handleDisableBiometricUnlock"
                  @update-biometric-reauth-hours="handleUpdateBiometricReauthHours"
                  @reveal-secret-key="handleRevealSecretKey"
                  @copy-secret-key="handleCopySecretKey"
                  @toggle-minimize-to-tray="handleToggleMinimizeToTray"
                  @toggle-launch-at-startup="handleToggleLaunchAtStartup"
                  @update-tray-auto-lock-minutes="handleUpdateTrayAutoLockMinutes"
                  @toggle-exclude-from-recents="handleToggleExcludeFromRecents"
                  @update-background-auto-lock-minutes="handleUpdateBackgroundAutoLockMinutes"
                  @open-autostart-settings="handleOpenAutostartSettings"
                  @save-webdav-settings="handleSaveWebDavSettings"
                  @upload-webdav="handleUploadWebDav"
                  @download-webdav="handleDownloadWebDav"
                  @save-device-name="handleSaveDeviceName"
                  @scan-lan="handleScanLanDevices"
                  @sync-lan-device="handleSyncLanDevice"
                  @refresh-passkeys="handleRefreshPasskeys"
                  @launch-passkey-companion="handleLaunchPasskeyCompanion"
                  @restart-passkey-companion="handleRestartPasskeyCompanion"
                  @toggle-passkey-companion-auto-launch="handleTogglePasskeyCompanionAutoLaunch"
                  @resolve-passkey-operation="handleResolvePasskeyOperation"
                  @remove-passkey="handleRemovePasskeyMetadata"
                  @restore-passkey="handleRestorePasskeyMetadata"
                  @permanent-delete-passkey="handlePermanentlyDeletePasskeyMetadata"
                  @clear-all-data="clearAllDataVisible = true"
                  @restore="handleRestoreDeleted"
                  @permanent-delete="handlePermanentDelete"
                  @restore-many="handleRestoreDeletedMany"
                  @permanent-delete-many="handlePermanentDeleteMany"
                  @manual-add-pending-import="handleManualAddImportedItem"
                  @manual-add-many-pending-import="handleManualAddImportedItems"
                  @dismiss-pending-import="handleDismissImportedReviewItem"
                  @dismiss-many-pending-import="handleDismissImportedReviewItems"
                  @export-app-logs="handleExportAppLogs"
                />
                </div>
              </section>
            </Transition>
          </template>
        </div>

        <AppBottomNav
          v-if="shouldRenderBottomNav"
          v-model="currentView"
          :alignment="preferences.navAlignment"
          :visible="bottomNavVisible"
        />
      </div>
    </v-main>

    <MasterKeyDialog
      :model-value="shouldShowMasterUnlockScreen"
      :mode="masterDialogMode"
      :loading="vault.state.unlocking"
      :biometric-enabled="biometricUnlockReady"
      :biometric-label="host.biometricLabel"
      :biometric-loading="busy.biometricUnlocking"
      :requires-secret-key="vault.state.requiresSecretKeyForUnlock"
      :secret-key-hint="secretKeyHint"
      @submit="handleMasterSubmit"
      @biometric-unlock="handleBiometricUnlock"
      @after-close="handleUnlockScreenClosed"
    />

    <OnboardingDialog
      :model-value="onboardingFlowVisible"
      :start-with-setup="settings.onboardingStartedWithSetup"
      :step="settings.onboardingStep"
      :theme-mode="preferences.themeMode"
      :locale="preferences.locale"
      :setup-completed="vault.state.unlocked"
      :setup-loading="vault.state.unlocking"
      :platform="host.platform"
      :biometric-supported="host.isSupported"
      :biometric-available="host.isBiometricAvailable"
      :biometric-enabled="host.isBiometricEnabled"
      :biometric-label="host.biometricLabel"
      :biometric-loading="busy.biometricConfiguring"
      :biometric-reauth-hours="settings.biometricReauthHours"
      :secret-key-hint="secretKeyHint"
      :secret-key-value="securityState.secretKey"
      :secret-key-loading="securityState.secretKeyLoading"
      :supports-web-dav-sync="host.supportsWebDavSync"
      :supports-lan-sync="host.supportsLanSync"
      :sync-settings="syncSettings"
      :lan-devices="syncState.lanDevices"
      :lan-scanning="busy.lanScanning"
      :lan-saving-device-name="busy.lanSavingDeviceName"
      @update:model-value="onboardingVisible = $event"
      @update:step="handleUpdateOnboardingStep"
      @update-theme-mode="handleUpdateThemeMode"
      @update-language="handleUpdateLanguage"
      @complete="handleCompleteOnboarding"
      @submit-setup="handleOnboardingSetupSubmit"
      @enable-biometric="handleEnableBiometricUnlock"
      @disable-biometric="handleDisableBiometricUnlock"
      @update-biometric-reauth-hours="handleUpdateBiometricReauthHours"
      @reveal-secret-key="handleRevealSecretKey"
      @copy-secret-key="handleCopySecretKey"
      @save-device-name="handleSaveDeviceName"
      @scan-lan="handleScanLanDevices"
    />

    <ChangeMasterPasswordDialog
      v-model="changeMasterPasswordVisible"
      :loading="busy.changingMasterPassword"
      @submit="handleChangeMasterPassword"
    />

    <ClearAllDataDialog
      v-model="clearAllDataVisible"
      :loading="busy.clearingData"
      @submit="handleClearAllData"
    />

    <PasswordEditorDialog
      :model-value="editorVisible"
      :initial-draft="editorDraft"
      :loading="busy.saving"
      :password-injection="passwordInjection"
      @update:model-value="handleEditorVisibilityChange"
      @save="handleSaveDraft"
    />

    <DeleteConfirmDialog
      v-model="deleteDialogVisible"
      :title="pendingDeleteTitle"
      :count="deleteDialogCount"
      :loading="busy.deleting"
      @confirm="handleConfirmDelete"
    />

    <ImportReviewDialog
      v-model="importReviewVisible"
      :items="pendingImportedReviewItems"
      :busy="busy.importReviewActing"
      @manual-add="handleManualAddImportedItem"
      @manual-add-many="handleManualAddImportedItems"
      @dismiss="handleDismissImportedReviewItem"
      @dismiss-many="handleDismissImportedReviewItems"
    />

    <SyncConfirmDialog
      v-model="syncState.confirmVisible"
      :loading="busy.lanConfirming"
      :source-label="syncState.confirmSourceLabel"
      :local-preview="syncState.confirmLocalPreview"
      :remote-preview="syncState.confirmRemotePreview"
      @confirm="handleConfirmSync"
    />

    <IncrementalSyncDialog
      v-model="syncState.incrementalVisible"
      :loading="busy.lanConfirming"
      :source-label="syncState.incrementalSourceLabel"
      :local-preview="syncState.incrementalPlan?.localPreview || emptyPreview()"
      :remote-preview="syncState.incrementalPlan?.remotePreview || emptyPreview()"
      :local-only-items="syncState.incrementalPlan?.localOnly || []"
      :remote-only-items="syncState.incrementalPlan?.remoteOnly || []"
      :conflicts="syncState.incrementalPlan?.conflicts || []"
      @toggle-item="handleToggleIncrementalItem"
      @set-conflict="handleResolveIncrementalConflict"
      @confirm="handleConfirmIncrementalSync"
    />

    <FirstUseGuideOverlay
      :model-value="shouldShowFirstUseGuide"
      :step="settings.firstUseGuideStep"
      :locale="preferences.locale"
      @update:model-value="firstUseGuideVisible = $event"
      @update:step="handleUpdateFirstUseGuideStep"
      @complete="handleCompleteFirstUseGuide"
      @skip="handleSkipFirstUseGuide"
    />

    <AppSnackbar
      v-model="snackbar.show"
      :text="snackbar.text"
      :color="snackbar.color"
      :alignment="preferences.navAlignment"
    />
  </v-app>
</template>
