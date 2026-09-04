# Phase 5 Quality and Maintenance Plan

| | |
|---|---|
| **Status** | Complete locally; hosted release-candidate validation deferred to Phase 6 |
| **Created** | 2026-09-04 |
| **Related plan** | [Open-source readiness](20260902_open-source-readiness.md) |

This plan implements Phase 5 of the open-source readiness work at a scale appropriate for a
small, single-maintainer project. The goal is to make important quality and maintenance work
visible and repeatable without creating an automation system that costs more to maintain than
the risks it addresses.

Phase 5 records a trustworthy baseline, fixes or tracks the most consequential gaps, and leaves
clear evidence for the final release-candidate review. It does not require enterprise reporting,
an exhaustive browser matrix, or recurring process without a demonstrated need.

## Lean-scope decisions

| ID | Decision | Starting point |
|---|---|---|
| P5-01 | Coverage | Record one Core and CLI baseline from the existing Cobertura reports. Keep uploading the reports, but add no threshold or external service in this phase. |
| P5-02 | Wasm coverage | Treat the absence of automated Wasm coverage as a documented gap. Accessibility checks cover representative user paths; broader component coverage is follow-up work. |
| P5-03 | Accessibility automation | Run an axe check against one editor route and one catalog route in a single browser. Do not add a cross-browser or operating-system matrix. |
| P5-04 | Accessibility follow-up | Log keyboard and screen-reader limitations found during review, then defer comprehensive remediation and the full NVDA pass to a dedicated accessibility plan. |
| P5-05 | Unfinished markers | Fix or issue-track validation and visible error-state markers. Classify the rest rather than requiring every architectural note to be resolved. |
| P5-06 | Dependency licenses | Perform one release-focused review of production dependencies and bundled assets. Add notices that are actually required; defer an automated license gate. |
| P5-07 | Maintenance | Keep one short cadence table with a confirmed owner. Do not add bots that automatically close inactive research issues. |
| P5-08 | Decision records | Add four short architecture decision records. Each should capture context, the decision, consequences, and authoritative references without duplicating policy documents. |

## Work plan

### 1. Record the coverage baseline

Phase 1 already collects Cobertura reports for `Caf.Midden.Core.Tests` and
`Caf.Midden.Cli.Tests`. Use a successful `develop` run, or reproduce the same Release commands
locally, to record:

- line and branch coverage for Core;
- line and branch coverage for CLI;
- the commit and date measured; and
- the explicit limitation that Wasm has no automated coverage suite.

Add the numbers to the acceptance record in this plan. Keep the existing coverage artifact in
CI so later reviews can compare results manually. Do not establish a minimum percentage from one
measurement, and do not add an external coverage service. A threshold may be proposed later if
several stable measurements show that it would detect meaningful regressions rather than reward
low-value tests.

### 2. Check representative web accessibility

Add a small browser accessibility harness using Playwright and axe. Publish or serve the Blazor
WebAssembly application as the harness expects, and use server readiness and browser conditions
rather than arbitrary delays.

Automate one editor route and one catalog route:

- `/editor/dataset`, covering form controls, validation surfaces, and editor navigation; and
- `/catalog/datasets`, covering catalog navigation, search/filter controls, and repeated results.

Run the check in one Chromium job. Fail on unreviewed serious or critical axe violations and
upload the axe result when the job fails. If an existing violation cannot reasonably be fixed in
Phase 5, open an issue that records its impact and place any temporary exclusion next to the test
with the issue reference.

Document keyboard and screen-reader limitations discovered during exploratory review. Keep the
automated baseline as a regression check, then address focus management, composite-control
keyboard behavior, semantic defects, contrast, and a full NVDA pass together in the
[web accessibility remediation plan](20260904_web-accessibility-remediation-plan.md). Phase 5
does not claim accessibility conformance.

### 3. Audit unfinished markers

Search maintained source, documentation, configuration, and bundled example data for `TODO`,
`FIXME`, `HACK`, and `XXX`. Classify each result as:

- resolved in Phase 5;
- represented by a bounded GitHub issue;
- an intentional explanatory note rewritten without an unfinished marker;
- third-party text that should remain unchanged; or
- metadata database content outside the repository-quality audit.

Prioritize the file-import paths that currently catch exceptions without a visible error state
and the quick-edit path that identifies missing validation. Either fix those behaviors with
focused tests or open issues that describe the user impact and expected result. Review TODO text
inside the bundled `catalog.json` as metadata database content and exclude it from source-quality
findings. Dependency-injection notes and legacy-schema questions may remain as issue-backed
follow-up work.

