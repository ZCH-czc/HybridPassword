<script setup>
import { computed } from "vue";
import InlineSvgIcon from "@/components/InlineSvgIcon.vue";
import { useAppPreferences } from "@/composables/useAppPreferences";

const props = defineProps({
  locale: {
    type: String,
    default: "zh-CN",
  },
  platform: {
    type: String,
    default: "web",
  },
  supportsPluginManager: {
    type: Boolean,
    default: false,
  },
  requiresCompanionApp: {
    type: Boolean,
    default: true,
  },
  companionAppIntegrated: {
    type: Boolean,
    default: false,
  },
  canLaunchCompanionApp: {
    type: Boolean,
    default: false,
  },
  supportsCompanionAutoLaunch: {
    type: Boolean,
    default: false,
  },
  companionAutoLaunchEnabled: {
    type: Boolean,
    default: false,
  },
  launchingCompanion: {
    type: Boolean,
    default: false,
  },
  autoLaunchSaving: {
    type: Boolean,
    default: false,
  },
  operationResolving: {
    type: Boolean,
    default: false,
  },
  companionCheckedAtUnixTimeMs: {
    type: Number,
    default: 0,
  },
  companionBuildNumber: {
    type: Number,
    default: 0,
  },
  companionUbr: {
    type: Number,
    default: 0,
  },
  companionMeetsPluginBuildRequirement: {
    type: Boolean,
    default: false,
  },
  companionWebAuthnLibraryAvailable: {
    type: Boolean,
    default: false,
  },
  companionPluginExportsAvailable: {
    type: Boolean,
    default: false,
  },
  companionIsPackagedProcess: {
    type: Boolean,
    default: false,
  },
  companionStatusSummary: {
    type: String,
    default: "",
  },
  companionDetailMessage: {
    type: String,
    default: "",
  },
  companionWorkflowMode: {
    type: String,
    default: "native-registration",
  },
  companionRegistrationAttempted: {
    type: Boolean,
    default: false,
  },
  companionRegistrationPrepared: {
    type: Boolean,
    default: false,
  },
  companionRegistrationEnvironmentReady: {
    type: Boolean,
    default: false,
  },
  companionRegistrationCompleted: {
    type: Boolean,
    default: false,
  },
  companionLastRegistrationAttemptUnixTimeMs: {
    type: Number,
    default: 0,
  },
  companionRegistrationStatus: {
    type: String,
    default: "",
  },
  companionLastRegistrationMessage: {
    type: String,
    default: "",
  },
  companionLastRegistrationHResultHex: {
    type: String,
    default: "",
  },
  companionAuthenticatorStateCode: {
    type: Number,
    default: 0,
  },
  companionAuthenticatorStateLabel: {
    type: String,
    default: "unknown",
  },
  companionHasOperationSigningPublicKey: {
    type: Boolean,
    default: false,
  },
  companionOperationSigningPublicKeyStoredAtUnixTimeMs: {
    type: Number,
    default: 0,
  },
  companionComSkeletonReady: {
    type: Boolean,
    default: false,
  },
  companionComClassIdMatchesManifest: {
    type: Boolean,
    default: false,
  },
  companionComFactoryReady: {
    type: Boolean,
    default: false,
  },
  companionComAuthenticatorReady: {
    type: Boolean,
    default: false,
  },
  companionComLastProbeUnixTimeMs: {
    type: Number,
    default: 0,
  },
  companionComLastProbeMessage: {
    type: String,
    default: "",
  },
  companionComAuthenticatorTypeName: {
    type: String,
    default: "",
  },
  companionComClassFactoryRegistered: {
    type: Boolean,
    default: false,
  },
  companionComClassFactoryRegistrationCookie: {
    type: Number,
    default: 0,
  },
  companionComClassFactoryLastRegistrationUnixTimeMs: {
    type: Number,
    default: 0,
  },
  companionComClassFactoryLastMessage: {
    type: String,
    default: "",
  },
  companionComClassFactoryLastHResultHex: {
    type: String,
    default: "",
  },
  companionCallbackTotalCount: {
    type: Number,
    default: 0,
  },
  companionCallbackMakeCredentialCount: {
    type: Number,
    default: 0,
  },
  companionCallbackGetAssertionCount: {
    type: Number,
    default: 0,
  },
  companionCallbackCancelOperationCount: {
    type: Number,
    default: 0,
  },
  companionCallbackGetLockStatusCount: {
    type: Number,
    default: 0,
  },
  companionCallbackLastUnixTimeMs: {
    type: Number,
    default: 0,
  },
  companionCallbackLastKind: {
    type: String,
    default: "",
  },
  companionCallbackLastMessage: {
    type: String,
    default: "",
  },
  companionCallbackLastHResultHex: {
    type: String,
    default: "",
  },
  companionLatestOperationId: {
    type: String,
    default: "",
  },
  companionLatestOperationKind: {
    type: String,
    default: "",
  },
  companionLatestOperationState: {
    type: String,
    default: "idle",
  },
  companionLatestOperationSource: {
    type: String,
    default: "",
  },
  companionLatestOperationCreatedAtUnixTimeMs: {
    type: Number,
    default: 0,
  },
  companionLatestOperationUpdatedAtUnixTimeMs: {
    type: Number,
    default: 0,
  },
  companionLatestOperationRequestPointerPresent: {
    type: Boolean,
    default: false,
  },
  companionLatestOperationResponsePointerPresent: {
    type: Boolean,
    default: false,
  },
  companionLatestOperationCancelPointerPresent: {
    type: Boolean,
    default: false,
  },
  companionLatestOperationMessage: {
    type: String,
    default: "",
  },
  companionLatestOperationHResultHex: {
    type: String,
    default: "",
  },
  companionActivationCount: {
    type: Number,
    default: 0,
  },
  companionLastActivationUnixTimeMs: {
    type: Number,
    default: 0,
  },
  companionLastActivationSource: {
    type: String,
    default: "",
  },
  companionStartedFromPluginActivation: {
    type: Boolean,
    default: false,
  },
  companionCreateRequestCount: {
    type: Number,
    default: 0,
  },
  companionLastCreateRequestUnixTimeMs: {
    type: Number,
    default: 0,
  },
  companionLastCreateRequestRpId: {
    type: String,
    default: "",
  },
  companionLastCreateRequestUsername: {
    type: String,
    default: "",
  },
  companionLastCreateRequestMessage: {
    type: String,
    default: "",
  },
  recentLogs: {
    type: Array,
    default: () => [],
  },
});

