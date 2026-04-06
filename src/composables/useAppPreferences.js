import { computed, reactive } from "vue";

const SUPPORTED_LOCALES = ["zh-CN", "en-US"];
const SUPPORTED_THEME_MODES = ["system", "light", "dark"];
const SUPPORTED_NAV_ALIGNMENTS = ["center", "left", "right"];

const state = reactive({
  locale: "zh-CN",
  themeMode: "system",
  navAlignment: "center",
  systemPrefersDark: false,
});

const messages = {
  "zh-CN": {},
  "en-US": {},
};

Object.assign(messages["zh-CN"], {
  "app.title": "密码库",
  "app.bootstrapping": "正在初始化密码库...",
  "common.none": "暂无",
  "common.save": "保存",
  "common.cancel": "取消",
  "common.close": "关闭",
  "common.confirm": "确认",
  "common.done": "完成",
  "common.edit": "编辑",
  "common.delete": "删除",
  "common.restore": "恢复",
  "common.refresh": "刷新",
  "common.copy": "复制",
  "common.lock": "立即锁定",
  "common.unlock": "解锁",
  "common.create": "新建",
  "common.next": "下一步",
  "common.skip": "跳过",
  "common.start": "开始使用",
  "common.saved": "已保存",
  "common.search": "搜索",
  "common.sync": "同步",
  "common.upload": "上传",
  "common.download": "下载",
  "common.preview": "预览",
  "common.settings": "设置",
  "common.home": "主页",
  "common.list": "列表",
  "common.notes": "备注",
  "common.password": "密码",
  "common.username": "用户名",
  "common.enabled": "已启用",
  "common.disabled": "未启用",
  "common.currentDevice": "当前设备",
  "common.sourceDevice": "来源设备",
  "common.unnamedEntry": "未命名项目",
  "common.unnamedDevice": "未命名设备",
  "common.requiredField": "该字段不能为空",
  "common.atLeastChars": ({ count }) => `至少需要 ${count} 位`,
  "common.countItems": ({ count }) => `${count}条`,
  "common.countNotes": ({ count }) => `${count}条备注`,
  "common.countEntries": ({ count }) => `${count}条记录`,
  "nav.home": "主页",
  "nav.list": "列表",
  "nav.settings": "设置",
  "search.homeLabel": "搜索密码、用户名或备注",
  "search.listLabel": "搜索全部项目或最近删除",
  "home.overline": "主页",
  "home.title": "密码库",
  "home.subtitle": "快速查看收藏夹和最近使用的项目。",
  "home.savedCount": ({ count }) => `已保存${count}条`,
  "home.filteredCount": ({ count }) => `当前结果${count}条`,
  "home.notesCount": ({ count }) => `备注${count}条`,
  "home.viewAll": "查看全部",
  "home.securityStatus": "安全状态",
  "home.unlockedTitle": "当前会话已解锁",
  "home.unlockedBody": "锁定后会清空本次会话中的解密内容。",
  "home.masterProtected": "主密码保护",
  "home.masterProtectedBody": "密码仅在解锁后按需解密显示",
  "home.favorites": "收藏夹",
  "home.noFavorites": "暂无收藏项目",
  "home.noFavoritesBody": "在列表中点亮星标后，这里会优先显示。",
  "home.sectionRecent": "最近项目",
  "home.sectionSearchPreview": "搜索结果预览",
  "home.openList": "查看列表",
  "home.noItems": "暂无项目",
  "home.noItemsBody": "新建一条密码记录后，这里会显示最近使用的项目。",
  "home.passwordPreview": "密码预览",
  "home.copyPassword": "复制密码",
  "home.copyUsername": "复制用户名",
  "list.title": "全部项目",
  "list.tabAll": "全部",
  "list.tabFavorites": "收藏夹",
  "list.tabDeleted": "最近删除",
  "list.statusDeleted": ({ count }) => `最近删除 ${count}条`,
  "list.statusSearch": ({ count }) => `搜索结果 ${count}条`,
  "list.statusAll": ({ total, favorite }) => `共 ${total}条，收藏 ${favorite}条`,
  "list.savedPasswords": "已保存的密码",
  "list.selectedCount": ({ count }) => `已选${count}条`,
  "list.select": "选择",
  "list.selectAll": "全选",
  "list.unselectAll": "取消全选",
  "list.bulkFavorite": "批量收藏",
  "list.bulkUnfavorite": "取消收藏",
  "list.bulkDelete": "批量删除",
  "list.complete": "完成",
  "list.noResults": "没有匹配的密码记录",
  "list.noResultsBody": "你可以新建密码，或调整顶部搜索条件。",
  "item.favorite": "收藏",
  "item.updatedAt": ({ time }) => `更新于 ${time}`,
  "item.password": "密码",
  "item.showPlain": "显示明文",
  "item.hidePlain": "隐藏明文",
  "item.copyPassword": "复制密码",
  "item.copyUsername": "复制用户名",
  "item.noNotes": "暂无备注",
  "item.expandNotes": "展开备注",
  "item.collapseNotes": "收起备注",
});

