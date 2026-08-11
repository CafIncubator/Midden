# Implementation Plan: Validation Improvements (Deferred)

> Status: Deferred — revisit after autosave/draft-recovery is implemented.

## Goal
Bring `ProjectEditor` and `ConfigurationEditor` up to the same validation standard as `MetadataEditor`, and improve how validation errors are surfaced across all three editors so non-technical researchers get clear, actionable feedback before downloading/saving.

## Current State
- `MetadataEditor`: has `FormValidationRule`s on several fields (Zone, Name, Project, Description) via `EditorFormItemWithHelpPopup`, but no comprehensive validation on Contacts, Variables, Tags, or Spatial Extent, and no gating on whatever finalizes/downloads the metadata.
- `ProjectEditor`: `AntDesign.Form` has **no validation rules** at all — Name/Status/Description can be saved/downloaded empty.
- `ConfigurationEditor`: `AntDesign.Form` has **no validation rules** — `OrganizationName`, `ToolName`, `CatalogPath` can be blank; `Geometries` (Name/GeoJson) has no validation at all.

## Design Decisions (confirmed)
1. Add full validation parity across all three editors, and block the Download/Save action when the form is invalid.

## Refinements Identified During Design Review
- **Disabled-button pattern over click-then-error.** Bind the Download/Save button's `Disabled` state to live form validity (tracked via `EditContext.OnFieldChanged` re-evaluating `EditContext.Validate()`), with a tooltip explaining why it's disabled, rather than allowing a click that then shows an error message. This is friendlier for non-technical users.
- **Error visibility across tabs.** `MetadataEditor` uses `Tabs`; a single toast/message doesn't indicate which tab holds the invalid field. Add a badge/indicator on the tab header when it contains an invalid field, so users don't have to hunt across tabs.
- **Autosave independence.** Validation must not block autosave — drafts save regardless of current validity; only the explicit Save/Download action enforces validation.
- **Non-`FormItem` fields need manual validation.** `Geometries` (Name/GeoJson pairs) and dynamic list editors (`StringListEditor`) aren't wrapped in AntDesign `FormItem`/`FormValidationRule` today; these need explicit validation logic (e.g., required Name, GeoJson must parse) surfaced consistently with the rest of the form (not just a generic message toast).

## Components to Change

### `ProjectEditor.razor` / `.razor.cs`
- Add `@ref="form"` to the `AntDesign.Form`.
- Add `FormValidationRule`s: `Name` (Required, Min 2), `ProjectStatus` (Required).
- Track live validity; disable Download button when invalid (with tooltip).
- `SaveProject`: call `await form.Validate()` as a final guard before serializing.

### `ConfigurationEditor.razor` / `.razor.cs`
- Add `@ref="form"` to the `AntDesign.Form`.
- Add `FormValidationRule`s: `OrganizationName`, `ToolName`, `CatalogPath` (Required).
- Add manual validation for `Geometries`: each entry requires a non-empty `Name` and a parseable `GeoJson` value; surface inline (e.g., red border/help text per row) rather than only a toast.
- Track live validity; disable Download button when invalid (with tooltip).
- `SaveConfiguration`: call `await form.Validate()` (plus manual geometry check) before serializing.

### `MetadataEditor.razor` / `.razor.cs`
- Extend validation to Contacts (at least one contact recommended — confirm with product owner whether required or advisory), Variables, and Spatial Extent (valid GeoJSON).
- Add tab-header badges reflecting per-tab validation state.
- Gate whatever finalizes/downloads metadata behind `form.Validate()`.

## Open Questions (to resolve when this plan is picked back up)
- Should Contacts be strictly required, or just strongly recommended (warning, not blocking)?
- Should partially-filled optional sections (e.g., Spatial Extent) block save if malformed, or only block if the user attempted to fill them in incorrectly?
- Exact wording/placement of tab-level error badges.

## Out of Scope (for this plan)
- Autosave/draft-recovery behavior (see companion plan: `autosave-and-draft-recovery.md`), already decided to be independent of validation state.