const emit = defineEmits([
  "launch-companion",
  "restart-companion",
  "toggle-auto-launch",
  "approve-operation",
  "reject-operation",
  "clear-operation",
]);

const { formatDateTime } = useAppPreferences();

const isZh = computed(() => String(props.locale || "").toLowerCase().startsWith("zh"));

const copy = computed(() =>
  isZh.value
    ? {
        title: "应用状态",
        subtitle: "查看后台 Companion、插件注册进度，以及当前这台 Windows 是否已经准备好接管 passkey。",
        launchAction: "启动后台组件",
        restartAction: "重启后台组件",
        launchUnavailable:
          "当前还没有找到可用的后台 Companion 可执行文件。先构建 companion，或配置 PASSWORD_VAULT_PASSKEY_COMPANION_PATH。",
        companionTitle: "后台 Companion",
        companionBody:
          "主应用会优先探测后台 Companion；如果它还没运行，就会尝试在后台模式下启动它。",
        autoLaunchTitle: "随主应用自动启动",
        autoLaunchBody:
          "开启后，Windows 主应用启动时会自动尝试拉起后台 Companion，便于后续接管系统级 passkey 流程。",
        autoLaunchUnsupported: "当前环境暂不支持自动启动设置。",
        checkedAt: "最近检查",
        buildLabel: "Windows 构建",
        none: "无",
        logsTitle: "插件日志",
        logsEmpty: "当前还没有 passkey 插件日志。",
        levelInfo: "信息",
        levelWarning: "警告",
        levelError: "错误",
        repeated: "重复",
        buildReady: "系统版本满足要求",
        buildMissing: "系统版本未满足要求",
        webAuthnReady: "webauthn.dll 可用",
        webAuthnMissing: "webauthn.dll 不可用",
        exportsReady: "插件导出已就绪",
        exportsMissing: "插件导出缺失",
        packagedReady: "已打包进程",
        packagedMissing: "尚未打包",
        pluginReady: "Plugin API 已就绪",
        pluginPending: "Plugin API 仍在准备",
        companionReady: "后台组件已接入",
        companionMissing: "后台组件未接入",
        workflowTitle: "插件注册",
        workflowMode: "当前模式",
        workflowNative: "Windows 原生注册",
        workflowSkeleton: "开发骨架",
        registrationPrepared: "注册请求已准备",
        registrationPending: "注册仍在准备",
        registrationCompleted: "已注册",
        registrationNotCompleted: "尚未注册",
        registrationAttempted: "最近注册尝试",
        registrationHr: "上次 HRESULT",
        authenticatorState: "认证器状态",
        authEnabled: "已启用",
        authDisabled: "已注册，但未启用",
        authUnknown: "状态未知",
        opKeyReady: "已缓存签名公钥",
        opKeyMissing: "未获取签名公钥",
        opKeyStoredAt: "公钥缓存时间",
        createRequests: "已接收创建请求",
        lastCreateRequest: "最近创建请求",
        latestMessage: "最近消息",
      }
    : {
        title: "App status",
        subtitle:
          "Review the background companion, plugin-registration progress, and whether this Windows device is ready for passkey takeover.",
        launchAction: "Start background companion",
        restartAction: "Restart companion",
        launchUnavailable:
          "No background companion executable was found yet. Build the companion first or configure PASSWORD_VAULT_PASSKEY_COMPANION_PATH.",
        companionTitle: "Background companion",
        companionBody:
          "The host first probes the background companion. If it is not running, it will try to launch it in background mode.",
        autoLaunchTitle: "Launch with the main app",
        autoLaunchBody:
          "When enabled, the Windows host will automatically try to start the background companion when the app opens.",
        autoLaunchUnsupported: "Auto-launch control is not available in this environment.",
        checkedAt: "Last checked",
        buildLabel: "Windows build",
        none: "None",
        logsTitle: "Plugin log",
        logsEmpty: "No passkey plugin logs are available yet.",
        levelInfo: "Info",
        levelWarning: "Warning",
        levelError: "Error",
        repeated: "Repeated",
        buildReady: "OS build requirement satisfied",
        buildMissing: "OS build requirement not satisfied",
        webAuthnReady: "webauthn.dll available",
        webAuthnMissing: "webauthn.dll unavailable",
        exportsReady: "Plugin exports ready",
        exportsMissing: "Plugin exports missing",
        packagedReady: "Packaged process",
        packagedMissing: "Not packaged yet",
        pluginReady: "Plugin API ready",
        pluginPending: "Plugin API still pending",
        companionReady: "Background companion connected",
        companionMissing: "Background companion not connected",
        workflowTitle: "Plugin registration",
        workflowMode: "Current mode",
        workflowNative: "Windows native registration",
        workflowSkeleton: "Development skeleton",
        registrationPrepared: "Registration request prepared",
        registrationPending: "Registration still pending",
        registrationCompleted: "Registered",
        registrationNotCompleted: "Not registered yet",
        registrationAttempted: "Last registration attempt",
        registrationHr: "Last HRESULT",
        authenticatorState: "Authenticator state",
        authEnabled: "Enabled",
        authDisabled: "Registered but disabled",
        authUnknown: "Unknown",
        opKeyReady: "Operation-signing key cached",
        opKeyMissing: "Operation-signing key missing",
        opKeyStoredAt: "Key cached at",
        createRequests: "Captured create requests",
        lastCreateRequest: "Last create request",
        latestMessage: "Latest message",
      }
);

