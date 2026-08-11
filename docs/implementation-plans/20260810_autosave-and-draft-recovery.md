# Implementation Plan: Autosave & Draft Recovery

## Goal
Prevent researchers from losing in-progress edits in `MetadataEditor`, `ProjectEditor`, and `ConfigurationEditor` due to accidental reloads, tab closures, or crashes, by autosaving drafts to browser `localStorage` and offering to restore them on return.

## Design Decisions (confirmed)
1. Persistence mechanism: browser `localStorage` via JS interop (no third-party package; `Blazored.LocalStorage` considered but avoided).
2. Restore behavior: **prompt** the user ("Resume unsaved draft from [time]?" Yes/No) rather than silently restoring.
3. Draft storage is per-editor (separate keys for Metadata, Project, Configuration) so editing one doesn't affect another's cached draft.
4. Autosave triggers (combination):
   - **A. Field/interaction change** — any meaningful edit (form field blur via `EditContext.OnFieldChanged`, plus explicit calls from list/drag/button handlers not covered by the form's `EditContext`) starts/resets a short debounce timer (~300-500ms) that persists to `localStorage`.
   - **C. Periodic fallback** — a periodic timer (~20s) also persists the current snapshot, as a safety net for any interaction not wired into (A).
   - **E. Navigate-away/tab-close flush** — `beforeunload` and `visibilitychange` (hidden) JS listeners synchronously call back into .NET (Blazor WASM supports synchronous JS interop) to flush the latest snapshot for all active editors.
5. Autosave is independent of validation — drafts persist regardless of whether the form currently validates.

## Key Risks / Refinements Identified During Design Review
- **Not all mutations flow through `EditContext.OnFieldChanged`.** Contact/Variable add-delete-drag-reorder, Geometry add/remove/edit, and tag list operations mutate collections directly via button/`@ondrop` handlers. These must explicitly call `NotifyChanged()` on the autosave registration, not rely solely on the periodic fallback.
- **Draft identity confusion.** `MetadataEditor` and `ProjectEditor` are reused to edit *existing* catalog items, not just new ones. A single fixed storage key per editor type risks offering to restore a stale draft belonging to a different dataset/project. The draft envelope must carry an identity fingerprint (e.g., Zone/Name/Project for Metadata, Name for Project) so restoration is only offered when it matches the currently loaded item (or the editor is in a "new/empty" state).
- **Explicit reset must clear the cached draft.** Clicking "New" (existing Popconfirm) or successfully uploading/loading a file must also remove the corresponding localStorage draft immediately, so a discarded draft can't resurface after a later reload.
- **Silent autosave erodes user trust.** Must add a visible, unobtrusive status indicator (e.g., "Saving…" / "All changes saved · 2 min ago") so users have confidence their work is protected.
- **Stale/incompatible drafts across app versions.** Wrap deserialization in try/catch and include a schema/version tag in the draft envelope; silently discard non-matching drafts instead of surfacing an error.
- **Storage quota.** Wrap `setItem` calls in try/catch for quota-exceeded errors; fail silently (console log) rather than throwing into the render pipeline.
- **Clear draft on successful explicit save/download**, since it's no longer "unsaved" work.

## Components to Add/Change

### 1. `wwwroot/js/autosaveInterop.js` (new)
- `getItem(key)`, `setItem(key, json)`, `removeItem(key)` — thin wrappers around `window.localStorage`.
- `registerUnloadFlush()` — attaches `beforeunload` and `visibilitychange` listeners that synchronously call a static `[JSInvokable]` .NET method (e.g. `AutosaveService.FlushAll`) to force a final write of all active registrations.
- Reference the script from `wwwroot/index.html`.

### 2. `Services/AutosaveService.cs` (new, registered `Scoped` in `Program.cs`)
- Wraps `IJSInProcessRuntime` for synchronous localStorage access (safe in Blazor WASM).
- `RegisterAutosave(string key, Func<DraftEnvelope> getSnapshot, TimeSpan debounce, TimeSpan periodic) -> IAutosaveRegistration`
  - `IAutosaveRegistration` exposes `NotifyChanged()` and `Dispose()`.
  - Internally: a debounce `Timer` (resets on `NotifyChanged`) and a periodic `Timer`, both writing via `SaveDraft`.
- `TryGetDraft(string key) -> DraftEnvelope?` (try/catch + schema version check).
- `RemoveDraft(string key)`.
- Maintains a registry of active registrations so `[JSInvokable] static Task FlushAll()` can iterate and force-save each on unload.
- Tracks a "prompted this session" set per key so the restore prompt isn't repeated on every navigation within the same app session.

### 3. `DraftEnvelope<T>` (new, in `Services/` or `Models/`)
```csharp
public sealed class DraftEnvelope<T>
{
	public int SchemaVersion { get; set; }
	public DateTime SavedAtUtc { get; set; }
	public string? IdentityFingerprint { get; set; } // e.g. "zone|name|project"
	public T? Payload { get; set; }
}
```

### 4. Editor wiring (`MetadataEditor`, `ProjectEditor`, `ConfigurationEditor`)
- Inject `AutosaveService`.
- `OnInitialized`: register autosave with a snapshot function and identity fingerprint; check for an existing draft not yet prompted this session and matching (or compatible with) the currently loaded item → show `ConfirmService` prompt with relative "saved X ago" text.
  - Confirm → deserialize payload, apply via `State.SetXxxEdit(...)`.
  - Decline → `RemoveDraft(key)`.
- Call `NotifyChanged()` from:
  - `EditContext.OnFieldChanged` (already present for Metadata; add for Project/Configuration).
  - All non-form mutation handlers (contact/variable/tag add-delete-drag, geometry add/remove/edit).
- `NewMetadata`/`NewProjectEdit`/`NewConfigurationEdit` and successful file upload/load handlers: also call `RemoveDraft(key)`.
- On successful explicit Save/Download: call `RemoveDraft(key)`.
- `Dispose()`: dispose the autosave registration (stops timers).
- Add a small UI status indicator (e.g., text near the New/Upload buttons) reflecting last-saved state, driven by an event/callback from `AutosaveService`.

## Autosave Keys (proposed)
- `midden.draft.metadata.v1`
- `midden.draft.project.v1`
- `midden.draft.configuration.v1`

## Out of Scope (for this plan)
- Cross-tab conflict resolution (two tabs editing the same draft concurrently) — noted as a known limitation, not solved here.
- Validation-related UX changes (see companion plan: `validation-improvements.md`).

## Rollout Steps
1. Add JS interop file + script reference.
2. Add `DraftEnvelope<T>` and `AutosaveService` + DI registration.
3. Wire `ConfigurationEditor` first (simplest: single global config, no identity-fingerprint concern).
4. Wire `ProjectEditor` (add identity fingerprint by Name).
5. Wire `MetadataEditor` (most complex: identity fingerprint by Zone/Name/Project, plus non-form mutation handlers).
6. Add "Saving…" / "All changes saved" status indicator to each editor.
7. Manual test pass: edit → reload → confirm restore prompt; decline → confirm draft cleared; New/Upload → confirm draft cleared; close tab mid-edit → reopen → confirm restore offered.
