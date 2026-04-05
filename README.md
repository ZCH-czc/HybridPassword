# Password Vault Hybrid

[中文](#中文) | [English](#english)

## 中文

### 项目简介

`Password Vault Hybrid` 是一个专为 `Android` 与 `Windows` 嵌入式 `WebView` / `Blazor Hybrid` 场景设计的纯前端密码管理器。  
它使用 `Vue 3`、`Vuetify 3` 与 `Vite` 构建 UI，使用 `IndexedDB` 做本地持久化，并通过 `Web Crypto API` 的 `AES-GCM` 对敏感数据进行加密落盘。项目没有后端依赖，适合以离线优先的方式运行，再通过宿主能力补充生物识别、原生文件选择、系统窗口行为和局域网同步。

当前仓库同时包含两部分：

- `Vue` 前端应用：负责密码管理、搜索、导入导出、同步交互与整体界面体验。
- `.NET MAUI / Blazor Hybrid` 宿主：负责在 Windows / Android 上承载前端页面，并提供生物识别、原生文件能力、WebDAV 与局域网同步桥接。

### 适用场景

- 需要将一个纯 Web 密码管理器嵌入 Android WebView
- 需要在 Windows 桌面端通过 Hybrid 宿主承载前端
- 需要零后端依赖、以本地数据为中心的密码保险库
- 需要逐步接入宿主平台能力，而不重写整套 UI

### 核心特性

- 主密码初始化、解锁与修改
- `IndexedDB` 本地持久化
- `AES-GCM` 加密落盘，避免敏感数据明文写入 `localStorage`
- 首页 / 列表 / 设置 三段式布局
- 收藏夹、最近删除、批量收藏、批量删除
- 实时搜索，支持用户名与备注模糊匹配
- 随机密码生成器，支持长度与字符类型控制
- CSV 导入、CSV/TXT 导出
- 首次使用新手引导
- 暗黑模式
- Windows Hello / Android 生物识别解锁桥接
- WebDAV 加密快照同步
- 局域网设备扫描与加密快照同步
- Android 安全区适配、Windows 托盘 / 开机自启等宿主设置

### 技术架构

#### Web 前端

- `Vue 3`
- `Composition API`
- `<script setup>`
- `Vuetify 3`
- `Vite`

#### 数据与安全

- `IndexedDB`：本地数据存储
- `Web Crypto API`：使用 PBKDF2 + AES-GCM 生成和使用主密钥
- 主密码校验：通过保险库配置中的验证密文完成
- 同步载体：使用整库“加密快照”进行 WebDAV / 局域网传输

#### Hybrid 宿主

- `.NET MAUI`
- `HybridWebView`
- `SecureStorage`
- Windows / Android 原生平台桥接

### 同步设计说明

#### WebDAV

- WebDAV 同步传输的是整份“加密后的保险库快照”
- 服务端不会收到明文密码
- 当前采用单文件上传 / 下载方式，适合个人或轻量部署

#### 局域网同步

- 使用宿主侧 UDP 广播发现同一局域网内的设备
- 使用宿主侧本地 HTTP 通道拉取已加密快照
- 同步前会展示“当前设备”和“来源设备”的最新新增项目，帮助用户确认哪台设备数据更新
- 当前第一版策略为“来源设备整库覆盖当前设备”，适合个人多设备同步

### 目录结构

```text
.
├─ blazor/                                  # .NET MAUI / Blazor Hybrid 宿主
│  └─ blazorApp/blazorApp
│     ├─ Platforms/
│     ├─ Resources/
│     ├─ Services/
│     └─ wwwroot/
├─ scripts/                                 # 构建与同步脚本
├─ src/                                     # Vue 前端源码
│  ├─ components/                           # 业务组件
│  ├─ composables/                          # 组合式逻辑
│  ├─ models/                               # 数据模型
│  ├─ plugins/                              # Vuetify 等插件初始化
│  ├─ styles/                               # 全局样式
│  └─ utils/                                # 加密、存储、CSV、宿主桥接、同步工具
├─ index.html
├─ package.json
└─ vite.config.js
```

### 本地开发

安装依赖并启动前端开发服务：

```bash
npm i
npm run dev
```

仅构建 Vue 前端：

```bash
npm run build
```

构建前端并同步到 MAUI 宿主 `wwwroot`：

```bash
npm run build:hybrid
```

仅同步已构建好的前端资源到 MAUI 宿主：

```bash
npm run sync:maui
```

### 宿主项目编译

Windows:

```bash
dotnet build blazor/blazorApp/blazorApp/blazorApp.csproj -f net10.0-windows10.0.19041.0
```

Android:

```bash
dotnet build blazor/blazorApp/blazorApp/blazorApp.csproj -f net10.0-android
```

如果在 `build:hybrid` 之后遇到 MAUI 仍引用旧的哈希静态资源，可以先执行一次清理再重新编译：

```bash
dotnet clean blazor/blazorApp/blazorApp/blazorApp.csproj -f net10.0-windows10.0.19041.0
dotnet clean blazor/blazorApp/blazorApp/blazorApp.csproj -f net10.0-android
```

### 关键安全说明

- 不使用 `localStorage` 保存明文密码数据
- 密码项目以加密形式写入 `IndexedDB`
- 生物识别目前用于“解锁主密码副本”的宿主辅助流程
- WebDAV 与局域网同步传输的是加密快照，不是解密后的业务数据
- 当前局域网同步为整库替换，操作前会二次确认

### 宿主能力

#### Windows

- 生物识别解锁
- 关闭窗口收纳到系统托盘
- 开机自启动
- 原生文件保存 / 选择

#### Android

- 生物识别解锁
- 原生文件保存 / 选择
- 最近任务隐藏
- 状态栏 / 底部手势区安全区适配
- 自启动 / 后台运行相关系统设置跳转

### 适配与打包注意事项

- `vite.config.js` 已使用相对路径构建，便于本地 WebView 加载
- Web 调试环境会回退到浏览器文件导入导出能力
- Hybrid 宿主中会优先使用原生文件选择与保存能力
- 如果宿主启用了严格 CSP，至少需要放行：
  - `script-src 'self'`
  - `style-src 'self' 'unsafe-inline'`
  - `font-src 'self' data:`

### 后续可扩展方向

- 增量同步与冲突合并
- 蓝牙设备发现与同步
- 多保险库 / 多分类标签
- 宿主侧更强的密钥包装策略

---

## English

### Overview

`Password Vault Hybrid` is a frontend-first password manager built for `Android` and `Windows` embedded `WebView` / `Blazor Hybrid` scenarios.  
It uses `Vue 3`, `Vuetify 3`, and `Vite` for the UI layer, `IndexedDB` for local persistence, and `AES-GCM` via the `Web Crypto API` for encrypted at-rest storage. The app is designed to work without a backend and can later be enhanced through host-provided capabilities such as biometrics, native file pickers, system window behaviors, and local network sync.

This repository contains two major parts:

- A `Vue` application for the password vault UI and interaction flow
- A `.NET MAUI / Blazor Hybrid` host that embeds the web app and exposes platform features

### Main Features

- Master password setup, unlock, and update
- Local persistence with `IndexedDB`
- Encrypted vault storage using `AES-GCM`
- Home / List / Settings navigation structure
- Favorites, recently deleted items, batch favorite, batch delete
- Live search across usernames and notes
- Random password generator
- CSV import and CSV/TXT export
- First-run onboarding
- Dark mode
- Windows Hello / Android biometric unlock bridge
- WebDAV encrypted snapshot sync
- LAN device discovery and encrypted snapshot sync
- Android safe-area support and Windows tray / auto-start host options

### Tech Stack

#### Frontend

- `Vue 3`
- `Composition API`
- `<script setup>`
- `Vuetify 3`
- `Vite`

#### Storage and Security

- `IndexedDB` for local storage
- `PBKDF2 + AES-GCM` with the `Web Crypto API`
- Vault verification ciphertext for master password validation
- Encrypted full-vault snapshots for WebDAV and LAN sync

#### Host Layer

- `.NET MAUI`
- `HybridWebView`
- `SecureStorage`
- Native Windows / Android interop services

### Sync Strategy

#### WebDAV

- Sync uploads and downloads a fully encrypted vault snapshot
- Plaintext passwords are not sent to the server
- The current implementation uses a single remote file path

#### LAN Sync

- Device discovery is handled through host-side UDP broadcast
- Snapshot retrieval is handled through a host-side local HTTP endpoint
- Before syncing, the UI shows the latest item added on both devices so the user can identify the more recent vault
- The current first version applies a full replacement from the selected source device

### Project Structure

```text
.
├─ blazor/
├─ scripts/
├─ src/
│  ├─ components/
│  ├─ composables/
│  ├─ models/
│  ├─ plugins/
│  ├─ styles/
│  └─ utils/
├─ index.html
├─ package.json
└─ vite.config.js
```

### Local Development

Install dependencies and run the web app in dev mode:

```bash
npm i
npm run dev
```

Build the Vue frontend only:

```bash
npm run build
```

Build the frontend and sync the output into the MAUI host:

```bash
npm run build:hybrid
```

Sync an existing frontend build into the MAUI host:

```bash
npm run sync:maui
```

### Host Build

Windows:

```bash
dotnet build blazor/blazorApp/blazorApp/blazorApp.csproj -f net10.0-windows10.0.19041.0
```

Android:

```bash
dotnet build blazor/blazorApp/blazorApp/blazorApp.csproj -f net10.0-android
```

If MAUI still references stale hashed assets after `build:hybrid`, run a clean first and build again.

### Security Notes

- Plaintext vault data is not stored in `localStorage`
- Password entries are stored in encrypted form inside `IndexedDB`
- Biometrics currently act as a host-assisted unlock flow for a stored master password copy
- WebDAV and LAN sync operate on encrypted snapshots, not decrypted business records
- LAN sync currently uses a full-vault replacement strategy and always requires confirmation

### Packaging Notes

- `vite.config.js` uses relative asset paths for embedded WebView loading
- Browser-based debugging falls back to web file APIs
- In the hybrid host, native save/open dialogs are preferred
- For strict CSP scenarios, allow at least:
  - `script-src 'self'`
  - `style-src 'self' 'unsafe-inline'`
  - `font-src 'self' data:`

### Roadmap Ideas

- Incremental sync and conflict resolution
- Bluetooth-based device sync
- Multiple vaults or category tags
- Stronger host-side key wrapping
