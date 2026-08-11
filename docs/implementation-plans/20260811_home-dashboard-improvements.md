# Implementation Plan: Home Dashboard Improvements

> Status: Shipped. Rollout steps 1-7 have landed. Written after a design review of
> `Pages/Index.razor`, `Pages/Index.razor.cs`, `Services/CatalogInsightsService.cs`, the two
> `FilteredCatalog*Viewer` components, and the `Caf.Midden.Core.Models.v0_2` model surface. See
> "Post-implementation notes" at the end of this document for where the shipped code diverged
> from the original plan.

## Goal

Turn the Home page from a passive report into the primary entry point of the catalog: a place a
researcher can **find** anything in one field, **see** whether the catalog is healthy and fresh,
and **act** on the specific records that need work.

Three principles guide every decision below:

1. **A number without a link is a dead end.** Every count, bar, and list row should navigate
   somewhere that answers "which ones?".
2. **A dashboard that only reports is half a dashboard.** The highest-value content is the
   work queue — incomplete metadata, orphaned projects, validation failures.
3. **One definition per metric.** The same concept must not be computed two different ways in
   two different places.

## Current State

- `Index.razor` renders: a 6-tile KPI strip, "Recent Datasets", "Recent Projects", and a
  "Stats for Nerds" block (2 column charts, 3 lists, 1 area chart).
- `Index.razor.cs` pulls everything from a single `CatalogInsightsService.BuildSnapshot(...)`
  call, re-run on `AppStateChange.Catalog` / `AppStateChange.AppConfig`.
- `CatalogInsightsService` is registered as a singleton in `Program.cs` and returns an immutable
  `CatalogInsightsSnapshot` record. This is a good shape and the new work extends it rather
  than replacing it.
- `MetadataCompletenessCalculator` and `MetadataValidator` already exist in
  `Caf.Midden.Core.Services.Validation` and are currently used **only** by the editors.
- There is an orphaned `Pages/Insights.razor` at route `/insights` that duplicates roughly 70%
  of the Home page, is not referenced from `MainLayout.razor`, and reads the same snapshot.

### Defects found

| # | Defect | Evidence |
|---|---|---|
| 1 | `TotalProjects` and "Projects by Status" disagree | `BuildSnapshot` counts distinct `metadata.Dataset.Project` strings (9); `BuildProjectsByStatus` counts `catalog.Projects` (2) |
| 2 | Charts collapse to intrinsic width | `<div style="display:flex; justify-content:center;">` makes an `AutoFit` chart measure its content box, not the card |
| 3 | Dead code | `Index.razor` lines 183–210: `DebugMsg`, `AppConfig_StateChanged`, commented-out `OnInitialized`/`Dispose`, all superseded by the `_stateSubscription` pattern |
| 4 | Dashboard renders list chrome | The recent viewers emit a result count, a "Showing x-y of n" label, and a live Previous/Next pager on a page that shows a fixed 2 items |
| 5 | Growth chart has no readable scale | `DatasetsOverTimeConfig.XAxis.Visible = false` |
| 6 | Silent blanks while loading | Every chart sits behind `@if (…Data.Length > 0)`, so an empty or still-loading catalog renders empty bordered boxes |

### Explicitly *not* a defect

`CatalogLastUpdate` maps to `catalog.CreationDate` **by design** — it is the date the crawler last
ran, i.e. the freshness of the *collation*, not the freshness of any metadata file. The value is
correct; only the label is ambiguous. It will be relabelled, not recomputed.

## Decisions

### D1 — One definition of "project", and orphans become a first-class concept

`Dataset.Project` is a free-text string; `Project` is a separately authored record. The two
counts disagreeing is not a bug to paper over — it is a real, useful signal that datasets
reference projects nobody has documented.

Therefore:

- `TotalProjects` is redefined as **the count of `Project` records in the catalog**. This is the
  only definition consistent with the Projects page, the status chart, and the nav.
- A new `OrphanedProjects` collection is added to the snapshot: project names referenced by at
  least one `Dataset.Project` for which no `Project` record exists, each with its dataset count.
- Comparison is `OrdinalIgnoreCase` on a trimmed name — the same normalization
  `BuildDatasetsByZone` / `BuildProjectsByStatus` already use. This normalization gets factored
  into a single private helper so the three call sites cannot drift.

Orphans surface as an actionable card linking to `editor/project`, not as a silent discrepancy.

### D2 — The search bar is the page's primary control

Modeled on the search-first landing pages of enterprise catalogs (Collibra, Alation, Atlan,
DataHub, Amundsen): a single wide field, grouped and typed results, keyboard-first.

- New `Services/CatalogSearchService.cs`, registered as a singleton alongside
  `CatalogInsightsService`, returning an immutable result record.
