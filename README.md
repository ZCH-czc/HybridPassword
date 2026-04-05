# Password Vault Hybrid

## Overview

`Password Vault Hybrid` is a frontend-first password manager built for `Android` and `Windows` embedded `WebView` / `Blazor Hybrid` scenarios.

It uses `Vue 3`, `Vuetify 3`, and `Vite` for the UI layer, `IndexedDB` for local persistence, and `AES-GCM` via the `Web Crypto API` for encrypted at-rest storage. The app is designed to work without a backend and can be extended through host-provided capabilities such as biometrics, native file dialogs, system window behaviors, WebDAV sync, and LAN sync.

This repository contains two main parts:

- A `Vue` application that handles the password vault UI, search, import/export, sync flows, and user experience.
- A `.NET MAUI / Blazor Hybrid` host that embeds the web app and exposes platform features for Windows and Android.

## Use Cases

- Embedding a pure web password manager into an Android WebView
- Hosting the same frontend inside a Windows desktop hybrid shell
- Running a local-first password vault with zero backend dependency
- Gradually integrating native platform capabilities without rewriting the UI

## Core Features

- Master password setup, unlock, and update
- Local persistence with `IndexedDB`
- Encrypted vault storage using `AES-GCM`
- Home / List / Settings navigation structure
- Favorites, recently deleted items, batch favorite, and batch delete
- Live search across usernames and notes
- Random password generator
- CSV import and CSV/TXT export
- First-run onboarding
- Dark mode
- Windows Hello / Android biometric unlock bridge
- WebDAV encrypted snapshot sync
- LAN device discovery and encrypted snapshot sync
- Android safe-area support and Windows tray / auto-start host options

## Architecture

### Frontend

- `Vue 3`
- `Composition API`
- `<script setup>`
- `Vuetify 3`
- `Vite`

### Storage and Security

- `IndexedDB` for local storage
- `PBKDF2 + AES-GCM` with the `Web Crypto API`
- Vault verification ciphertext for master password validation
- Encrypted full-vault snapshots for WebDAV and LAN sync

### Host Layer

- `.NET MAUI`
- `HybridWebView`
- `SecureStorage`
- Native Windows / Android interop services

## Sync Strategy

### WebDAV

- Sync uploads and downloads a fully encrypted vault snapshot
- Plaintext passwords are not sent to the server
- The current implementation uses a single remote file path

### LAN Sync

- Device discovery is handled through host-side UDP broadcast
- Snapshot retrieval is handled through a host-side local HTTP endpoint
- Before syncing, the UI shows the latest item added on both devices so the user can identify the more recent vault
- The current first version applies a full replacement from the selected source device

## Project Structure

```text
.
├─ blazor/                                  # .NET MAUI / Blazor Hybrid host
│  └─ blazorApp/blazorApp
│     ├─ Platforms/
│     ├─ Resources/
│     ├─ Services/
│     └─ wwwroot/
├─ scripts/                                 # build and sync scripts
├─ src/                                     # Vue frontend source
│  ├─ components/                           # UI components
│  ├─ composables/                          # composable logic
│  ├─ models/                               # data models
│  ├─ plugins/                              # plugin setup
│  ├─ styles/                               # global styles
│  └─ utils/                                # crypto, storage, CSV, bridge, sync helpers
├─ index.html
├─ package.json
└─ vite.config.js
```

## Local Development

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

## Host Build

Windows:

```bash
dotnet build blazor/blazorApp/blazorApp/blazorApp.csproj -f net10.0-windows10.0.19041.0
```

Android:

```bash
dotnet build blazor/blazorApp/blazorApp/blazorApp.csproj -f net10.0-android
```

If MAUI still references stale hashed assets after `build:hybrid`, run a clean first and build again:

```bash
dotnet clean blazor/blazorApp/blazorApp/blazorApp.csproj -f net10.0-windows10.0.19041.0
dotnet clean blazor/blazorApp/blazorApp/blazorApp.csproj -f net10.0-android
```

## Security Notes

- Plaintext vault data is not stored in `localStorage`
- Password entries are stored in encrypted form inside `IndexedDB`
- Biometrics currently act as a host-assisted unlock flow for a stored master password copy
- WebDAV and LAN sync operate on encrypted snapshots, not decrypted business records
- LAN sync currently uses a full-vault replacement strategy and always requires confirmation

## Host Features

### Windows

- Biometric unlock
- Minimize to system tray on close
- Auto-start on system boot
- Native save/open dialogs

### Android

- Biometric unlock
- Native save/open dialogs
- Hide from recent tasks
- Safe-area handling for status bar and bottom gesture area
- Deep links into vendor/system settings for auto-start or background behavior

## Packaging Notes

- `vite.config.js` uses relative asset paths for embedded WebView loading
- Browser-based debugging falls back to web file APIs
- In the hybrid host, native save/open dialogs are preferred
- For strict CSP scenarios, allow at least:
  - `script-src 'self'`
  - `style-src 'self' 'unsafe-inline'`
  - `font-src 'self' data:`

## Roadmap Ideas

- Incremental sync and conflict resolution
- Bluetooth-based device sync
- Multiple vaults or category tags
- Stronger host-side key wrapping