const activationCopy = computed(() =>
  isZh.value
    ? {
        title: "插件激活",
        count: "激活次数",
        lastActivation: "最近激活",
        source: "激活来源",
        sourceStartup: "启动参数激活",
        sourceForwarded: "转发到已运行组件",
        passive: "当前会话不是从插件激活路径启动的",
        active: "当前会话来自插件激活路径",
      }
    : {
        title: "Plugin activations",
        count: "Activation count",
        lastActivation: "Last activation",
        source: "Activation source",
        sourceStartup: "Startup activation argument",
        sourceForwarded: "Forwarded to running companion",
        passive: "This session was not started from plugin activation",
        active: "This session started from the plugin activation path",
      }
);

const comCopy = computed(() =>
  isZh.value
    ? {
        title: "COM 骨架",
        ready: "COM 骨架已就绪",
        pending: "COM 骨架仍在准备",
        classId: "CLSID 已对齐清单",
        classIdMissing: "CLSID 尚未对齐清单",
        factoryReady: "Factory 可创建",
        factoryMissing: "Factory 尚未就绪",
        authenticatorReady: "Authenticator 可实例化",
        authenticatorMissing: "Authenticator 尚未就绪",
        lastProbe: "最近探测",
        typeName: "类型名称",
      }
    : {
        title: "COM skeleton",
        ready: "COM skeleton is ready",
        pending: "COM skeleton is still pending",
        classId: "CLSID matches the manifest",
        classIdMissing: "CLSID does not match the manifest",
        factoryReady: "Factory can create instances",
        factoryMissing: "Factory is not ready",
        authenticatorReady: "Authenticator can be instantiated",
        authenticatorMissing: "Authenticator is not ready",
        localServerReady: "Local COM server registered",
        localServerPending: "Local COM server not registered",
        localServerCookie: "Registration cookie",
        localServerLast: "Last local-server registration",
        localServerHr: "Local-server HRESULT",
        lastProbe: "Last probe",
        typeName: "Type name",
      }
);

const comLocalServerReadyText = computed(() =>
  comCopy.value.localServerReady || (isZh.value ? "本地 COM 服务已注册" : "Local COM server registered")
);

const comLocalServerPendingText = computed(() =>
  comCopy.value.localServerPending || (isZh.value ? "本地 COM 服务尚未注册" : "Local COM server not registered")
);

const comLocalServerCookieText = computed(() =>
  comCopy.value.localServerCookie || (isZh.value ? "注册 Cookie" : "Registration cookie")
);

const comLocalServerLastText = computed(() =>
  comCopy.value.localServerLast || (isZh.value ? "最近本地服务注册" : "Last local-server registration")
);

const comLocalServerHrText = computed(() =>
  comCopy.value.localServerHr || (isZh.value ? "本地服务 HRESULT" : "Local-server HRESULT")
);

const callbackCopy = computed(() =>
  isZh.value
    ? {
        title: "插件回调",
        total: "回调总数",
        makeCredential: "MakeCredential",
        getAssertion: "GetAssertion",
        cancelOperation: "CancelOperation",
        getLockStatus: "GetLockStatus",
        lastCallback: "最近回调",
        lastKind: "最近类型",
        lastHr: "最近 HRESULT",
      }
    : {
        title: "Plugin callbacks",
        total: "Total callbacks",
        makeCredential: "MakeCredential",
        getAssertion: "GetAssertion",
        cancelOperation: "CancelOperation",
        getLockStatus: "GetLockStatus",
        lastCallback: "Last callback",
        lastKind: "Last callback kind",
        lastHr: "Last HRESULT",
      }
);

const operationCopy = computed(() =>
  isZh.value
    ? {
        title: "当前插件操作",
        operationId: "操作 ID",
        kind: "操作类型",
        state: "当前状态",
        source: "来源",
        createdAt: "首次捕获",
        updatedAt: "最近更新",
        requestPointer: "请求指针",
        responsePointer: "响应指针",
        cancelPointer: "取消指针",
        lastHr: "最近 HRESULT",
        approveAction: "标记通过",
        rejectAction: "标记拒绝",
        clearAction: "清空记录",
        stateIdle: "尚未捕获",
        stateCaptured: "已捕获",
        stateCapturedNotImplemented: "已捕获，但尚未实现",
        stateCapturedError: "已捕获，但返回错误",
        stateCancelRequested: "已收到取消请求",
        stateApproved: "已在骨架流程中标记通过",
        stateRejected: "已在骨架流程中标记拒绝",
        kindMakeCredential: "创建 Passkey",
        kindGetAssertion: "使用 Passkey 登录",
        kindCancel: "取消操作",
        sourceCom: "系统插件回调",
      }
    : {
        title: "Current plugin operation",
        operationId: "Operation ID",
        kind: "Kind",
        state: "State",
        source: "Source",
        createdAt: "Captured at",
        updatedAt: "Updated at",
        requestPointer: "Request pointer",
        responsePointer: "Response pointer",
        cancelPointer: "Cancel pointer",
        lastHr: "Latest HRESULT",
        approveAction: "Approve",
        rejectAction: "Reject",
        clearAction: "Clear",
        stateIdle: "No operation captured yet",
        stateCaptured: "Captured",
        stateCapturedNotImplemented: "Captured, not implemented yet",
        stateCapturedError: "Captured with error",
        stateCancelRequested: "Cancellation requested",
        stateApproved: "Approved in skeleton flow",
        stateRejected: "Rejected in skeleton flow",
        kindMakeCredential: "Create passkey",
        kindGetAssertion: "Sign in with passkey",
        kindCancel: "Cancel operation",
        sourceCom: "System plugin callback",
      }
);