Object.assign(messages["zh-CN"], {
  "deleted.title": "最近删除",
  "deleted.emptyTitle": "最近删除0条",
  "deleted.emptyBody": "从列表中删除的项目会先进入这里，你可以恢复或彻底删除。",
  "deleted.deletedAt": ({ time }) => `删除于 ${time}`,
  "deleted.restore": "恢复",
  "deleted.permanentDelete": "彻底删除",
  "generator.title": "随机密码生成器",
  "generator.length": "密码长度",
  "generator.uppercase": "大写字母 A-Z",
  "generator.lowercase": "小写字母 a-z",
  "generator.numbers": "数字 0-9",
  "generator.symbols": "特殊字符",
  "generator.preview": "实时预览",
  "generator.emptyPreview": "请至少选择一种字符类型",
  "generator.copyPreview": "复制预览",
  "generator.fillForm": "填入表单",
  "generator.error": "无法生成密码。",
  "editor.createTitle": "新建密码",
  "editor.editTitle": "编辑密码",
  "editor.siteName": "网站或应用（选填）",
  "editor.username": "用户名",
  "editor.password": "密码",
  "editor.notes": "备注",
  "editor.addNote": "新增备注",
  "editor.noteLabel": ({ index }) => `备注 ${index}`,
  "deleteDialog.title": "移入最近删除",
  "deleteDialog.message": ({ target }) =>
    `你即将把 ${target} 移入最近删除，之后仍然可以在列表或设置中的最近删除里恢复。`,
  "deleteDialog.singleTarget": "该密码记录",
  "deleteDialog.multiTarget": ({ count }) => `${count}条密码记录`,
  "deleteDialog.confirm": "移入最近删除",
  "master.setupTitle": "初始化主密码",
  "master.unlockTitle": "解锁密码库",
  "master.setupBody": "主密码会保护当前设备上的密码库，请务必牢记。",
  "master.unlockBody": "输入主密码后，应用会在本地临时解锁你的密码库。",
  "master.passphrase": "主密码",
  "master.confirmPassphrase": "确认主密码",
  "master.createAndUnlock": "创建并解锁",
  "master.useBiometric": ({ label }) => `使用${label}解锁`,
  "master.minLength": "主密码至少需要 8 位",
  "master.confirmMismatch": "两次输入的主密码不一致",
  "changeMaster.title": "修改主密码",
  "changeMaster.description": "新主密码至少 8 位，更新后会立即重新保护现有数据。",
  "changeMaster.current": "当前主密码",
  "changeMaster.next": "新主密码",
  "changeMaster.confirm": "确认新主密码",
  "changeMaster.submit": "更新主密码",
  "changeMaster.mismatch": "两次输入的新主密码不一致",
  "onboarding.title": "新手指引",
  "onboarding.slide1.title": "欢迎来到你的密码库",
  "onboarding.slide1.body": "主页会展示概览、收藏夹和最近项目，方便你快速进入常用账号。",
  "onboarding.slide2.title": "列表页管理全部项目",
  "onboarding.slide2.body": "你可以在列表页查看全部、收藏夹和最近删除，也可以恢复误删记录。",
  "onboarding.slide3.title": "设置页集中管理偏好与同步",
  "onboarding.slide3.body": "外观、安全、导入导出、WebDAV 与局域网同步都集中在这里。",
  "settings.headerDescription": ({ count, deletedCount }) =>
    `已保存${count}条，最近删除${deletedCount}条`,
  "settings.appearance": "外观",
  "settings.security": "安全",
  "settings.language": "语言",
  "settings.language.zhCn": "简体中文",
  "settings.language.enUs": "English",
  "settings.appearance.themeMode": "主题模式",
  "settings.appearance.themeModeHint": ({ theme }) => `当前显示为${theme}`,
  "settings.appearance.theme.system": "跟随系统",
  "settings.appearance.theme.light": "浅色",
  "settings.appearance.theme.dark": "暗黑",
  "settings.appearance.resolved.light": "浅色模式",
  "settings.appearance.resolved.dark": "暗黑模式",
  "settings.changeMaster": "修改主密码",
  "settings.changeMasterBody": "修改后会用新的主密码重新保护现有数据。",
  "settings.biometricUnlock": "生物识别解锁",
  "settings.biometricUnavailable": ({ label }) => `当前设备暂时无法使用${label}。`,
  "settings.biometricNotIntegrated": "当前宿主尚未接入生物识别。",
  "settings.biometricEnabledBody": ({ label }) => `已启用${label}，下次可以直接验证解锁。`,
  "settings.biometricDisabledBody": ({ label }) => `启用后可直接使用${label}解锁。`,
  "settings.enableBiometric": "启用生物识别",
  "settings.disableBiometric": "关闭生物识别",
  "settings.windowsTray": "关闭窗口时收纳到托盘",
  "settings.windowsTrayBody": "仅在 Windows 上生效，关闭窗口时应用会隐藏到系统托盘。",
  "settings.launchAtStartup": "开机自启动",
  "settings.launchAtStartupBody": "登录 Windows 后自动启动应用。",
  "settings.excludeFromRecents": "不在最近任务中显示",
  "settings.excludeFromRecentsBody": "开启后，Android 最近任务卡片中会隐藏当前应用。",
  "settings.autostartShortcut": "自启动与后台运行",
  "settings.autostartShortcutBody":
    "Android 不同厂商的自启动入口不统一，这里会优先打开系统相关设置页。",
  "settings.openSystemSettings": "打开系统设置",
  "settings.importExport": "导入与导出",
  "settings.importStrategy": "CSV 冲突策略",
  "settings.importOverwrite": "按用户名覆盖已有记录",
  "settings.importSkip": "按用户名跳过重复记录",
  "settings.importExportHint": "多条备注会使用 | 连接保存。",
  "settings.importExportNativeHint": "当前会调用系统文件管理器。",
  "settings.importExportBrowserHint": "浏览器调试环境会使用网页文件选择与下载。",
  "settings.exportCsv": "导出 CSV",
  "settings.exportTxt": "导出 TXT",
  "settings.importCsv": "导入 CSV",
  "settings.webDav": "WebDAV 同步",
  "settings.encryptedSnapshot": "加密快照",
  "settings.webDavUrl": "WebDAV 地址",
  "settings.webDavPath": "远端文件路径",
  "settings.webDavUsername": "用户名",
  "settings.webDavPassword": "密码",
  "settings.webDavHint": "WebDAV 中保存的是加密快照，不会上传明文密码。",
  "settings.webDavPasswordSaved": "当前已保存 WebDAV 密码，留空则保持不变。",
  "settings.webDavPasswordEmpty": "如果服务端需要认证，请填写用户名和密码。",
  "settings.saveConfig": "保存配置",
  "settings.uploadCurrentData": "上传当前数据",
  "settings.downloadFromWebDav": "从 WebDAV 拉取",
  "settings.lanSync": "局域网同步",
  "settings.scanDevices": "扫描设备",
  "settings.deviceIdentity": "本机标识",
  "settings.deviceIdentityBody": "其他设备会看到这个名称，并用它来区分同步来源。",
  "settings.deviceName": "设备名称",
  "settings.saveName": "保存名称",
  "settings.lanHint": "同步前会展示“本机”和“目标设备”的最新新增项目，用来帮你确认哪一侧数据更新。",
  "settings.noLanDevices": "还没有扫描到其他可用设备",
  "settings.syncAvailable": "可同步",
  "settings.syncUnavailable": "暂无数据",
  "settings.latestAdded": "最新添加",
  "settings.lastPublished": ({ time }) => `最近发布 ${time}`,
  "settings.useDeviceData": "使用这台设备的数据",
  "syncConfirm.title": "确认同步",
  "syncConfirm.description": ({ source }) => `你将使用 ${source} 的数据覆盖当前设备。`,
  "syncConfirm.warning":
    "为了避免误同步，下面会同时显示两边最近新增的项目。确认后，本机会替换为来源设备的整库加密快照。",
  "syncConfirm.addedAt": ({ time }) => `添加时间 ${time}`,
  "notify.unlockFailed": "解锁失败。",
  "notify.biometricUnavailable": ({ label }) => `无法使用${label}解锁。`,
  "notify.biometricStoredPasswordExpired":
    "设备验证已通过，但宿主中保存的主密码已经失效，请手动输入主密码后重新启用生物识别。",
  "notify.readDraftFailed": "读取待编辑数据失败。",
  "notify.recordCreated": "密码记录已创建。",
  "notify.recordUpdated": "密码记录已更新。",
  "notify.saveFailed": "保存失败。",
  "notify.decryptFailed": "密码解密失败。",
  "notify.favoriteFailed": "更新收藏状态失败。",
  "notify.passwordCopied": "密码已复制到剪贴板。",
  "notify.usernameCopied": "用户名已复制到剪贴板。",
  "notify.copyPasswordFailed": "复制密码失败，请检查剪贴板权限。",
  "notify.copyUsernameFailed": "复制用户名失败，请检查剪贴板权限。",
  "notify.deleted": "密码记录已移入最近删除。",
  "notify.deletedMany": ({ count }) => `${count}条密码记录已移入最近删除。`,
  "notify.deleteFailed": "删除失败。",
  "notify.restored": "密码记录已恢复。",
  "notify.restoreFailed": "恢复失败。",
  "notify.permanentlyDeleted": "密码记录已彻底删除。",
  "notify.permanentDeleteFailed": "彻底删除失败。",
  "notify.exportSaved": ({ format }) => `${format} 已保存。`,
  "notify.exportSuccess": ({ format }) => `${format} 导出成功。`,
  "notify.exportFailed": "导出失败。",
  "notify.importDone": ({ created, updated, skipped }) =>
    `导入完成：新增 ${created}条，覆盖 ${updated}条，跳过 ${skipped}条。`,
  "notify.importFailed": "CSV 导入失败。",
  "notify.generatedCopied": "随机密码已复制到剪贴板。",
  "notify.generatedCopyFailed": "复制随机密码失败，请检查剪贴板权限。",
  "notify.bulkFavorite": ({ count }) => `已收藏 ${count}条记录。`,
  "notify.bulkUnfavorite": ({ count }) => `已取消收藏 ${count}条记录。`,
  "notify.bulkFavoriteFailed": "批量收藏失败。",
  "notify.themeFailed": "主题切换失败。",
  "notify.languageFailed": "语言切换失败。",
  "notify.onboardingSaveFailed": "保存新手引导状态失败。",
  "notify.enterMasterPasswordFirst": "请先手动输入主密码解锁一次，再启用生物识别。",
  "notify.biometricEnableFailed": "无法启用生物识别。",
  "notify.biometricEnabled": "生物识别已启用。",
  "notify.biometricDisableFailed": "关闭生物识别失败。",
  "notify.biometricDisabled": "生物识别解锁已关闭。",
  "notify.masterPasswordSyncWarning": "主密码已更新，但宿主中的生物识别凭据同步失败。",
  "notify.masterPasswordUpdated": "主密码已更新。",
  "notify.masterPasswordChangeFailed": "修改主密码失败。",
  "notify.minimizeToTrayFailed": "更新托盘设置失败。",
  "notify.minimizeToTrayUpdated": "托盘设置已更新。",
  "notify.launchAtStartupFailed": "更新开机自启动失败。",
  "notify.launchAtStartupUpdated": "开机自启动设置已更新。",
  "notify.excludeFromRecentsFailed": "更新最近任务设置失败。",
  "notify.excludeFromRecentsUpdated": "最近任务设置已更新。",
  "notify.openSystemSettingsFailed": "打开系统设置失败。",
  "notify.openSystemSettingsSuccess": "已打开系统设置。",
  "notify.webDavSaveFailed": "保存 WebDAV 配置失败。",
  "notify.webDavSaved": "WebDAV 配置已保存。",
  "notify.webDavUploadFailed": "上传到 WebDAV 失败。",
  "notify.webDavUploaded": "当前数据已上传到 WebDAV。",
  "notify.webDavDownloadFailed": "从 WebDAV 拉取失败。",
  "notify.deviceNameSaveFailed": "保存设备名称失败。",
  "notify.deviceNameSaved": "设备名称已更新。",
  "notify.lanPublishFailed": "发布局域网同步数据失败。",
  "notify.noLanDevices": "没有扫描到可用设备。",
  "notify.lanDownloadFailed": "从局域网设备拉取失败。",
  "notify.syncCompletedRelocked": "同步完成，请使用同步来源设备的主密码重新解锁。",
  "notify.syncCompletedUnlocked": "同步完成，已使用当前主密码重新解锁。",
  "notify.syncFailed": "同步失败。",
  "notify.vaultInitFailed": "初始化密码库失败。",
});