Record the command or search used, the number of maintained-project matches, and the resulting
issue or pull-request links. Phase 5 does not require unrelated refactoring merely to remove a
marker.

### 4. Review licenses in shipped dependencies

Generate a versioned inventory of direct and transitive production dependencies for the CLI and
Wasm projects. Review the inventory against what is actually shipped:

- self-contained CLI archives, including the .NET runtime and native components;
- NuGet libraries used by Core and CLI;
- Wasm UI and JavaScript dependencies included in published output; and
- vendored fonts, icons, images, and other assets, including Open Iconic.

Record package or component name, resolved version, license identifier or source, artifact in
which it ships, and any attribution or notice requirement. Test-only packages and GitHub Actions
may be listed separately but are not part of the release-artifact notice requirement.

Update `NOTICE.md` or add a concise third-party notice document only where the review finds a
requirement. Ensure required notice files are included in the corresponding release archive.
Retain the reviewed inventory as Phase 5 evidence. Do not add a new CI license scanner or fail
builds on license metadata in this phase.

### 5. Define a small maintenance cadence

Add a maintenance section to `GOVERNANCE.md` with the confirmed maintainer and these starting
intervals:

| Activity | Interval |
|---|---|
| Dependency updates and vulnerability alerts | Monthly and when GitHub raises an alert |
| Issue, discussion, and pull-request triage | Monthly |
| Release planning, documentation, and license review | Before each release |
| Repository access, CODEOWNERS, private reporting, and security settings | Annually and whenever maintainer roles change |

The cadence is a reminder, not an SLA. Evidence may be a dated issue, pull request, release
checklist, or documented review. Do not enable automatic stale-issue closure; an old research
request may still be valid even when maintainer capacity is limited.

### 6. Complete repository metadata

Review GitHub's repository settings and record the final values in the acceptance record:

- retain or update the existing description and topics;
- set the homepage only when there is a confirmed maintained deployment, otherwise record it as
   intentionally blank;
- retain Discussions as the documented support channel;
- upload a legible social preview image;
- confirm private vulnerability reporting is enabled and test the reporting link; and
- add funding metadata only if a real, approved destination exists; otherwise record it as not
  applicable.

These settings are not fully represented in Git, so record the reviewer and date. After the
Phase 6 promotion, verify that GitHub recognizes the repository's current Apache-2.0 licensing
rather than metadata from the old default-branch contents.

### 7. Record four key decisions

Create `docs/architecture/decisions/` with a brief index and four numbered records:

1. Apache-2.0 with the federal public-domain notice and preservation of earlier CC0 grants.
2. One shared product version for Core, CLI, and Wasm.
3. Explicit opt-in live cloud integration tests outside the deterministic default suite.
4. GitHub Releases with four self-contained CLI runtime archives as the initial distribution.

Each record should fit on roughly one page and contain status, date, context, decision,
consequences, and links to the authoritative license, versioning, testing, or release documents.
The licensing record must state whether agency or legal review is pending or complete and record
the review date.

## Quality review implementation record

### Coverage baseline

Coverage was measured on 2026-09-04 at commit
`0d0590d1302c6cac083c2c61a6c1550e8460f122` using the same Release configuration and Coverlet
collector as CI.

| Project under test | Tests passed | Line coverage | Branch coverage | Covered/valid lines | Covered/valid branches |
|---|---:|---:|---:|---:|---:|
| Core | 151 | 78.48% | 75.04% | 1,131 / 1,441 | 376 / 501 |
| CLI | 59 | 40.81% | 39.29% | 1,146 / 2,808 | 413 / 1,051 |

Wasm has no automated line-coverage suite. No threshold or external reporting service was added.
CI continues to retain the raw Cobertura reports for manual comparison. After this work merges,
rerun coverage on the resulting `develop` commit and confirm that these baseline values remain
achievable; update the reference commit if the merged result differs materially.

### Automated accessibility

The root `package.json` pins Playwright and the Playwright axe integration. The accessibility
test starts the Blazor application through Playwright's deterministic web-server readiness,
runs Chromium against `/editor/dataset` and `/catalog/datasets`, and attaches the full axe result.
The CI workflow installs Chromium and uploads Playwright diagnostics when the job fails.

