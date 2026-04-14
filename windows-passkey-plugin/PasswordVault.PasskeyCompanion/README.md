# PasswordVault.PasskeyCompanion

This is the first scaffold for the future `Windows third-party passkey manager / plugin`
companion app.

## Purpose

The main MAUI Hybrid app keeps:

- vault UI
- password data
- passkey metadata display

This companion project is intended to grow into the packaged Windows-side app that
handles:

- plugin authenticator registration
- plugin credential lifecycle
- Windows-native verification
- safe metadata handoff back to the main vault app

## Current State

Right now this project runs as a lightweight `background companion`.

It checks:

- Windows build
- `webauthn.dll` availability
- required plugin exports
- whether the process is packaged

It also exposes a small local named-pipe bridge that the main MAUI Hybrid host can use to:

- query a live readiness snapshot
- detect whether the background companion is already running
- trigger a real `WebAuthNPluginAddAuthenticator` registration attempt when the packaged prerequisites are satisfied
- capture create-passkey skeleton requests for future plugin callback wiring

The executable now starts in background mode by default and does not show a persistent
window to end users.

The current plugin workflow is now a `native-registration` preparation stage:

- it **does** attempt a real `WebAuthNPluginAddAuthenticator` call once the companion is packaged and the Windows plugin prerequisites are satisfied
- it **does** persist the latest registration result and the operation-signing public key returned by Windows
- it **does** expose the live authenticator state (`enabled / disabled / unknown`) back to the MAUI host
- it **does** treat plugin activation as a first-class workflow event and will automatically re-run registration preparation when the plugin activation path reaches the companion
- it **does** include a local COM authenticator / factory skeleton so we can probe the packaged class ID, factory creation path, and authenticator instantiation path before wiring the final callbacks
- it does **not** intercept full system passkey creation yet
- it does **not** implement the final `IPluginAuthenticator` callback path yet

When the main MAUI app launches it, the companion now links itself to that host process.
If the MAUI host exits normally, crashes, or is terminated, the background companion
will detect that the linked host session is gone and shut itself down automatically.

If you need the old readiness window for debugging, run it with:

```powershell
D:\Code\Password\windows-passkey-plugin\PasswordVault.PasskeyCompanion\bin\Debug\net10.0-windows10.0.19041.0\PasswordVault.PasskeyCompanion.exe --show-status
```

## Build

```powershell
dotnet build D:\Code\Password\windows-passkey-plugin\PasswordVault.PasskeyCompanion\PasswordVault.PasskeyCompanion.csproj
```

## Next Milestones

1. Finish packaged companion / MSIX validation on a Windows build that exposes the plugin APIs.
2. Add the COM authenticator server / class factory path for plugin activation callbacks.
3. Connect captured create-passkey drafts to actual Windows plugin callbacks.
4. Replace the development-time executable launcher with a packaged registration flow.