Object.assign(messages["en-US"], {
  "app.title": "Password Vault",
  "app.bootstrapping": "Initializing your vault...",
  "common.none": "None",
  "common.save": "Save",
  "common.cancel": "Cancel",
  "common.close": "Close",
  "common.confirm": "Confirm",
  "common.done": "Done",
  "common.edit": "Edit",
  "common.delete": "Delete",
  "common.restore": "Restore",
  "common.refresh": "Refresh",
  "common.copy": "Copy",
  "common.lock": "Lock now",
  "common.unlock": "Unlock",
  "common.create": "Create",
  "common.next": "Next",
  "common.skip": "Skip",
  "common.start": "Get started",
  "common.saved": "Saved",
  "common.search": "Search",
  "common.sync": "Sync",
  "common.upload": "Upload",
  "common.download": "Download",
  "common.preview": "Preview",
  "common.settings": "Settings",
  "common.home": "Home",
  "common.list": "List",
  "common.notes": "Notes",
  "common.password": "Password",
  "common.username": "Username",
  "common.enabled": "Enabled",
  "common.disabled": "Disabled",
  "common.currentDevice": "Current device",
  "common.sourceDevice": "Source device",
  "common.unnamedEntry": "Unnamed item",
  "common.unnamedDevice": "Unnamed device",
  "common.requiredField": "This field is required",
  "common.atLeastChars": ({ count }) => `At least ${count} characters required`,
  "common.countItems": ({ count }) => `${count} ${count === 1 ? "item" : "items"}`,
  "common.countNotes": ({ count }) => `${count} ${count === 1 ? "note" : "notes"}`,
  "common.countEntries": ({ count }) => `${count} ${count === 1 ? "entry" : "entries"}`,
  "nav.home": "Home",
  "nav.list": "List",
  "nav.settings": "Settings",
  "search.homeLabel": "Search passwords, usernames, or notes",
  "search.listLabel": "Search items or recently deleted entries",
  "home.overline": "Home",
  "home.title": "Password Vault",
  "home.subtitle": "Quick access to favorites and recent items.",
  "home.savedCount": ({ count }) => `${count} saved`,
  "home.filteredCount": ({ count }) => `${count} in results`,
  "home.notesCount": ({ count }) => `${count} notes`,
  "home.viewAll": "View all",
  "home.securityStatus": "Security status",
  "home.unlockedTitle": "Current session is unlocked",
  "home.unlockedBody": "Locking will clear decrypted content from the current session.",
  "home.masterProtected": "Protected by master password",
  "home.masterProtectedBody": "Passwords are only decrypted on demand after unlock",
  "home.favorites": "Favorites",
  "home.noFavorites": "No favorites yet",
  "home.noFavoritesBody": "Star an item in the list and it will show up here first.",
  "home.sectionRecent": "Recent items",
  "home.sectionSearchPreview": "Search preview",
  "home.openList": "Open list",
  "home.noItems": "No items yet",
  "home.noItemsBody": "Create a password entry and your recent items will appear here.",
  "home.passwordPreview": "Password preview",
  "home.copyPassword": "Copy password",
  "home.copyUsername": "Copy username",
  "list.title": "All items",
  "list.tabAll": "All",
  "list.tabFavorites": "Favorites",
  "list.tabDeleted": "Deleted",
  "list.statusDeleted": ({ count }) => `${count} recently deleted`,
  "list.statusSearch": ({ count }) => `${count} search results`,
  "list.statusAll": ({ total, favorite }) =>
    `${total} total, ${favorite} ${favorite === 1 ? "favorite" : "favorites"}`,
  "list.savedPasswords": "Saved passwords",
  "list.selectedCount": ({ count }) => `${count} selected`,
  "list.select": "Select",
  "list.selectAll": "Select all",
  "list.unselectAll": "Clear all",
  "list.bulkFavorite": "Favorite selected",
  "list.bulkUnfavorite": "Remove favorite",
  "list.bulkDelete": "Delete selected",
  "list.complete": "Done",
  "list.noResults": "No matching password entries",
  "list.noResultsBody": "Create a new password or adjust the search query above.",
  "item.favorite": "Favorite",
  "item.updatedAt": ({ time }) => `Updated ${time}`,
  "item.password": "Password",
  "item.showPlain": "Show plaintext",
  "item.hidePlain": "Hide plaintext",
  "item.copyPassword": "Copy password",
  "item.copyUsername": "Copy username",
  "item.noNotes": "No notes",
  "item.expandNotes": "Expand notes",
  "item.collapseNotes": "Collapse notes",
});