- Searches **datasets** (name, description, tags, methods), **projects** (name, description),
  **variables** (name, description, units, tags), and **tags** (as navigable entities).
- Results grouped by entity type with per-group counts and a per-group "see all" row that
  deep-links to the existing routes (`catalog/datasets/tags/{tag}`,
  `catalog/datasets/{zone}/{project}/{name}`, `catalog/projects/{name}`,
  `catalog/variables/tags/{tag}`).
- Ranking: exact match > name prefix > name contains > tag match > description contains. Ties
  broken by name, ascending, so results are stable between keystrokes.
- Rendered with AntDesign `AutoComplete`; debounced ~200 ms via the same
  `CancellationTokenSource` pattern `FilteredCatalogMetadataViewer` already uses for filters.
- `Ctrl+K` and `/` focus the field.

All catalog data is already in memory in `StateContainer`, so this is pure in-memory LINQ with
no new I/O. Putting it in a service (rather than in the page) keeps it unit-testable in
`Caf.Midden.Core.Tests`-style isolation and lets the catalog pages adopt it later.

### D3 — Dashboard-mode parameters, not a new component

The recent-items viewers get two new `[Parameter] bool` properties, `ShowResultCount` and
`ShowPager`, both defaulting to `true`. Home passes `false`. This is deliberately the cheap
option: a bespoke `RecentItemsCard` is a larger change than the problem justifies, and the
existing cards already render the right information.

### D4 — Reuse the existing Core calculators; do not re-derive quality

"Needs attention" and "Validation health" both run over `State.Catalog.Metadatas` using
`MetadataCompletenessCalculator.Calculate` and `MetadataValidator.Validate` respectively. No new
scoring rules are introduced — the dashboard's job is to *aggregate and surface* the scores the
editor already shows, so the two can never disagree.

`MetadataValidator` is instantiated once per snapshot build, not per dataset.

### D5 — Keep "Stats for Nerds"

The heading stays. It correctly signals "supporting detail" once the actionable content is
promoted above it, and it is part of the product's voice.

### D6 — KPI tiles: link only where a destination exists

Datasets, Projects, and Variables link to their catalog pages. **Tags and Contributors do not
link** — there is no dedicated page for either, and a tile that looks clickable but is not is
worse than a plain one. If a tags or contributors page is added later, the tiles become links
then.

`Last Updated` is relabelled **"Catalog Collated"** with a tooltip explaining it is the last
crawler run (see "not a defect", above).

## Page Order

The Home page is a **landing page for data users** — people who want to find data and understand
what the catalog holds — not for data contributors. Discovery and holdings come first; the
contributor work queue is real but secondary, so it goes last.

1. `PageHeader` (organization name)
2. **Universal search** (new)
3. KPI strip (6 tiles: Catalog Collated, Projects, Datasets, Variables, Tags, Contributors)
4. Recent Datasets / Recent Projects (no counts, no pager)
5. Stats for Nerds — charts and top-N lists
6. **Needs Attention** (new) — completeness, orphaned projects, validation. Bottom of the page
   and labelled as contributor-facing, so it never competes with discovery content.

## New Insights

| Insight | Source data | Presentation |
|---|---|---|
| **Catalog completeness** | `MetadataCompletenessCalculator.Calculate` averaged over all metadata | `Progress` ring + the 5 lowest-scoring datasets, each linking to `editor/dataset` |
| **Orphaned projects** | D1 | List of referenced-but-undocumented project names with dataset counts; CTA to `editor/project` |
| **Datasets per project** | `Dataset.Project` grouped | Horizontal `Bar`, top 8 — handles long names like `CafModelingRegionalSoilConditioningIndex` far better than a vertical column |
| **Project coverage** | `Project` × dataset counts | Projects with ≥1 dataset vs. none; status split weighted by dataset count rather than raw project count |
| **Spatial coverage** | `Dataset.Geometry` (GeoJSON) | Small map of dataset extents (see "Post-implementation notes" — shipped as bounding boxes via a new `CatalogCoverageMap` component rather than reusing `GeoJsonMap.razor`) |
| **Temporal coverage** | `Dataset.TemporalExtent` (`{start}/{end}` ISO-8601) | Year-bucketed coverage strip showing which periods are covered and where the gaps are |
| **Undocumented variables** | `Variable.Description` / `Variable.Units` across all metadata | Count of variables missing a description and/or units, plus the datasets with the most affected variables, each linking to `editor/dataset` |
| **Validation health** | `MetadataValidator.Validate` over all metadata | Count of datasets with errors / with warnings / clean, linking to the offending datasets |

Temporal parsing reuses the `TemporalExtentValidator` helper from the validation work rather
than a second hand-rolled parser; unparseable extents are counted as "unknown" and never throw.

## Layout & Styling