const companionBuildLabel = computed(() => {
  if (!props.companionBuildNumber) {
    return copy.value.none;
  }

  return `${props.companionBuildNumber}.${props.companionUbr || 0}`;
});

const readinessTiles = computed(() => [
  {
    key: "build",
    label: props.companionMeetsPluginBuildRequirement
      ? copy.value.buildReady
      : copy.value.buildMissing,
    color: props.companionMeetsPluginBuildRequirement ? "success" : "warning",
    icon: "mdi-laptop",
  },
  {
    key: "webauthn",
    label: props.companionWebAuthnLibraryAvailable
      ? copy.value.webAuthnReady
      : copy.value.webAuthnMissing,
    color: props.companionWebAuthnLibraryAvailable ? "success" : "warning",
    icon: "mdi-shield-key-outline",
  },
  {
    key: "exports",
    label: props.companionPluginExportsAvailable
      ? copy.value.exportsReady
      : copy.value.exportsMissing,
    color: props.companionPluginExportsAvailable ? "success" : "warning",
    icon: "mdi-api",
  },
  {
    key: "packaged",
    label: props.companionIsPackagedProcess
      ? copy.value.packagedReady
      : copy.value.packagedMissing,
    color: props.companionIsPackagedProcess ? "success" : "secondary",
    icon: "mdi-package-variant-closed",
  },
  {
    key: "plugin",
    label: props.supportsPluginManager ? copy.value.pluginReady : copy.value.pluginPending,
    color: props.supportsPluginManager ? "success" : "warning",
    icon: "mdi-puzzle-outline",
  },
  {
    key: "companion",
    label: props.companionAppIntegrated ? copy.value.companionReady : copy.value.companionMissing,
    color: props.companionAppIntegrated ? "success" : "secondary",
    icon: "mdi-application-cog-outline",
  },
]);

const workflowModeLabel = computed(() => {
  switch (props.companionWorkflowMode) {
    case "native-registration":
      return copy.value.workflowNative;
    case "skeleton":
      return copy.value.workflowSkeleton;
    default:
      return props.companionWorkflowMode || copy.value.none;
  }
});

const authenticatorStateLabel = computed(() => {
  switch (props.companionAuthenticatorStateLabel) {
    case "enabled":
      return copy.value.authEnabled;
    case "disabled":
      return copy.value.authDisabled;
    default:
      return copy.value.authUnknown;
  }
});

const workflowSummaryTiles = computed(() => [
  {
    key: "mode",
    label: copy.value.workflowMode,
    value: workflowModeLabel.value,
  },
  {
    key: "registration",
    label: props.companionRegistrationCompleted
      ? copy.value.registrationCompleted
      : props.companionRegistrationPrepared
        ? copy.value.registrationPrepared
        : copy.value.registrationPending,
    value: props.companionRegistrationStatus || copy.value.registrationNotCompleted,
  },
  {
    key: "state",
    label: copy.value.authenticatorState,
    value: authenticatorStateLabel.value,
  },
  {
    key: "op-key",
    label: props.companionHasOperationSigningPublicKey
      ? copy.value.opKeyReady
      : copy.value.opKeyMissing,
    value:
      props.companionHasOperationSigningPublicKey &&
      props.companionOperationSigningPublicKeyStoredAtUnixTimeMs
        ? formatDateTime(props.companionOperationSigningPublicKeyStoredAtUnixTimeMs)
        : copy.value.none,
  },
  {
    key: "activation-count",
    label: activationCopy.value.count,
    value: String(props.companionActivationCount || 0),
  },
]);

const comSummaryTiles = computed(() => [
  {
    key: "com-ready",
    label: props.companionComSkeletonReady ? comCopy.value.ready : comCopy.value.pending,
    value: props.companionComLastProbeMessage || copy.value.none,
  },
  {
    key: "com-class-id",
    label: props.companionComClassIdMatchesManifest
      ? comCopy.value.classId
      : comCopy.value.classIdMissing,
    value: props.companionComAuthenticatorTypeName || copy.value.none,
  },
  {
    key: "com-factory",
    label: props.companionComFactoryReady ? comCopy.value.factoryReady : comCopy.value.factoryMissing,
    value: props.companionComAuthenticatorReady
      ? comCopy.value.authenticatorReady
      : comCopy.value.authenticatorMissing,
  },
  {
    key: "com-local-server",
    label: props.companionComClassFactoryRegistered
      ? comLocalServerReadyText.value
      : comLocalServerPendingText.value,
    value: props.companionComClassFactoryRegistered
      ? `${comLocalServerCookieText.value}: ${props.companionComClassFactoryRegistrationCookie || 0}`
      : props.companionComClassFactoryLastMessage || copy.value.none,
  },
]);