Object.assign(messages["en-US"], {
  "deleted.title": "Recently deleted",
  "deleted.emptyTitle": "0 recently deleted",
  "deleted.emptyBody":
    "Deleted items stay here first, so you can restore them or remove them permanently.",
  "deleted.deletedAt": ({ time }) => `Deleted ${time}`,
  "deleted.restore": "Restore",
  "deleted.permanentDelete": "Delete forever",
  "generator.title": "Password generator",
  "generator.length": "Password length",
  "generator.uppercase": "Uppercase A-Z",
  "generator.lowercase": "Lowercase a-z",
  "generator.numbers": "Numbers 0-9",
  "generator.symbols": "Symbols",
  "generator.preview": "Live preview",
  "generator.emptyPreview": "Select at least one character group",
  "generator.copyPreview": "Copy preview",
  "generator.fillForm": "Use in form",
  "generator.error": "Unable to generate a password.",
  "editor.createTitle": "Create password",
  "editor.editTitle": "Edit password",
  "editor.siteName": "Website or app (optional)",
  "editor.username": "Username",
  "editor.password": "Password",
  "editor.notes": "Notes",
  "editor.addNote": "Add note",
  "editor.noteLabel": ({ index }) => `Note ${index}`,
  "deleteDialog.title": "Move to recently deleted",
  "deleteDialog.message": ({ target }) =>
    `You are about to move ${target} to recently deleted. You can still restore it later from the list or settings page.`,
  "deleteDialog.singleTarget": "this password entry",
  "deleteDialog.multiTarget": ({ count }) =>
    `${count} password ${count === 1 ? "entry" : "entries"}`,
  "deleteDialog.confirm": "Move to recently deleted",
  "master.setupTitle": "Create a master password",
  "master.unlockTitle": "Unlock your vault",
  "master.setupBody":
    "Your master password protects the vault on this device, so please keep it safe.",
  "master.unlockBody":
    "Enter your master password to unlock the vault locally for this session.",
  "master.passphrase": "Master password",
  "master.confirmPassphrase": "Confirm master password",
  "master.createAndUnlock": "Create and unlock",
  "master.useBiometric": ({ label }) => `Unlock with ${label}`,
  "master.minLength": "Master password must be at least 8 characters",
  "master.confirmMismatch": "The two master passwords do not match",
  "changeMaster.title": "Change master password",
  "changeMaster.description":
    "Your new master password must be at least 8 characters. Existing data will be re-protected immediately.",
  "changeMaster.current": "Current master password",
  "changeMaster.next": "New master password",
  "changeMaster.confirm": "Confirm new master password",
  "changeMaster.submit": "Update master password",
  "changeMaster.mismatch": "The new passwords do not match",
  "onboarding.title": "Quick guide",
  "onboarding.slide1.title": "Welcome to your vault",
  "onboarding.slide1.body":
    "Home highlights your overview, favorites, and recent items for quick access.",
  "onboarding.slide2.title": "Manage everything in the list view",
  "onboarding.slide2.body":
    "The list view helps you browse all items, favorites, and recently deleted entries.",
  "onboarding.slide3.title": "Settings keep preferences and sync in one place",
  "onboarding.slide3.body":
    "Appearance, security, import/export, WebDAV, and LAN sync all live in the settings page.",
  "settings.headerDescription": ({ count, deletedCount }) =>
    `${count} saved, ${deletedCount} recently deleted`,
  "settings.appearance": "Appearance",
  "settings.security": "Security",
  "settings.language": "Language",
  "settings.language.zhCn": "Simplified Chinese",
  "settings.language.enUs": "English",
  "settings.appearance.themeMode": "Theme mode",
  "settings.appearance.themeModeHint": ({ theme }) => `Currently showing ${theme}`,
  "settings.appearance.theme.system": "Follow system",
  "settings.appearance.theme.light": "Light",
  "settings.appearance.theme.dark": "Dark",
  "settings.appearance.resolved.light": "light mode",
  "settings.appearance.resolved.dark": "dark mode",
  "settings.changeMaster": "Change master password",
  "settings.changeMasterBody":
    "Existing data will be re-protected with the new master password.",
  "settings.biometricUnlock": "Biometric unlock",
  "settings.biometricUnavailable": ({ label }) => `${label} is not available on this device.`,
  "settings.biometricNotIntegrated": "Biometric unlock is not integrated in this host yet.",
  "settings.biometricEnabledBody": ({ label }) =>
    `${label} is enabled and can unlock the vault next time.`,
  "settings.biometricDisabledBody": ({ label }) =>
    `Enable ${label} to unlock the vault directly.`,
  "settings.enableBiometric": "Enable biometrics",
  "settings.disableBiometric": "Disable biometrics",
  "settings.windowsTray": "Minimize to tray when closing",
  "settings.windowsTrayBody":
    "Windows only. Closing the window hides the app to the system tray instead of exiting.",
  "settings.launchAtStartup": "Launch at startup",
  "settings.launchAtStartupBody": "Start the app automatically after signing in to Windows.",
  "settings.excludeFromRecents": "Hide from recent tasks",
  "settings.excludeFromRecentsBody":
    "On Android, the app will be hidden from the recent tasks screen.",
  "settings.autostartShortcut": "Auto-start and background settings",
  "settings.autostartShortcutBody":
    "Android vendors expose these settings differently, so the app opens the most relevant system page it can find.",
  "settings.openSystemSettings": "Open system settings",
  "settings.importExport": "Import and export",
  "settings.importStrategy": "CSV conflict strategy",
  "settings.importOverwrite": "Overwrite existing entries by username",
  "settings.importSkip": "Skip duplicates by username",
  "settings.importExportHint": "Multiple notes are joined with | when saved.",
  "settings.importExportNativeHint": "The system file manager will be used.",
  "settings.importExportBrowserHint":
    "In browser debugging, web file pickers and downloads are used.",
  "settings.exportCsv": "Export CSV",
  "settings.exportTxt": "Export TXT",
  "settings.importCsv": "Import CSV",
  "settings.webDav": "WebDAV sync",
  "settings.encryptedSnapshot": "Encrypted snapshot",
  "settings.webDavUrl": "WebDAV URL",
  "settings.webDavPath": "Remote file path",
  "settings.webDavUsername": "Username",
  "settings.webDavPassword": "Password",
  "settings.webDavHint":
    "Only encrypted snapshots are stored in WebDAV. Plaintext passwords are never uploaded.",
  "settings.webDavPasswordSaved":
    "A WebDAV password is already stored. Leave this field blank to keep it unchanged.",
  "settings.webDavPasswordEmpty":
    "Fill in username and password if your server requires authentication.",
  "settings.saveConfig": "Save configuration",
  "settings.uploadCurrentData": "Upload current data",
  "settings.downloadFromWebDav": "Download from WebDAV",
  "settings.lanSync": "LAN sync",
  "settings.scanDevices": "Scan devices",
  "settings.deviceIdentity": "This device",
  "settings.deviceIdentityBody":
    "Other devices will see this name and use it to identify the sync source.",
  "settings.deviceName": "Device name",
  "settings.saveName": "Save name",
  "settings.lanHint":
    "Before syncing, the app shows the latest item added on both devices so you can identify which side is newer.",
  "settings.noLanDevices": "No other available devices have been discovered yet",
  "settings.syncAvailable": "Ready to sync",
  "settings.syncUnavailable": "No snapshot",
  "settings.latestAdded": "Latest added",
  "settings.lastPublished": ({ time }) => `Published ${time}`,
  "settings.useDeviceData": "Use this device as source",
  "syncConfirm.title": "Confirm sync",
  "syncConfirm.description": ({ source }) =>
    `You are about to replace the current device data with the data from ${source}.`,
  "syncConfirm.warning":
    "To avoid syncing the wrong vault, both sides show their latest added item below. Confirming will replace this device with the encrypted full-vault snapshot from the source.",
  "syncConfirm.addedAt": ({ time }) => `Added ${time}`,
  "notify.unlockFailed": "Unable to unlock the vault.",
  "notify.biometricUnavailable": ({ label }) => `Unable to unlock with ${label}.`,
  "notify.biometricStoredPasswordExpired":
    "Device verification succeeded, but the stored master password is no longer valid. Enter it manually once and enable biometrics again.",
  "notify.readDraftFailed": "Unable to read the selected entry.",
  "notify.recordCreated": "Password entry created.",
  "notify.recordUpdated": "Password entry updated.",
  "notify.saveFailed": "Unable to save changes.",
  "notify.decryptFailed": "Unable to decrypt the password.",
  "notify.favoriteFailed": "Unable to update favorite state.",
  "notify.passwordCopied": "Password copied to clipboard.",
  "notify.usernameCopied": "Username copied to clipboard.",
  "notify.copyPasswordFailed": "Unable to copy the password. Check clipboard permissions.",
  "notify.copyUsernameFailed": "Unable to copy the username. Check clipboard permissions.",
  "notify.deleted": "Password entry moved to recently deleted.",
  "notify.deletedMany": ({ count }) =>
    `${count} password ${count === 1 ? "entry was" : "entries were"} moved to recently deleted.`,
  "notify.deleteFailed": "Unable to delete the selected entry.",
  "notify.restored": "Password entry restored.",
  "notify.restoreFailed": "Unable to restore the selected entry.",
  "notify.permanentlyDeleted": "Password entry permanently deleted.",
  "notify.permanentDeleteFailed": "Unable to delete the selected entry permanently.",
  "notify.exportSaved": ({ format }) => `${format} saved successfully.`,
  "notify.exportSuccess": ({ format }) => `${format} exported successfully.`,
  "notify.exportFailed": "Unable to export your data.",
  "notify.importDone": ({ created, updated, skipped }) =>
    `Import complete: ${created} created, ${updated} overwritten, ${skipped} skipped.`,
  "notify.importFailed": "CSV import failed.",
  "notify.generatedCopied": "Generated password copied to clipboard.",
  "notify.generatedCopyFailed":
    "Unable to copy the generated password. Check clipboard permissions.",
  "notify.bulkFavorite": ({ count }) =>
    `${count} ${count === 1 ? "entry was" : "entries were"} added to favorites.`,
  "notify.bulkUnfavorite": ({ count }) =>
    `${count} ${count === 1 ? "entry was" : "entries were"} removed from favorites.`,
  "notify.bulkFavoriteFailed": "Unable to update favorites in bulk.",
  "notify.themeFailed": "Unable to change the theme.",
  "notify.languageFailed": "Unable to change the language.",
  "notify.onboardingSaveFailed": "Unable to save onboarding state.",
  "notify.enterMasterPasswordFirst":
    "Unlock once with the master password before enabling biometrics.",
  "notify.biometricEnableFailed": "Unable to enable biometrics.",
  "notify.biometricEnabled": "Biometric unlock enabled.",
  "notify.biometricDisableFailed": "Unable to disable biometrics.",
  "notify.biometricDisabled": "Biometric unlock disabled.",
  "notify.masterPasswordSyncWarning":
    "The master password was updated, but host biometric credentials could not be synchronized.",
  "notify.masterPasswordUpdated": "Master password updated.",
  "notify.masterPasswordChangeFailed": "Unable to change the master password.",
  "notify.minimizeToTrayFailed": "Unable to update the tray preference.",
  "notify.minimizeToTrayUpdated": "Tray preference updated.",
  "notify.launchAtStartupFailed": "Unable to update the launch-at-startup preference.",
  "notify.launchAtStartupUpdated": "Launch-at-startup preference updated.",
  "notify.excludeFromRecentsFailed": "Unable to update the recent tasks preference.",
  "notify.excludeFromRecentsUpdated": "Recent tasks preference updated.",
  "notify.openSystemSettingsFailed": "Unable to open system settings.",
  "notify.openSystemSettingsSuccess": "System settings opened.",
  "notify.webDavSaveFailed": "Unable to save the WebDAV configuration.",
  "notify.webDavSaved": "WebDAV configuration saved.",
  "notify.webDavUploadFailed": "Unable to upload to WebDAV.",
  "notify.webDavUploaded": "Current data uploaded to WebDAV.",
  "notify.webDavDownloadFailed": "Unable to download from WebDAV.",
  "notify.deviceNameSaveFailed": "Unable to save the device name.",
  "notify.deviceNameSaved": "Device name updated.",
  "notify.lanPublishFailed": "Unable to publish LAN sync data.",
  "notify.noLanDevices": "No available devices were found on the local network.",
  "notify.lanDownloadFailed": "Unable to download data from the selected LAN device.",
  "notify.syncCompletedRelocked":
    "Sync completed. Please unlock again with the master password from the source device.",
  "notify.syncCompletedUnlocked":
    "Sync completed and the vault was unlocked again with the current master password.",
  "notify.syncFailed": "Sync failed.",
  "notify.vaultInitFailed": "Unable to initialize the vault.",
});

