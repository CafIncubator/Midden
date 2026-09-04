# Implementation Plan: Validation Improvements

> Status: Complete. The rollout steps below shipped. Starting-state observations are retained as
> historical design context and may no longer describe the current implementation.

## Goal

Give researchers clear, actionable, non-punishing feedback about the quality and correctness of
the metadata, projects, and configurations they author, and make sure the artifacts they download
actually work in the catalog. Validation must be a *guide*, not a gate that hides the exit.

## Current State (revised)

- **`MetadataEditor`** — already has `@ref="form"` and `OnFieldChanged="OnFormFieldChanged"`
  (added by the autosave work). Has `FormValidationRule`s on Zone, Name, Project only.
  `SaveDataset()` still contains a literal `// TODO: Validate first!` and serializes
  unconditionally. Contacts, Variables, Tags, Geometry, and Temporal Extent are unvalidated.
- **`ProjectEditor`** — `AntDesign.Form` has no `@ref`, no validation rules. `SaveProject()`
  serializes unconditionally.
- **`ConfigurationEditor`** — `AntDesign.Form` has no `@ref`, no validation rules. `Geometries`
  (Name/GeoJson pairs) are edited outside any `FormItem` and are entirely unvalidated.
- All three editors now have an autosave status line and an editor-owned declarative
  `<Modal>` for the draft-restore prompt. That is the established pattern for editor-scoped,
  non-toast UI and should be reused for validation surfaces.

## Core Problem: two competing sources of truth

Validation rules exist in two disconnected places today, and they already disagree:

| Rule | `Caf.Midden.Core` model | Editor markup |
|---|---|---|
| `Dataset.Description` | `[Required]` | `IsRequired=false` |
| `Dataset.Zone/Project/Name` | `[Required]` | `FormValidationRule{ Required = true }` |
| `Variable.Name/Description/Units` | `[Required]` | not in the form at all |
| `Project.Name/Description` | `[Required]` | no rules |
| `Configuration.*` | no attributes | no rules |

Adding a third pile of hand-written `FormValidationRule`s makes this worse.

### Decision: a Core-level validator is the single source of truth

Introduce `Caf.Midden.Core/Services/Validation/` containing validators that take a model
(plus the active `Configuration` for vocabulary checks) and return a structured result:

```csharp
ValidationIssue { Severity, Code, Path, Message, Hint }
// Path e.g. "dataset.variables[3].units" -> maps to a tab + row in the UI
```

Editors render the issues; the CLI reuses the same validators. `FormValidationRule`s remain
only as cheap inline "this is required" affordances — they are *not* the gate.

#### Why this is required, not just tidier

`MetadataEditor` uses `<Tabs Animated>`. AntDesign renders tab panes lazily, so **`FormItem`s
on tabs the user never opened do not exist in the form's field collection** — `await form.Validate()`
returns `true` for a dataset with a blank required field on the unopened "Structure" tab. The
original plan's mechanism (`form.Validate()` plus tab badges derived from form state) is
unreliable for exactly the editor with the most fields. Model-level validation sidesteps this,
and makes tab badges trivially computable by grouping issues on `Path`.

Secondary benefit: a Core validator is plain-unit-testable next to the existing
`Caf.Midden.Core.Tests`. UI-coupled validation is not.

## Severity Model

The original open question ("are Contacts required or advisory?") is a symptom of a missing
concept. Three tiers:

| Tier | Meaning | Behavior |
|---|---|---|
| **Error** | The output would break the catalog or the file | Blocks download |
| **Warning** | Valid file, but poor-quality metadata | Confirm-and-proceed |
| **Info** | Nice to have | Feeds the completeness meter only |

### Errors (block download)

- `Dataset.Zone`, `Dataset.Name`, `Dataset.Project` present.
- `Dataset.Name` is filesystem-safe — it becomes `{Name}.midden`, and `Collate` independently
  filters unsafe paths via `IsDatasetPathSafe`, so an unsafe name means a silently missing dataset.
- `Dataset.Geometry`, if non-empty, parses as a GeoJSON **geometry** object.
- `Dataset.TemporalExtent`, if non-empty, parses as an ISO-8601 `{start}/{end}` interval.
- `Dataset.SpatialRepeats`, if present, is non-negative.
- Every `Variable` that exists has a Name and Units.
- Every `Contact` that exists has a Name (an anonymous contact entry is structurally useless).
- Configuration: `OrganizationName`, `ToolName`, `CatalogPath` present; every `Geometry` has a
  Name and a parseable GeoJson.
- Project: `Name` present (min length 2), `ProjectStatus` present.

### Warnings (confirm and proceed)

- No Description / no Contacts / no Tags / no Variables.
- Variable missing a Description.
- Contact missing or malformed email.
- Temporal extent end before start.
- **Value not present in the configured vocabulary.** Zone, Structure, ProcessingLevel,
  VariableType, QCTags, Tags, and Roles are all config-driven. A `.midden` authored at another
  organization, or produced before a config change, will legitimately contain values absent from
  the current `AppConfiguration`. Hard-failing these would be brutal. Warn, and (later) offer
  "add to configuration".
- Duplicate names within a configuration vocabulary list or geometry list.

### Info / completeness