const callbackSummaryTiles = computed(() => [
  {
    key: "callback-total",
    label: callbackCopy.value.total,
    value: String(props.companionCallbackTotalCount || 0),
  },
  {
    key: "callback-make-credential",
    label: callbackCopy.value.makeCredential,
    value: String(props.companionCallbackMakeCredentialCount || 0),
  },
  {
    key: "callback-get-assertion",
    label: callbackCopy.value.getAssertion,
    value: String(props.companionCallbackGetAssertionCount || 0),
  },
  {
    key: "callback-cancel",
    label: callbackCopy.value.cancelOperation,
    value: String(props.companionCallbackCancelOperationCount || 0),
  },
  {
    key: "callback-lock-status",
    label: callbackCopy.value.getLockStatus,
    value: String(props.companionCallbackGetLockStatusCount || 0),
  },
]);

const createRequestSummary = computed(() => {
  if (!props.companionLastCreateRequestRpId && !props.companionLastCreateRequestMessage) {
    return copy.value.none;
  }

  const account = props.companionLastCreateRequestUsername || copy.value.none;
  return `${props.companionLastCreateRequestRpId || copy.value.none} / ${account}`;
});

const activationSourceLabel = computed(() => {
  switch (props.companionLastActivationSource) {
    case "startup-activation":
      return activationCopy.value.sourceStartup;
    case "ipc-forward":
      return activationCopy.value.sourceForwarded;
    default:
      return props.companionLastActivationSource || copy.value.none;
  }
});

const activationSessionLabel = computed(() =>
  props.companionStartedFromPluginActivation
    ? activationCopy.value.active
    : activationCopy.value.passive
);

const callbackKindLabel = computed(() => {
  switch (props.companionCallbackLastKind) {
    case "make-credential":
      return callbackCopy.value.makeCredential;
    case "get-assertion":
      return callbackCopy.value.getAssertion;
    case "cancel-operation":
      return callbackCopy.value.cancelOperation;
    case "get-lock-status":
      return callbackCopy.value.getLockStatus;
    default:
      return props.companionCallbackLastKind || copy.value.none;
  }
});

const latestOperationKindLabel = computed(() => {
  switch (props.companionLatestOperationKind) {
    case "make-credential":
      return operationCopy.value.kindMakeCredential;
    case "get-assertion":
      return operationCopy.value.kindGetAssertion;
    case "cancel-operation":
      return operationCopy.value.kindCancel;
    default:
      return props.companionLatestOperationKind || copy.value.none;
  }
});

const latestOperationStateLabel = computed(() => {
  switch (props.companionLatestOperationState) {
    case "idle":
      return operationCopy.value.stateIdle;
    case "captured":
      return operationCopy.value.stateCaptured;
    case "captured-not-implemented":
      return operationCopy.value.stateCapturedNotImplemented;
    case "captured-error":
      return operationCopy.value.stateCapturedError;
    case "cancel-requested":
      return operationCopy.value.stateCancelRequested;
    case "approved-skeleton":
      return operationCopy.value.stateApproved;
    case "rejected-skeleton":
      return operationCopy.value.stateRejected;
    default:
      return props.companionLatestOperationState || copy.value.none;
  }
});

const latestOperationSourceLabel = computed(() => {
  switch (props.companionLatestOperationSource) {
    case "com-callback":
      return operationCopy.value.sourceCom;
    default:
      return props.companionLatestOperationSource || copy.value.none;
  }
});

const latestOperationCanResolve = computed(() =>
  ["captured", "captured-not-implemented", "captured-error", "cancel-requested"].includes(
    props.companionLatestOperationState
  )
);

function getLogLevelColor(level) {
  if (level === "error") {
    return "error";
  }

  if (level === "warning") {
    return "warning";
  }

  return "secondary";
}

function getLogLevelLabel(level) {
  if (level === "error") {
    return copy.value.levelError;
  }

  if (level === "warning") {
    return copy.value.levelWarning;
  }

  return copy.value.levelInfo;
}
</script>