Object.assign(messages["zh-CN"], {
  "master.secretKey": "Secret Key",
  "master.secretKeyRequired": "此保险库需要主密码和 Secret Key 一起解锁。",
  "master.secretKeyHint": ({ hint }) => `当前提示：${hint}`,
  "settings.sectionListHint": "选择一个分类进入二级设置页面。",
  "settings.sectionAppearanceBody": "主题、语言与显示方式",
  "settings.sectionSecurityBody": "主密码、生物识别与 Secret Key",
  "settings.sectionSyncBody": "WebDAV、局域网设备与同步来源",
  "settings.sectionDataBody": "导入导出、最近删除与数据整理",
  "settings.sectionPlatformBody": "系统集成、自启动与窗口行为",
  "settings.dataManagement": "数据管理",
  "settings.platformIntegration": "平台集成",
  "settings.biometricReauth.title": "定期重新输入主密码",
  "settings.biometricReauth.body": "生物识别只负责解锁本机数据密钥。到期后需要再次手动输入主密码，才能继续使用生物识别。",
  "settings.biometricReauth.24h": "24 小时",
  "settings.biometricReauth.72h": "72 小时",
  "settings.biometricReauth.1w": "1 周",
  "settings.biometricReauth.1m": "1 个月",
  "settings.biometricReauth.never": "永不",
  "settings.secretKey": "Secret Key",
  "settings.secretKeyBody": "Secret Key 是跨设备恢复和同步解锁所需的第二层密钥，请单独妥善保存。",
  "settings.secretKeyHintTitle": "当前 Secret Key 提示",
  "settings.revealSecretKey": "显示 Secret Key",
  "settings.copySecretKey": "复制 Secret Key",
  "notify.biometricKeySyncFailed": "已手动解锁，但宿主中的生物识别密钥状态没有同步成功。",
  "notify.biometricManualUnlockRequired": "已到主密码复验时间，请先手动输入主密码一次。",
  "notify.biometricStoredKeyInvalid": "本机保存的生物识别解锁密钥已失效，请手动解锁后重新启用生物识别。",
  "notify.biometricReauthUpdated": "生物识别复验时间已更新。",
  "notify.biometricReauthUpdateFailed": "更新生物识别复验时间失败。",
  "notify.secretKeyCreated": "已生成新的 Secret Key，请尽快在设置中查看并妥善保存。",
  "notify.unlockRequiredForSecretKey": "请先解锁后再查看 Secret Key。",
  "notify.secretKeyRevealFailed": "显示 Secret Key 失败。",
  "notify.secretKeyCopied": "Secret Key 已复制到剪贴板。",
  "notify.secretKeyCopyFailed": "复制 Secret Key 失败，请检查剪贴板权限。",
  "notify.syncCompletedRelockedRecovery": "同步已完成。当前设备已重新锁定，请使用来源设备的主密码和 Secret Key 重新解锁，并按需重新启用生物识别。",
});