- **Consistent containers.** Every tile — chart, list, map, progress — is wrapped in an
  `AntDesign.Card` with a `Title`. Today charts use `Card` and lists use `AntList`'s own
  `Header`, which produces two different borders and paddings side by side.
- **Inline styles move to `Index.razor.css`.** The repeated `Style="padding: 5px;"` on six
  columns and the ad-hoc `margin-right:5px; margin-bottom:5px;` on three of six KPI cards are
  replaced by `Gutter` plus scoped classes. The KPI strip becomes one `Card` with internal
  dividers rather than six loose borderless cards.
- **Chart width.** The `display:flex; justify-content:center` wrappers become `width:100%`
  (defect 2). Column chart `Height` drops from 350 to ~260 — 350px is far more than two or
  three bars need.
- **Loading and empty states.** `Skeleton` while `CatalogLoader` is running; `Empty` with a
  "Create your first dataset" CTA when the catalog is genuinely empty. Replaces the current
  silent blank cards (defect 6).
- **Growth chart axis.** `XAxis.Visible = true` with a minimal tick count, keeping `YAxis.Min = 0`.
  Enough scale to read the series without adding clutter (defect 5).
- **Top Contributors rows** stay non-navigable for the same reason as the Contributors KPI tile
  (D6) — there is no contributor page to navigate to.

## Rollout Steps

1. **`CatalogInsightsService`** — fix `TotalProjects` (D1), add `OrphanedProjects`, extract the
   shared name-normalization helper, and add the completeness, validation, datasets-per-project,
   project-coverage, and temporal-coverage aggregates to `CatalogInsightsSnapshot`. No UI.
2. **Viewer parameters** — add `ShowResultCount` / `ShowPager` to
   `FilteredCatalogMetadataViewer` and `FilteredCatalogProjectViewer` (D3).
3. **Index cleanup** — delete the dead `@code` block, fix the chart wrappers, relabel
   `Last Updated`, correct the KPI links, move inline styles into `Index.razor.css`.
4. **`CatalogSearchService` + search bar** (D2), including the keyboard shortcuts.
5. **Needs Attention section** — completeness ring, orphaned projects,
   validation health. **Stale datasets are deliberately excluded:** a dataset that has not
   changed in a year is usually *complete*, not neglected, and flagging it punishes finished
   work. Age alone is not evidence of a problem, so it is not an actionable queue item.
6. **Stats for Nerds additions** — datasets-per-project bar, project coverage, temporal
   coverage strip, spatial coverage map.
7. **Reorder** the page per "Page Order" and apply the skeleton/empty states.
8. `run_build`, then verify against the existing test suite.

## Open Question

`Pages/Insights.razor` (`/insights`) is unreachable from the nav and duplicates most of what the
Home page will now do better. Recommendation: **delete it** along with `Insights.razor.cs` once
step 6 lands. Flagged rather than assumed, since it may be linked externally.

**Status: still unresolved.** Step 6 has landed but `Pages/Insights.razor` and
`Insights.razor.cs` are still present in the workspace and still unreferenced from
`MainLayout.razor`. This should be revisited: either delete the files now, or record an explicit
decision (e.g. an external link depends on it) for why they remain.

## Out of Scope

- A dedicated Tags page or Contributors page (would unlock the two remaining KPI links).
- Full lineage graph visualization; `MetadataLineageWidget` already covers the per-dataset case
  and a catalog-wide graph is its own piece of work.
- Tag hygiene / near-duplicate tag detection.
- Trend deltas on the KPI tiles ("+3 this month") — cheap once `DatasetGrowth` is reused, but
  deferred so the KPI strip rework stays a single concern.
- Any change to how the catalog is crawled, collated, or loaded.

## Post-implementation notes

Added after reviewing the shipped code against this plan:

- **Spatial coverage** did not reuse `GeoJsonMap.razor` / `geojsonMap.js`. Instead,
  `CatalogInsightsService` collapses each dataset's geometry server-side into a
  `SpatialBoundingBox` (min/max envelope, degenerate boxes rendered as point markers), and
  `Index.razor` renders those through a new `CatalogCoverageMap` component. This keeps the
  catalog-wide map cheap in WASM (no full-geometry parsing in the browser) and is the preferred
  approach going forward; this document has been updated to match.
- **Undocumented variables** shipped as an additional Needs Attention insight
  (`UndocumentedVariableSummary` in `CatalogInsightsService`, surfaced in `Index.razor` and
  factored into `HasAttentionItems`) even though it was never called out in the original "New
  Insights" table. It has now been added above.
- **KPI strip** ships with 6 tiles (Catalog Collated, Projects, Datasets, Variables, Tags,
  Contributors), consistent with D6 ("Tags and Contributors do not link") but the original
  "Page Order" section undercounted it as 5; corrected above.
- `Pages/Insights.razor` / `Insights.razor.cs` were **not** deleted; the Open Question above is
  still outstanding.