<template>
  <div class="d-flex flex-column ga-4">
    <v-card class="border-sm passkey-status-card">
      <v-card-text class="pa-5 pa-sm-6">
        <div class="d-flex flex-column flex-xl-row justify-space-between ga-4">
          <div class="d-flex align-start ga-4 min-w-0">
            <v-avatar size="56" class="passkey-status-hero__avatar">
              <InlineSvgIcon icon="mdi-chart-box-outline" :size="26" />
            </v-avatar>
            <div class="min-w-0">
              <div class="text-h5 font-weight-bold">{{ copy.title }}</div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                {{ copy.subtitle }}
              </div>
            </div>
          </div>

          <div class="d-flex align-center ga-2 flex-wrap justify-end">
            <v-btn
              variant="text"
              :disabled="platform !== 'windows' || (!canLaunchCompanionApp && !companionAppIntegrated)"
              :loading="launchingCompanion"
              @click="emit('launch-companion')"
            >
              <template #prepend>
                <InlineSvgIcon icon="mdi-open-in-new" :size="18" />
              </template>
              {{ copy.launchAction }}
            </v-btn>

            <v-btn
              v-if="platform === 'windows' && (canLaunchCompanionApp || companionAppIntegrated)"
              variant="text"
              :loading="launchingCompanion"
              @click="emit('restart-companion')"
            >
              <template #prepend>
                <InlineSvgIcon icon="mdi-refresh" :size="18" />
              </template>
              {{ copy.restartAction }}
            </v-btn>
          </div>
        </div>
      </v-card-text>
    </v-card>

    <v-card class="border-sm passkey-status-card">
      <v-card-text class="pa-5 pa-sm-6">
        <div class="d-flex flex-column flex-xl-row justify-space-between ga-4">
          <div class="flex-grow-1">
            <div class="text-h6 font-weight-bold">{{ copy.companionTitle }}</div>
            <div class="text-body-2 text-medium-emphasis mt-2">
              {{
                canLaunchCompanionApp || companionAppIntegrated
                  ? copy.companionBody
                  : copy.launchUnavailable
              }}
            </div>
          </div>

          <div class="passkey-status-meta">
            <div class="passkey-status-meta__row">
              <span class="text-medium-emphasis">{{ copy.buildLabel }}</span>
              <span>{{ companionBuildLabel }}</span>
            </div>
            <div class="passkey-status-meta__row">
              <span class="text-medium-emphasis">{{ copy.checkedAt }}</span>
              <span>
                {{
                  companionCheckedAtUnixTimeMs
                    ? formatDateTime(companionCheckedAtUnixTimeMs)
                    : copy.none
                }}
              </span>
            </div>
          </div>
        </div>

        <v-sheet class="mt-4 px-4 py-3 bg-surface-variant rounded-xl text-body-2">
          {{ companionStatusSummary || companionDetailMessage || copy.companionBody }}
        </v-sheet>

        <div
          v-if="companionDetailMessage && companionDetailMessage !== companionStatusSummary"
          class="text-body-2 text-medium-emphasis mt-3"
        >
          {{ companionDetailMessage }}
        </div>

        <div class="passkey-status-grid mt-4">
          <v-sheet
            v-for="item in readinessTiles"
            :key="item.key"
            class="pa-4 passkey-status-tile"
          >
            <div class="d-flex align-center ga-3">
              <v-avatar size="38" class="passkey-status-tile__avatar">
                <InlineSvgIcon :icon="item.icon" :size="18" />
              </v-avatar>
              <v-chip :color="item.color" variant="tonal">
                {{ item.label }}
              </v-chip>
            </div>
          </v-sheet>
        </div>
      </v-card-text>
    </v-card>

    <v-card class="border-sm passkey-status-card">
      <v-card-text class="pa-5 pa-sm-6">
        <div class="text-h6 font-weight-bold">{{ copy.workflowTitle }}</div>

        <div class="passkey-status-grid mt-4">
          <v-sheet
            v-for="tile in workflowSummaryTiles"
            :key="tile.key"
            class="pa-4 passkey-status-tile"
          >
            <div class="text-caption text-medium-emphasis">
              {{ tile.label }}
            </div>
            <div class="text-subtitle-1 font-weight-medium mt-2 text-wrap">
              {{ tile.value }}
            </div>
          </v-sheet>
        </div>

        <div class="text-subtitle-1 font-weight-medium mt-5">
          {{ comCopy.title }}
        </div>

        <div class="passkey-status-grid mt-3">
          <v-sheet
            v-for="tile in comSummaryTiles"
            :key="tile.key"
            class="pa-4 passkey-status-tile"
          >
            <div class="text-caption text-medium-emphasis">
              {{ tile.label }}
            </div>
            <div class="text-subtitle-1 font-weight-medium mt-2 text-wrap">
              {{ tile.value }}
            </div>
          </v-sheet>
        </div>

        <div class="d-flex flex-column ga-3 mt-4">
          <v-sheet class="pa-4 passkey-workflow-block">
            <div class="text-caption text-medium-emphasis">
              {{ copy.registrationAttempted }}
            </div>
            <div class="text-body-1 font-weight-medium mt-2">
              {{
                companionLastRegistrationAttemptUnixTimeMs
                  ? formatDateTime(companionLastRegistrationAttemptUnixTimeMs)
                  : copy.none
              }}
            </div>
            <div class="text-caption text-medium-emphasis mt-2">
              {{ copy.registrationHr }}:
              {{ companionLastRegistrationHResultHex || copy.none }}
            </div>
            <div class="text-body-2 text-medium-emphasis mt-2">
              {{
                companionLastRegistrationMessage ||
                companionRegistrationStatus ||
                copy.none
              }}
            </div>
          </v-sheet>

          <v-sheet class="pa-4 passkey-workflow-block">
            <div class="text-caption text-medium-emphasis">
              {{ activationCopy.title }}
            </div>
            <div class="text-body-1 font-weight-medium mt-2">
              {{ activationSessionLabel }}
            </div>
            <div class="text-caption text-medium-emphasis mt-2">
              {{ activationCopy.count }}: {{ companionActivationCount || 0 }}
            </div>
            <div class="text-caption text-medium-emphasis mt-2">
              {{ activationCopy.lastActivation }}:
              {{
                companionLastActivationUnixTimeMs
                  ? formatDateTime(companionLastActivationUnixTimeMs)
                  : copy.none
              }}
            </div>
            <div class="text-body-2 text-medium-emphasis mt-2">
              {{ activationCopy.source }}: {{ activationSourceLabel }}
            </div>
          </v-sheet>

          <v-sheet class="pa-4 passkey-workflow-block">
            <div class="text-caption text-medium-emphasis">
              {{ comCopy.lastProbe }}
            </div>
            <div class="text-body-1 font-weight-medium mt-2">
              {{
                companionComLastProbeUnixTimeMs
                  ? formatDateTime(companionComLastProbeUnixTimeMs)
                  : copy.none
              }}
            </div>
            <div class="text-caption text-medium-emphasis mt-2">
              {{ comCopy.typeName }}: {{ companionComAuthenticatorTypeName || copy.none }}
            </div>
            <div class="text-body-2 text-medium-emphasis mt-2">
              {{ companionComLastProbeMessage || copy.none }}
            </div>
          </v-sheet>

          <v-sheet class="pa-4 passkey-workflow-block">
            <div class="text-caption text-medium-emphasis">
              {{ comLocalServerLastText }}
            </div>
            <div class="text-body-1 font-weight-medium mt-2">
              {{
                companionComClassFactoryLastRegistrationUnixTimeMs
                  ? formatDateTime(companionComClassFactoryLastRegistrationUnixTimeMs)
                  : copy.none
              }}
            </div>
            <div class="text-caption text-medium-emphasis mt-2">
              {{ comLocalServerCookieText }}:
              {{ companionComClassFactoryRegistrationCookie || 0 }}
            </div>
            <div
              v-if="companionComClassFactoryLastHResultHex"
              class="text-caption text-medium-emphasis mt-2"
            >
              {{ comLocalServerHrText }}: {{ companionComClassFactoryLastHResultHex }}
            </div>
            <div class="text-body-2 text-medium-emphasis mt-2">
              {{ companionComClassFactoryLastMessage || copy.none }}
            </div>
          </v-sheet>

          <v-sheet class="pa-4 passkey-workflow-block">
            <div class="text-caption text-medium-emphasis">
              {{ callbackCopy.title }}
            </div>
            <div class="passkey-status-grid mt-3">
              <v-sheet
                v-for="tile in callbackSummaryTiles"
                :key="tile.key"
                class="pa-4 passkey-status-tile"
              >
                <div class="text-caption text-medium-emphasis">
                  {{ tile.label }}
                </div>
                <div class="text-subtitle-1 font-weight-medium mt-2 text-wrap">
                  {{ tile.value }}
                </div>
              </v-sheet>
            </div>
            <div class="text-caption text-medium-emphasis mt-4">
              {{ callbackCopy.lastCallback }}:
              {{
                companionCallbackLastUnixTimeMs
                  ? formatDateTime(companionCallbackLastUnixTimeMs)
                  : copy.none
              }}
            </div>
            <div class="text-caption text-medium-emphasis mt-2">
              {{ callbackCopy.lastKind }}: {{ callbackKindLabel }}
            </div>
            <div
              v-if="companionCallbackLastHResultHex"
              class="text-caption text-medium-emphasis mt-2"
            >
              {{ callbackCopy.lastHr }}: {{ companionCallbackLastHResultHex }}
            </div>
            <div class="text-body-2 text-medium-emphasis mt-2">
              {{ companionCallbackLastMessage || copy.none }}
            </div>
          </v-sheet>

          <v-sheet class="pa-4 passkey-workflow-block">
            <div class="text-caption text-medium-emphasis">
              {{ operationCopy.title }}
            </div>
            <div class="passkey-status-grid mt-3">
              <v-sheet class="pa-4 passkey-status-tile">
                <div class="text-caption text-medium-emphasis">
                  {{ operationCopy.kind }}
                </div>
                <div class="text-subtitle-1 font-weight-medium mt-2 text-wrap">
                  {{ latestOperationKindLabel }}
                </div>
              </v-sheet>
              <v-sheet class="pa-4 passkey-status-tile">
                <div class="text-caption text-medium-emphasis">
                  {{ operationCopy.state }}
                </div>
                <div class="text-subtitle-1 font-weight-medium mt-2 text-wrap">
                  {{ latestOperationStateLabel }}
                </div>
              </v-sheet>
              <v-sheet class="pa-4 passkey-status-tile">
                <div class="text-caption text-medium-emphasis">
                  {{ operationCopy.source }}
                </div>
                <div class="text-subtitle-1 font-weight-medium mt-2 text-wrap">
                  {{ latestOperationSourceLabel }}
                </div>
              </v-sheet>
              <v-sheet class="pa-4 passkey-status-tile">
                <div class="text-caption text-medium-emphasis">
                  {{ operationCopy.operationId }}
                </div>
                <div class="text-subtitle-1 font-weight-medium mt-2 text-wrap passkey-operation-id">
                  {{ companionLatestOperationId || copy.none }}
                </div>
              </v-sheet>
            </div>
            <div class="text-caption text-medium-emphasis mt-4">
              {{ operationCopy.createdAt }}:
              {{
                companionLatestOperationCreatedAtUnixTimeMs
                  ? formatDateTime(companionLatestOperationCreatedAtUnixTimeMs)
                  : copy.none
              }}
            </div>
            <div class="text-caption text-medium-emphasis mt-2">
              {{ operationCopy.updatedAt }}:
              {{
                companionLatestOperationUpdatedAtUnixTimeMs
                  ? formatDateTime(companionLatestOperationUpdatedAtUnixTimeMs)
                  : copy.none
              }}
            </div>
            <div class="text-caption text-medium-emphasis mt-2">
              {{ operationCopy.requestPointer }}:
              {{ companionLatestOperationRequestPointerPresent ? "true" : "false" }}
            </div>
            <div class="text-caption text-medium-emphasis mt-2">
              {{ operationCopy.responsePointer }}:
              {{ companionLatestOperationResponsePointerPresent ? "true" : "false" }}
            </div>
            <div class="text-caption text-medium-emphasis mt-2">
              {{ operationCopy.cancelPointer }}:
              {{ companionLatestOperationCancelPointerPresent ? "true" : "false" }}
            </div>
            <div
              v-if="companionLatestOperationHResultHex"
              class="text-caption text-medium-emphasis mt-2"
            >
              {{ operationCopy.lastHr }}: {{ companionLatestOperationHResultHex }}
            </div>
            <div class="text-body-2 text-medium-emphasis mt-2">
              {{ companionLatestOperationMessage || copy.none }}
            </div>
            <div class="d-flex flex-wrap ga-2 mt-4">
              <v-btn
                color="primary"
                variant="tonal"
                :disabled="!latestOperationCanResolve"
                :loading="operationResolving"
                @click="emit('approve-operation')"
              >
                {{ operationCopy.approveAction }}
              </v-btn>
              <v-btn
                color="error"
                variant="tonal"
                :disabled="!latestOperationCanResolve"
                :loading="operationResolving"
                @click="emit('reject-operation')"
              >
                {{ operationCopy.rejectAction }}
              </v-btn>
              <v-btn
                variant="text"
                :disabled="companionLatestOperationState === 'idle'"
                :loading="operationResolving"
                @click="emit('clear-operation')"
              >
                {{ operationCopy.clearAction }}
              </v-btn>
            </div>
          </v-sheet>

          <v-sheet class="pa-4 passkey-workflow-block">
            <div class="text-caption text-medium-emphasis">
              {{ copy.lastCreateRequest }}
            </div>
            <div class="text-body-1 font-weight-medium mt-2">
              {{ createRequestSummary }}
            </div>
            <div class="text-caption text-medium-emphasis mt-2">
              {{
                companionLastCreateRequestUnixTimeMs
                  ? formatDateTime(companionLastCreateRequestUnixTimeMs)
                  : copy.none
              }}
            </div>
            <div class="text-body-2 text-medium-emphasis mt-2">
              {{ companionLastCreateRequestMessage || copy.none }}
            </div>
          </v-sheet>
        </div>
      </v-card-text>
    </v-card>

    <v-card class="border-sm passkey-status-card">
      <v-card-text class="pa-5 pa-sm-6">
        <div class="text-h6 font-weight-bold">{{ copy.autoLaunchTitle }}</div>

        <v-sheet
          v-if="supportsCompanionAutoLaunch"
          class="mt-4 pa-4 settings-block passkey-auto-launch-block"
        >
          <div class="d-flex align-center justify-space-between ga-4">
            <div class="flex-grow-1">
              <div class="text-body-1 font-weight-medium">
                {{ copy.autoLaunchTitle }}
              </div>
              <div class="text-body-2 text-medium-emphasis mt-2">
                {{ copy.autoLaunchBody }}
              </div>
            </div>

            <v-switch
              :model-value="companionAutoLaunchEnabled"
              color="primary"
              hide-details
              inset
              :loading="autoLaunchSaving"
              :disabled="platform !== 'windows'"
              @update:model-value="emit('toggle-auto-launch', $event)"
            />
          </div>
        </v-sheet>

        <div v-else class="text-body-2 text-medium-emphasis mt-4">
          {{ copy.autoLaunchUnsupported }}
        </div>
      </v-card-text>
    </v-card>

    <v-card class="border-sm passkey-status-card">
      <v-card-text class="pa-5 pa-sm-6">
        <div class="text-h6 font-weight-bold">{{ copy.logsTitle }}</div>

        <div v-if="!recentLogs.length" class="text-body-2 text-medium-emphasis mt-4">
          {{ copy.logsEmpty }}
        </div>

        <div v-else class="d-flex flex-column ga-3 mt-4">
          <div
            v-for="(entry, index) in recentLogs"
            :key="`${entry.timestampUnixTimeMs}-${index}`"
            class="passkey-log-item"
          >
            <div class="d-flex align-center justify-space-between ga-3 flex-wrap">
              <div class="d-flex align-center ga-2 flex-wrap">
                <v-chip size="small" variant="tonal" :color="getLogLevelColor(entry.level)">
                  {{ getLogLevelLabel(entry.level) }}
                </v-chip>
                <span class="text-caption text-medium-emphasis">{{ entry.source || "passkey" }}</span>
                <span
                  v-if="Number(entry.repeatCount || 1) > 1"
                  class="text-caption text-medium-emphasis"
                >
                  {{ copy.repeated }} x{{ entry.repeatCount }}
                </span>
              </div>

              <span class="text-caption text-medium-emphasis">
                {{
                  entry.timestampUnixTimeMs
                    ? formatDateTime(entry.timestampUnixTimeMs)
                    : copy.none
                }}
              </span>
            </div>

            <div class="text-body-2 mt-2">
              {{ entry.message }}
            </div>
          </div>
        </div>
      </v-card-text>
    </v-card>
  </div>