Object.assign(messages["en-US"], {
  "master.secretKey": "Secret Key",
  "master.secretKeyRequired": "This vault requires both your master password and Secret Key.",
  "master.secretKeyHint": ({ hint }) => `Hint: ${hint}`,
  "settings.sectionListHint": "Choose a category to open its detail page.",
  "settings.sectionAppearanceBody": "Theme, language, and display style",
  "settings.sectionSecurityBody": "Master password, biometrics, and Secret Key",
  "settings.sectionSyncBody": "WebDAV, LAN devices, and sync source",
  "settings.sectionDataBody": "Import/export, recently deleted, and data cleanup",
  "settings.sectionPlatformBody": "System integration, auto-start, and window behavior",
  "settings.dataManagement": "Data management",
  "settings.platformIntegration": "Platform integration",
  "settings.biometricReauth.title": "Require master password again",
  "settings.biometricReauth.body": "Biometrics only unlock the local device key. When the interval expires, you must manually enter the master password again before biometrics can keep unlocking the vault.",
  "settings.biometricReauth.24h": "24 hours",
  "settings.biometricReauth.72h": "72 hours",
  "settings.biometricReauth.1w": "1 week",
  "settings.biometricReauth.1m": "1 month",
  "settings.biometricReauth.never": "Never",
  "settings.secretKey": "Secret Key",
  "settings.secretKeyBody": "The Secret Key is the second factor required for cross-device recovery and sync unlock. Store it separately from your master password.",
  "settings.secretKeyHintTitle": "Current Secret Key hint",
  "settings.revealSecretKey": "Reveal Secret Key",
  "settings.copySecretKey": "Copy Secret Key",
  "notify.biometricKeySyncFailed": "The vault was unlocked manually, but the host biometric key state could not be synchronized.",
  "notify.biometricManualUnlockRequired": "The manual unlock interval has expired. Enter the master password once before using biometrics again.",
  "notify.biometricStoredKeyInvalid": "The stored biometric device key is no longer valid. Unlock manually and enable biometrics again.",
  "notify.biometricReauthUpdated": "Biometric re-auth interval updated.",
  "notify.biometricReauthUpdateFailed": "Unable to update the biometric re-auth interval.",
  "notify.secretKeyCreated": "A new Secret Key was created. Open Settings and store it somewhere safe.",
  "notify.unlockRequiredForSecretKey": "Unlock the vault before revealing the Secret Key.",
  "notify.secretKeyRevealFailed": "Unable to reveal the Secret Key.",
  "notify.secretKeyCopied": "Secret Key copied to clipboard.",
  "notify.secretKeyCopyFailed": "Unable to copy the Secret Key. Check clipboard permissions.",
  "notify.syncCompletedRelockedRecovery": "Sync completed. This device is locked again. Use the source device master password and Secret Key to unlock, then re-enable biometrics if needed.",
});

