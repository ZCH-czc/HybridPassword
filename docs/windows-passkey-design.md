# Windows Passkey Integration Design

## Goal

Add `Windows-only` passkey support to `Password Vault Hybrid` without breaking the
existing password vault, web build, Android host, or current sync model.

The passkey feature should:

- integrate with the Windows passkey / third-party provider model
- keep private key material in the Windows-native security boundary
- expose only searchable metadata to the Vue app
- remain disabled on Web and Android unless a future platform implementation is added

## Scope Decision

This design intentionally targets `Windows` first.

It does **not** try to make the web app itself a system passkey provider.
The Vue layer stays a UI shell and local metadata store; the MAUI Windows host
owns passkey operations and Windows integration.

## Key Principle

Do **not** store passkey private keys like normal password records.

Current password records are protected by:

- Vue state
- IndexedDB
- Web Crypto
- vault key wrapping

That model is acceptable for encrypted password entries, but it is not the
right boundary for system passkey private keys.

For passkeys:

- the Windows-native provider should hold or broker the real credential material
- the Vue / IndexedDB layer should store metadata only

## What Changes in Storage

The existing password vault structure can stay in place.

### Keep As-Is

- `password records`
- `vault config`
- `recently deleted`
- `favorites`
- `WebDAV / LAN encrypted snapshot sync for passwords`

### Add

A new `passkey metadata` store in IndexedDB.

This is an additive change, not a destructive migration.

## Proposed Passkey Metadata Model

Frontend metadata shape:

```js
{
  id: string,
  credentialId: string,
  rpId: string,
  rpIdNormalized: string,
  username: string,
  displayName: string,
  userHandle: string,
  transportHints: string[],
  nativeProviderRecordId: string,
  origin: string,
  sourceDeviceId: string,
  syncState: "local-only" | "windows-managed" | "sync-disabled",
  attestationFormat: string,
  isFavorite: boolean,
  deletedAt: number | null,
  createdAt: number,
  updatedAt: number,
  lastUsedAt: number | null
}
```

Notes:

- `nativeProviderRecordId` links the Vue layer to the Windows-native entry.
- `credentialId` is metadata, not the private key.
- `syncState` is explicit so the UI can clearly show that passkeys are not
  synced with the normal password snapshot flow in phase 1.

## IndexedDB Plan

Add a new object store:

- `passkeyRecords`

Recommended indexes:

- `byRpIdNormalized`
- `byUsername`
- `byDeletedAt`
- `byUpdatedAt`
- `byNativeProviderRecordId`

The current password stores should not be renamed or restructured.

## Windows Host Responsibilities

The Windows host should own:

- provider registration / plugin integration
- create passkey
- sign with passkey
- delete passkey
- enumerate locally managed passkeys
- return metadata to Vue
- map native provider records to Vue metadata rows

The Windows host should **not** return raw private keys to the Vue layer.

## Proposed Host Service Layer

Add a new host service:

- `IPasskeyHostService`
- `PasskeyHostService`

Responsibilities:

- query Windows passkey capability state
- create a passkey through the Windows provider flow
- request passkey assertion / use flow
- delete a Windows-native passkey
- list passkey metadata
- open OS settings if Windows requires provider onboarding

## Proposed Bridge Contract

Add JS bridge methods like:

- `GetPasskeyState()`
- `ListPasskeys()`
- `CreatePasskey(request)`
- `UsePasskey(request)`
- `DeletePasskey(request)`
- `RefreshPasskeyMetadata()`

Suggested request models:

```csharp
public sealed class PasskeyCreateRequest
{
    public string RpId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string UserHandle { get; set; } = string.Empty;
}

public sealed class PasskeyUseRequest
{
    public string RpId { get; set; } = string.Empty;
    public string CredentialId { get; set; } = string.Empty;
}

public sealed class PasskeyDeleteRequest
{
    public string NativeProviderRecordId { get; set; } = string.Empty;
}
```

Suggested metadata result model:

```csharp
public sealed class PasskeyMetadataState
{
    public string NativeProviderRecordId { get; set; } = string.Empty;
    public string CredentialId { get; set; } = string.Empty;
    public string RpId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string UserHandle { get; set; } = string.Empty;
    public string[] TransportHints { get; set; } = [];
    public string AttestationFormat { get; set; } = string.Empty;
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public long? LastUsedAt { get; set; }
}
```

## Vue Responsibilities

Vue should own:

- passkey list UI
- search / filter / favorite / soft delete metadata
- informational states
- explainers and onboarding
- local metadata caching

Vue should **not**:

- generate the passkey private key
- store private key material
- export passkeys through CSV / TXT
- include passkeys inside current WebDAV / LAN password snapshot sync in phase 1

## UI Plan

### Phase 1

Add passkeys as a new list mode or a dedicated section under `List`.

Recommended UI:

- `All passwords`
- `Favorites`
- `Recently deleted`
- `Passkeys`

Passkey item card fields:

- site / service (`rpId`)
- username
- display name
- last used
- Windows-managed badge
- favorite

Detail actions:

- copy username
- open relying party domain
- delete metadata + delete Windows-native passkey
- refresh metadata

### Settings

Add a `Passkeys (Windows)` section in Settings with:

- supported / unavailable state
- provider onboarding state
- "refresh passkey metadata"
- short explanation that passkeys stay Windows-managed

## Security Boundaries

### Allowed in Vue / IndexedDB

- `credentialId`
- `rpId`
- `username`
- `displayName`
- `userHandle`
- timestamps
- favorite / deleted flags
- native linkage IDs

### Not Allowed in Vue / IndexedDB

- passkey private keys
- raw native key handles that allow use without Windows mediation
- exportable secret material that bypasses Windows Hello / provider policy

## Sync Policy

Phase 1 recommendation:

- passwords continue to use current vault sync
- passkeys remain `Windows-local only`
- WebDAV and LAN sync ignore passkeys

Reason:

- passkeys are tied to the Windows-native provider model
- mixing them into the existing encrypted snapshot flow would blur the security boundary
- cross-device passkey sync requires a much stronger design than current password sync

The UI should explicitly show:

- `Stored on this Windows device`
- `Not included in vault snapshot sync`

## Migration Strategy

No destructive migration is required.

Steps:

1. add `passkeyRecords` store and version bump the IndexedDB schema
2. leave existing password stores unchanged
3. first launch after upgrade creates an empty passkey store
4. populate it only after the Windows host reports real passkey metadata

## Failure Handling

If Windows passkey APIs are unavailable:

- hide passkey creation flow
- keep passkey list read-only or hidden
- show a clear Windows-only capability message

If metadata and native provider state drift apart:

- mark entries as `unlinked`
- offer `refresh metadata`
- allow metadata cleanup

## Implementation Phases

### Phase 0: Non-invasive preparation

- add metadata model
- add IndexedDB store
- add bridge contracts
- add Windows capability detection

### Phase 1: Windows metadata management

- list passkeys
- refresh metadata
- delete passkeys
- favorite / search / soft delete metadata

### Phase 2: Create / use passkeys

- create passkey through Windows-native flow
- use passkey through Windows-native flow
- reflect `lastUsedAt` and metadata updates in Vue

### Phase 3: UX polish

- onboarding
- settings integration
- badges and explanations
- recovery / drift repair UI

## Recommended First Deliverable

The safest first delivery is:

- Windows capability detection
- passkey metadata store
- passkey settings page
- metadata listing
- delete / refresh actions

Do **not** start with cross-device sync or web export.

## Official References

- [Plugin passkey manager support](https://learn.microsoft.com/en-us/windows/apps/develop/security/third-party)
- [Support for passkeys in Windows](https://learn.microsoft.com/en-us/windows/security/identity-protection/passkeys/)
- [Web authentication API overview on Windows](https://learn.microsoft.com/en-us/windows/apps/develop/security/reference)