</template>

<style scoped>
.passkey-status-card {
  background: var(--vault-panel-bg);
}

.passkey-status-hero__avatar {
  background:
    radial-gradient(circle at 30% 30%, rgba(var(--v-theme-secondary), 0.2), transparent 58%),
    rgba(var(--v-theme-on-surface), 0.05);
  color: rgba(var(--v-theme-on-surface), 0.84);
}

.passkey-status-meta {
  display: flex;
  min-width: 220px;
  flex-direction: column;
  gap: 10px;
}

.passkey-status-meta__row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  font-size: 0.92rem;
}

.passkey-status-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.passkey-status-tile,
.passkey-workflow-block {
  background: var(--vault-block-bg);
  border-radius: calc(var(--vault-radius) + 2px);
}

.passkey-status-tile__avatar {
  background: rgba(var(--v-theme-on-surface), 0.05);
  color: rgba(var(--v-theme-on-surface), 0.72);
}

.passkey-auto-launch-block {
  background: var(--vault-block-bg-subtle);
}

.passkey-log-item {
  padding: 14px 16px;
  border-radius: calc(var(--vault-radius) - 2px);
  background: var(--vault-block-bg-subtle);
}

.passkey-operation-id {
  word-break: break-all;
}

@media (max-width: 760px) {
  .passkey-status-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 640px) {
  .passkey-status-meta {
    min-width: 0;
  }

  .passkey-status-meta__row {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