A weighted **completeness score** ("Metadata completeness: 72% — adding variables and contacts
would help data users") rewards good behavior instead of nagging. In practice this moves metadata
quality far more than any blocking rule.

## UX Decisions (revised)

### Rejected: the disabled-Download-button pattern

The original plan chose "disable Download when invalid + tooltip". Rejected because:

- Disabled buttons are not focusable, so the tooltip explaining *why* never fires for keyboard
  or touch users — the button is simply dead.
- With six tabs, "it is disabled" conveys nothing about *where* the problem is.
- It punishes the common case of grabbing a partial `.midden` to finish later.

### Adopted: enabled button + validation summary + navigation

1. **Download stays enabled.**
2. On click, run the Core validator.
   - Blocking errors → do not download; open a **validation summary panel** (editor-owned
	 declarative component, same approach as the draft-restore modal) listing each issue as a
	 clickable row. Clicking an issue switches to the owning tab and scrolls/focuses the control.
   - Warnings only → "Download anyway?" confirmation listing the warnings.
3. A quiet **status chip beside the existing autosave status text** — "3 issues to fix" /
   "Ready to download" — so validity is ambient rather than a surprise at the end.
4. **Tab badges** with an issue count, derived by grouping `ValidationIssue.Path` by tab.
   Count + icon, never color alone.

### Timing

Validating an empty new dataset on load paints the form red before the user types anything.

- Inline field errors: display only for touched/modified fields.
- Full validation: on explicit Download, on Preview, and after a draft restore or file upload
  (so restored/loaded state is honest).
- Validation never blocks autosave; validation state is never persisted into the draft envelope.

## Dataset-specific complexity

This is the part the original plan under-specified.

1. **Variables and Contacts live outside the `EditContext`.** They are edited through
   `ModalService` modals and an inline quick-edit row (`VariableQuickEditRef`). Per-row issues
   must be surfaced as a row highlight plus an icon, using the `RowClassName` hook already used
   for contact drag state (`GetContactRowClassName`).
2. **Bulk CSV variable import.** `DataDictionaryLoaderCafCsv` replaces *all* variables at once and
   is the most likely source of mass-invalid data. Needs a post-import summary:
   "Imported 47 variables. 6 are missing Units."
3. **GeoJSON is currently only validated in JavaScript.** `GeoJsonEditorMap` calls `setGeometry`
   and gets a `bool` back — async, map-dependent, unavailable to a deterministic gate. A C#-side
   structural check is required. Note `Dataset.Geometry` is documented as the *geometry member
   only*; users will paste a full `Feature` / `FeatureCollection`, so that case gets its own
   error code and a hint to unwrap rather than a generic "invalid".
4. **`TemporalExtent` is free text** in `{start}/{end}` ISO-8601 form and is parsed nowhere.
   Validate it, and later offer a date-range picker that writes the string.
5. **Identity collisions.** Zone+Project+Name is effectively the catalog key. The editor cannot
   see the whole catalog, but the Catalog page can — a "this name already exists in this
   zone/project" warning is a later enhancement.
6. **Legacy schema round-tripping.** `MetadataParser` up-converts `v0.1.0-alpha*` to `v0.2`.
   Converted files will often fail current rules through no fault of the user. Issues raised on a
   *freshly loaded* file should read as informational ("this file predates current requirements")
   rather than as if the user just broke something.

## CLI parity

`Collate` silently `continue`s past metadata it rejects, and there is no way to check a file
before a catalog build. Once the validators live in Core, a `midden validate <path>` command is
nearly free and gives the catalog operator (and CI) a real feedback loop.

Shipped as `midden validate <paths...>`:

- Accepts files or directories; directories are searched recursively for `.midden` and
  `DESCRIPTION.md`. Overlapping arguments are de-duplicated so the summary counts stay honest.
- `--app-config` supplies the controlled vocabularies, so zone/project/tag checks match what the
  editor would say for that organization. Without it those checks are skipped rather than guessed.
- `--warnings-as-errors` for repositories that have agreed to a documentation standard.
- `--quiet` prints only files with issues.
- Exit codes distinguish the two failures CI cares about: `1` means metadata is wrong, `2` means
  the check could not run (bad path, unreadable config). `0` is clean.
- Unparseable files are reported per-file and the run continues, which is precisely the case
  `collate` hides today.

## Accessibility

- `aria-invalid` on invalid inputs; error text, not tooltip-only.
- `aria-live` announcement of the issue count when validation runs.
- Focus moves to the first error when the summary row is activated.
- Tab badges convey state with a count and an icon, not color alone.

## Rollout Steps

1. **`Caf.Midden.Core` validators** — `ValidationSeverity`, `ValidationIssue`, `ValidationResult`,
   `IValidator<T>`, `MetadataValidator`, `ProjectValidator`, `ConfigurationValidator`, the
   `GeoJsonGeometryValidator` / `TemporalExtentValidator` / `DatasetNameRules` helpers, and
   `MetadataCompletenessCalculator`. Unit tests. No UI. **(done)**
2. **`ConfigurationEditor`** — simplest model, no draft-identity concerns, exercises the
   row-level Geometries case. **(done)**
3. **`ProjectEditor`** — trivial; proves the shared summary/gate component. **(done)**
4. **Shared UI** — `ValidationSummary` component, status chip, tab badges, focus/navigation.
   **(done; shipped as `ValidationIssueList` / `ValidationStatusChip` / `ValidationGate`. Tab
   badges and focus/navigation landed with the metadata editor, which is the only tabbed editor.)**
5. **`MetadataEditor`** — tabs, variable/contact row validation, CSV import summary,
   completeness meter, Preview integration. **(done)**
6. **`midden validate` CLI command** reusing the same validators. **(done)**

## Out of Scope

- Autosave/draft-recovery behavior (see `20260810_autosave-and-draft-recovery.md`); already
  decided to be independent of validation state.
- Cross-catalog duplicate detection (noted above as a later enhancement).
- Writing vocabulary values back into the configuration from the metadata editor.