Object.assign(messages["zh-CN"], {
  "settings.appearance.navAlignment": "\u5e95\u90e8\u680f\u4f4d\u7f6e",
  "settings.appearance.navAlignmentBody": "\u8ba9\u5e95\u90e8\u5bfc\u822a\u6d6e\u5c9b\u5728\u754c\u9762\u4e2d\u5c45\u4e2d\u3001\u9760\u5de6\u6216\u9760\u53f3\u505c\u9760\u3002",
  "settings.appearance.navAlignment.center": "\u5c45\u4e2d",
  "settings.appearance.navAlignment.left": "\u9760\u5de6",
  "settings.appearance.navAlignment.right": "\u9760\u53f3",
});

Object.assign(messages["en-US"], {
  "settings.appearance.navAlignment": "Bottom bar position",
  "settings.appearance.navAlignmentBody":
    "Place the floating bottom bar in the center, near the left edge, or near the right edge.",
  "settings.appearance.navAlignment.center": "Centered",
  "settings.appearance.navAlignment.left": "Left",
  "settings.appearance.navAlignment.right": "Right",
});

Object.assign(messages["zh-CN"], {
  "settings.trayAutoLock": "\u6536\u7eb3\u5230\u6258\u76d8\u540e\u81ea\u52a8\u9501\u5b9a",
  "settings.trayAutoLockBody":
    "\u5f53 Windows \u7a97\u53e3\u88ab\u6536\u7eb3\u5230\u6258\u76d8\u540e\uff0c\u5230\u65f6\u4f1a\u81ea\u52a8\u9501\u5b9a\u5e76\u53d1\u51fa\u7cfb\u7edf\u901a\u77e5\u3002",
  "settings.backgroundAutoLock": "\u9000\u5230\u540e\u53f0\u540e\u81ea\u52a8\u9501\u5b9a",
  "settings.backgroundAutoLockBody":
    "\u5f53 Android \u5e94\u7528\u9000\u5230\u540e\u53f0\u540e\uff0c\u5230\u65f6\u4f1a\u81ea\u52a8\u9501\u5b9a\u5e76\u53d1\u51fa\u7cfb\u7edf\u901a\u77e5\u3002",
  "settings.autoLock.1m": "1 \u5206\u949f",
  "settings.autoLock.5m": "5 \u5206\u949f",
  "settings.autoLock.15m": "15 \u5206\u949f",
  "settings.autoLock.30m": "30 \u5206\u949f",
  "settings.autoLock.1h": "1 \u5c0f\u65f6",
  "settings.autoLock.never": "\u4ece\u4e0d",
  "notify.autoLocked": "\u4fdd\u9669\u5e93\u5df2\u88ab\u5bbf\u4e3b\u81ea\u52a8\u9501\u5b9a\u3002",
  "notify.trayAutoLockUpdated": "\u6258\u76d8\u81ea\u52a8\u9501\u5b9a\u65f6\u95f4\u5df2\u66f4\u65b0\u3002",
  "notify.trayAutoLockFailed": "\u66f4\u65b0\u6258\u76d8\u81ea\u52a8\u9501\u5b9a\u65f6\u95f4\u5931\u8d25\u3002",
  "notify.backgroundAutoLockUpdated": "\u540e\u53f0\u81ea\u52a8\u9501\u5b9a\u65f6\u95f4\u5df2\u66f4\u65b0\u3002",
  "notify.backgroundAutoLockFailed": "\u66f4\u65b0\u540e\u53f0\u81ea\u52a8\u9501\u5b9a\u65f6\u95f4\u5931\u8d25\u3002",
});

