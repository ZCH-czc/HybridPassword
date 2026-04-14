# Windows Third-Party Passkey Manager / Plugin Roadmap

## Goal

Move the project from:

- Windows platform passkey metadata management

to:

- a true `Windows third-party passkey manager / plugin` architecture that can
  eventually participate in system passkey flows such as Google passkeys.

## Important Constraint

The current `MAUI Hybrid` host is **not** the final plugin implementation target.

Based on Microsoft's official third-party passkey manager guidance, the plugin
route requires a separate `packaged Windows companion app` that integrates with
the Windows plugin passkey manager model.

So the project should be split into two Windows layers:

1. `Current MAUI Hybrid app`
   - vault UI
   - password vault
   - passkey metadata UI
   - user-facing settings and management

2. `Future packaged Windows companion app`
   - plugin passkey manager registration
   - authenticator lifecycle
   - Windows-native passkey create / use / delete
   - system-facing passkey provider behaviors

## What Is Already Done

Current repository status:

- Windows passkey capability detection
- system passkey metadata listing
- system passkey metadata deletion
- IndexedDB metadata cache
- passkey list / settings UI
- plugin prerequisite detection in the Windows host

## What Still Needs a Separate Companion App

These items should move into the packaged companion app:

- plugin authenticator registration
- plugin credential creation
- plugin credential assertion / sign-in
- authenticator metadata updates
- provider onboarding / Windows integration UX

## Recommended Architecture

### MAUI Hybrid App

Responsibilities:

- show passkey list
- search passkeys
- favorite / soft-delete local metadata
- show plugin readiness state
- explain why a companion app is required
- communicate with the companion app later through a local bridge

### Packaged Companion App

Responsibilities:

- implement the Windows plugin passkey manager path
- own registration and authenticator lifecycle
- translate Windows-native records to metadata
- return safe metadata only to the MAUI app
- never expose private key material to the Vue layer

## Phase Plan

### Phase A: Readiness and separation

- detect Windows plugin API availability
- detect OS build readiness
- document the companion-app requirement
- keep current metadata path working

### Phase B: Companion app scaffold

- add a dedicated Windows packaged project
- define plugin-specific service boundaries
- prepare registration / activation contracts

### Phase C: Native plugin integration

- add authenticator registration
- add create / use / delete passkey flows
- sync metadata back to the MAUI app

### Phase D: UX and recovery

- onboarding
- drift recovery
- system status messaging
- companion app health checks

## Current Decision

For now, this repository should continue to:

- use the current MAUI host for metadata management and readiness checks
- avoid pretending that full plugin creation / assertion is already integrated
- prepare a dedicated packaged companion app as the next real milestone

## Current Scaffold

The repository now includes a first companion scaffold project:

- `windows-passkey-plugin/PasswordVault.PasskeyCompanion`

This scaffold currently provides:

- Windows build and `webauthn.dll` probing
- plugin export probing
- packaged vs. unpackaged process detection
- a background companion process that keeps the plugin readiness bridge alive
- host-session linking so the companion exits if the MAUI app crashes or closes
- an optional debug status UI (`--show-status`) for development only
- a local named-pipe bridge for `status` and `activate` commands
- a real `WebAuthNPluginAddAuthenticator` attempt path once packaged prerequisites are satisfied
- persistent registration-result tracking, including authenticator state and operation-signing public key cache status
- a create-passkey skeleton request capture path for future Windows callback wiring
- MAUI-host-side probing so the main vault app can show companion readiness
- a launcher path so the main vault app can try to start the companion automatically during development

## Current Boundary

The repository is now past the purely mock registration stage:

- the companion can attempt the official Windows plugin registration API
- the companion app manifest now includes a COM-server skeleton entry for the future authenticator class
- the main app can surface whether the plugin is already registered, disabled, enabled, or still blocked
- plugin-activation hits are now tracked and can automatically trigger another registration-preparation pass inside the packaged companion workflow
- the companion now also ships a local COM authenticator / factory skeleton so the repo can probe class-ID alignment and instance creation before the final callback implementation lands

What is still missing:

- the real `IPluginAuthenticator` COM implementation
- packaged validation on a Windows build that exposes the final plugin APIs
- create / get-assertion callback handling

## Official References

- [Plugin passkey manager support](https://learn.microsoft.com/en-us/windows/apps/develop/security/third-party)
- [Support for passkeys in Windows](https://learn.microsoft.com/en-us/windows/security/identity-protection/passkeys/)
- [Web authentication API overview on Windows](https://learn.microsoft.com/en-us/windows/apps/develop/security/reference)