The first review fixed app-owned defects that were inexpensive and low risk: the missing document
language, the unnamed configuration link and metadata upload, decorative shared-layout and
catalog icons, and icon-only catalog actions. The remaining serious or critical findings are a
reviewed maximum-count baseline:

| Route | Rule | Maximum affected nodes | Follow-up area |
|---|---|---:|---|
| Dataset editor | `aria-required-attr` | 2 | AntDesign select semantics |
| Dataset editor | `color-contrast` | 7 | Navigation, status, and editor colors |
| Dataset editor | `label` | 4 | AntDesign select and Markdown editor controls |
| Dataset editor | `role-img-alt` | 14 | Route-specific decorative icons |
| Dataset catalog | `aria-required-attr` | 3 | AntDesign search and select semantics |
| Dataset catalog | `aria-valid-attr-value` | 1 | AntDesign search semantics |
| Dataset catalog | `button-name` | 1 | AntDesign-generated paging control |
| Dataset catalog | `color-contrast` | 29 | Catalog metadata links and controls |
| Dataset catalog | `label` | 3 | Search and select controls |
| Dataset catalog | `role-img-alt` | 1 | AntDesign-generated paging icon |

Each route waits for its representative application content rather than scanning the loading
shell. The test compares normalized axe target signatures and their multiplicities, so replacing
a reviewed violation with a different target under the same rule fails even when the total node
count is unchanged. A reduction passes without requiring baseline churn. The baseline is not a
claim that the listed violations conform.

Exploratory keyboard review found that focus does not reliably enter interactive AntDesign
popovers and that removing or clearing multi-select values is not consistently keyboard
operable. These findings and the axe clusters are logged as `A11Y-01` through `A11Y-05` in the
[web accessibility remediation plan](20260904_web-accessibility-remediation-plan.md). That plan
owns comprehensive fixes and the eventual keyboard/NVDA verification.

### Unfinished-marker audit

The final audit used:

```powershell
git grep -n -I -i -E 'TODO|FIXME|HACK|XXX' -- '*.cs' '*.razor' '*.js' '*.ts' '*.md' '*.yml' '*.yaml' '*.json'
```

It returned 13 textual matches: six metadata database values, one third-party prose match, and
six implementation-plan terms or examples. No unfinished marker remains in maintained C#,
Razor, or JavaScript source. Phase 5:

- added visible failure messages for metadata and project imports;
- validates staged variable quick edits before mutating editor state;
- reports malformed GeoJSON preview failures without breaking the metadata view; and
- removed stale dependency-injection and retired-schema question comments.

Six matches are values in the metadata database at `Caf.Midden.Wasm/wwwroot/catalog.json` and are
excluded from this repository-quality audit. The remaining matches are third-party Open Iconic
prose or historical/current implementation-plan terms and examples; they are not unfinished
product work.

## Maintenance review implementation record

### Dependency licenses and notices

Production NuGet packages, the self-contained .NET runtime, and self-hosted browser assets were
reviewed on 2026-09-04. The artifact-specific versions, copyright attributions, and license
expressions are recorded in
[`docs/maintenance/dependency-license-inventory.md`](../maintenance/dependency-license-inventory.md).
No production dependency with a source-redistribution obligation was found.

`THIRD-PARTY-NOTICES.md` records the applicable MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, and
Open Iconic terms. The release workflow now copies that file together with `LICENSE.md`,
`NOTICE.md`, and the exact RID-specific .NET runtime license and third-party notice into every CLI
archive. The Wasm publish also includes `LICENSE.md`, `NOTICE.md`, and
`THIRD-PARTY-NOTICES.md` under `wwwroot/legal`; its Leaflet, Leaflet-Geoman Free, and Leaflet.heat
dependencies are pinned, self-hosted, and distributed with their authoritative license files. A
local Windows self-contained publish rehearsal resolved runtime version 10.0.11 from
the generated dependency manifest and verified all five files beside the executable. That
RID-specific runtime-pack version is independent of the 10.0.10 .NET NuGet reference versions
listed in the dependency inventory.

No continuous license scanner was added. Re-review the inventory before a release when production
package versions, runtime targets, or vendored browser assets change.

### Maintenance cadence

`GOVERNANCE.md` assigns the lightweight monthly, release-time, annual, and role-change reviews to
the current maintainer. The cadence is explicitly a reminder rather than an SLA, and inactive
research issues are not closed automatically.

### Architecture decision records