Object.assign(messages["en-US"], {
  "settings.trayAutoLock": "Auto-lock after minimizing to tray",
  "settings.trayAutoLockBody":
    "When the app is hidden to the Windows tray, it will lock itself after the selected delay and send a system notification.",
  "settings.backgroundAutoLock": "Auto-lock after going to background",
  "settings.backgroundAutoLockBody":
    "When the Android app goes to the background, it will lock itself after the selected delay and send a system notification.",
  "settings.autoLock.1m": "1 minute",
  "settings.autoLock.5m": "5 minutes",
  "settings.autoLock.15m": "15 minutes",
  "settings.autoLock.30m": "30 minutes",
  "settings.autoLock.1h": "1 hour",
  "settings.autoLock.never": "Never",
  "notify.autoLocked": "The vault was locked automatically by the host.",
  "notify.trayAutoLockUpdated": "Tray auto-lock delay updated.",
  "notify.trayAutoLockFailed": "Unable to update the tray auto-lock delay.",
  "notify.backgroundAutoLockUpdated": "Background auto-lock delay updated.",
  "notify.backgroundAutoLockFailed": "Unable to update the background auto-lock delay.",
});

function normalizeLocale(locale) {
  if (SUPPORTED_LOCALES.includes(locale)) {
    return locale;
  }

  if (String(locale || "").toLowerCase().startsWith("en")) {
    return "en-US";
  }

  return "zh-CN";
}

function normalizeThemeMode(themeMode) {
  return SUPPORTED_THEME_MODES.includes(themeMode) ? themeMode : "system";
}

function normalizeNavAlignment(alignment) {
  return SUPPORTED_NAV_ALIGNMENTS.includes(alignment) ? alignment : "center";
}

function getMessage(locale, key) {
  return messages[locale]?.[key] ?? messages["en-US"]?.[key] ?? key;
}

function interpolate(template, params) {
  return String(template).replace(/\{(\w+)\}/g, (_, token) => String(params?.[token] ?? ""));
}

function translate(key, params = {}) {
  const message = getMessage(state.locale, key);

  if (typeof message === "function") {
    return message(params);
  }

  return interpolate(message, params);
}

function resolveTheme(mode = state.themeMode) {
  const normalizedMode = normalizeThemeMode(mode);
  if (normalizedMode === "system") {
    return state.systemPrefersDark ? "dark" : "light";
  }

  return normalizedMode;
}

function formatDateTime(value) {
  if (!value) {
    return translate("common.none");
  }

  return new Intl.DateTimeFormat(state.locale, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
}

function formatDate(value) {
  if (!value) {
    return translate("common.none");
  }

  return new Intl.DateTimeFormat(state.locale, {
    year: "numeric",
    month: "short",
    day: "numeric",
  }).format(new Date(value));
}

function setLocale(locale) {
  state.locale = normalizeLocale(locale);
}

function setThemeMode(themeMode) {
  state.themeMode = normalizeThemeMode(themeMode);
}

function setNavAlignment(alignment) {
  state.navAlignment = normalizeNavAlignment(alignment);
}

function setSystemPrefersDark(prefersDark) {
  state.systemPrefersDark = Boolean(prefersDark);
}

function getVuetifyLocale(locale = state.locale) {
  return normalizeLocale(locale) === "en-US" ? "en" : "zhHans";
}

export function getDefaultLocale() {
  return normalizeLocale(globalThis?.navigator?.language);
}

export function useAppPreferences() {
  return {
    state,
    t: translate,
    formatDateTime,
    formatDate,
    setLocale,
    setThemeMode,
    setNavAlignment,
    setSystemPrefersDark,
    getVuetifyLocale,
    localeOptions: computed(() => [
      { value: "zh-CN", title: translate("settings.language.zhCn") },
      { value: "en-US", title: translate("settings.language.enUs") },
    ]),
    themeModeOptions: computed(() => [
      { value: "system", title: translate("settings.appearance.theme.system") },
      { value: "light", title: translate("settings.appearance.theme.light") },
      { value: "dark", title: translate("settings.appearance.theme.dark") },
    ]),
    navAlignmentOptions: computed(() => [
      { value: "center", title: translate("settings.appearance.navAlignment.center") },
      { value: "left", title: translate("settings.appearance.navAlignment.left") },
      { value: "right", title: translate("settings.appearance.navAlignment.right") },
    ]),
    resolvedTheme: computed(() => resolveTheme()),
  };
}