The [architecture decision index](../architecture/decisions/README.md) records the licensing,
shared product version, opt-in live-test policy, and initial GitHub Release distribution choices.
Each record links to the authoritative policy or runbook that owns operational details. The
licensing record documents agency or legal best-guess approval on 2026-09-04.

### Repository metadata review

Public repository metadata was reviewed on 2026-09-04 through the GitHub repository API and
public repository pages.

| Setting | Observed or selected value | Result |
|---|---|---|
| Description | Research metadata catalog and editor for common academic workflows | Existing value retained |
| Homepage | Blank | Intentionally left blank; no maintained project website is being designated |
| Topics | `academic`, `data`, `data-catalog`, `data-management`, `data-science`, `metadata`, `research`, `research-data-management` | Existing focused set retained |
| Social preview | Maintainer-selected image | Uploaded in repository settings on 2026-09-04 |
| Discussions | Enabled | Existing support channel retained |
| Private vulnerability reporting | Enabled | Confirmed by the maintainer on 2026-09-04 |
| Funding | No approved destination | Not applicable; do not add `FUNDING.yml` |
| Detected license | CC0-1.0 from current `main` | Expected until Phase 6; verify Apache-2.0 detection after promotion |

The homepage decision, social preview, and private vulnerability reporting are complete. Do not
treat the old detected license as a Phase 5 defect because `main` intentionally remains unchanged
until Phase 6.

## Delivery sequence

Keep the work reviewable in three pull requests where practical:

1. **Quality review:** coverage baseline, accessibility automation and issue log, and unfinished
   marker classification or fixes.
2. **Maintenance review:** dependency-license inventory and notices, governance cadence, and
   repository metadata verification.
3. **Decision record:** ADR index and the four short records.

The pull requests may be combined if the changes remain easy for the sole maintainer to review,
but each workstream must retain its own acceptance evidence. All changes target `develop`; none
are promoted separately to `main`.

## Acceptance record

| Phase item | Evidence required | Result |
|---|---|---|
| 36 - Coverage expectations | Core and CLI line/branch baseline, commit, date, and Wasm limitation recorded; no arbitrary threshold added | Complete; baseline recorded 2026-09-04 |
| 37 - Web accessibility | Axe runs on the selected editor and catalog routes; observed limitations are logged for comprehensive follow-up | Complete for Phase 5; regression baseline passes and `A11Y-01` through `A11Y-05` are deferred to the linked remediation plan |
| 38 - Unfinished markers | Maintained-project matches are classified; validation and visible error-state markers are fixed or logged | Complete; maintained source markers resolved or classified, with metadata database values excluded |
| 39 - Dependency licenses | Shipped production dependencies and assets are reviewed; required notices are documented and packaged | Complete; reviewed inventory and archive notices added 2026-09-04 |
| 40 - Maintenance cadence | Confirmed owner and intervals appear in governance documentation | Complete; cadence assigned in `GOVERNANCE.md` |
| 41 - Repository metadata | Description, homepage, topics, social preview, Discussions, vulnerability reporting, and funding decision are reviewed and dated | Complete; private vulnerability reporting confirmed enabled 2026-09-04 |
| 42 - Key decisions | Four short ADRs and an index are committed and linked to authoritative documents | Complete; ADR-0001 through ADR-0004 added 2026-09-04 |

## Phase exit gate

Phase 5 is complete when:

- the existing Core and CLI coverage reports have a dated baseline and no unsupported threshold;
- axe passes on one representative editor route and one catalog route;
- observed accessibility limitations are logged with a bounded comprehensive remediation plan;
- actionable unfinished markers are fixed, classified, or logged with an owner requirement;
- shipped dependency licenses have been reviewed and required notices accompany release output;
- governance names the maintainer and the small recurring cadence;
- GitHub repository metadata and private vulnerability reporting have been checked; and
- the four architecture decisions are recorded.

Hosted validation of the exact candidate commit against the normal CI and release-artifact
workflows is deferred to the Phase 6 freeze gate. Phase 5 supplies local evidence for that
cumulative promotion; it does not introduce an independent merge or tag on `main`.

## Deferred until justified

The following are explicitly outside this phase unless implementation reveals a concrete need:

- coverage percentage gates or an external coverage service;
- automated Wasm line coverage;
- cross-browser or multi-operating-system accessibility matrices;
- automated screen-reader testing;
- comprehensive keyboard, focus, screen-reader, semantic, and contrast remediation described in
   the linked accessibility plan;
- a continuous dependency-license enforcement service;
- automated stale-issue closure; and
- additional recurring governance meetings or reports.