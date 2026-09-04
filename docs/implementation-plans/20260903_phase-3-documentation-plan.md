# Phase 3 Documentation Plan

| | |
|---|---|
| **Status** | Complete |
| **Created** | 2026-09-03 |
| **Related plan** | [Open-source readiness](20260902_open-source-readiness.md) |

This review sheet records the proposed shape of Phase 3 and the decisions that should survive
after everyone forgets why a particular paragraph exists. Approve or revise the decisions below,
then complete the validation record before marking Phase 3 complete.

The intended reader is a researcher, data manager, occasional operator, or new contributor. The
documentation should be technically accurate without sounding as though it was raised by a
committee in a windowless room.

## Approved decisions

| ID | Decision | Recommended starting point | Approval or replacement |
|---|---|---|---|
| P3-01 | Documentation tone | Prefer plain language, concrete examples, and restrained humor. Keep commands, warnings, security boundaries, and recovery steps unambiguous; jokes may carry attention but never carry required meaning. Preserve “Researcher sciences, creates a dataset” as intentional wordplay. | Approved |
| P3-02 | Source of truth | Keep revision-specific setup, architecture, deployment, configuration, support, and contribution guidance in the repository. Use the Wiki only for supplementary tutorials or screenshots and link back to repository guidance. | Approved |
| P3-03 | README entry paths | Route readers through four explicit paths: Use Midden, Run the CLI, Deploy your own, and Develop Midden. Keep the README orienting rather than duplicating complete guides. | Approved and implemented |
| P3-04 | CLI documentation | Keep `docs/usage-guides/cli-usage.md` authoritative for CLI commands, credentials, automation, and CLI troubleshooting. Link to its sections from other documents rather than maintaining competing instructions. | Approved and implemented |
| P3-05 | Architecture scope | Maintain one approachable architecture overview covering Core, CLI, Wasm, data flow, metadata/configuration versions, crawler abstractions, validation, catalog generation, and credential boundaries. Add narrower documents only when the overview becomes difficult to navigate. | Approved and implemented |
| P3-06 | Deployment support | Document a host-neutral static publishing process plus adaptations for GitHub Pages, Netlify, and Azure Static Web Apps. Treat other hosts as supported when they provide the required static assets, base path, MIME types, HTTPS, and SPA fallback. | Approved and implemented |
| P3-07 | Deployment configuration | Keep `app-config.json` and `catalog.json` as replaceable deployment inputs. A maintainer may deploy the default configuration, edit it at `/editor/app-configuration`, download the replacement, and redeploy it. Prefer a relative `catalogPath`; allow a full HTTPS URL when the catalog host supplies the required CORS response. Do not publish CLI configuration, credentials, token caches, or secret stores. | Approved and implemented; absolute URL covered by contract test |
| P3-08 | Schema examples | Store minimal, valid, non-sensitive JSON examples under `docs/examples`. Examples must parse and match runtime models; longer organization-specific vocabularies remain deployment data, not universal defaults. | Approved and implemented |
| P3-09 | Application schema versions | Make `v0.2` the current application-configuration format, accept `v0.1` as a documented legacy input during a migration window, and reject unknown versions clearly. Update the default file through an explicit migration after reader and validator tests exist. | Approved and implemented |
| P3-10 | Azure routing | Use Azure's current `staticwebapp.config.json` with `navigationFallback` and remove deprecated `routes.json`. Ensure the current file reaches published output and require a direct-route refresh smoke test with each Azure deployment. | Approved and implemented; publish artifact verified |
| P3-11 | Troubleshooting links | Provide one repository troubleshooting entry point linked from the README. Link command-line symptoms to the CLI guide and deployment symptoms to the deployment guide. Add URLs to runtime errors only after a stable public documentation URL exists. | Approved and implemented; runtime links deferred until URLs are stable |
| P3-12 | Documentation accessibility | Require descriptive alt text for meaningful images, logical heading order, headed tables, descriptive link text, labeled code fences, and complete text instructions alongside screenshots. Treat Wasm axe, keyboard, and screen-reader testing as Phase 5 application work. | Approved and implemented |

## Implemented document set

| Document | Purpose | Phase 3 items |
|---|---|---|
| [`README.md`](../../README.md) | Orient users and route them to maintained guidance | 21, 22, 26, 27 |
| [`docs/architecture/overview.md`](../architecture/overview.md) | Explain component, data, validation, crawler, version, and credential boundaries | 23 |
| [`docs/usage-guides/deployment.md`](../usage-guides/deployment.md) | Publish, configure, route, update, verify, troubleshoot, and roll back the Wasm site | 24, 26 |
| [`docs/usage-guides/configuration.md`](../usage-guides/configuration.md) | Distinguish CLI and app configuration and document the application fields | 25 |
| [`docs/usage-guides/troubleshooting.md`](../usage-guides/troubleshooting.md) | Route operational symptoms to focused recovery instructions and support | 26 |
| [`docs/examples/configuration.local.example.json`](../examples/configuration.local.example.json) | Safe, runnable local CLI starting point | 25 |
| [`docs/examples/app-config.example.json`](../examples/app-config.example.json) | Safe, minimal application-configuration starting point | 25 |

The existing [`docs/usage-guides/cli-usage.md`](../usage-guides/cli-usage.md) remains canonical and
is updated only when review finds an actual CLI documentation gap.

## P3-09 background

Application configuration has accumulated three ideas that currently look more coordinated than
they are:

- Core contains application-configuration model namespaces for `v0_1_0alpha4`, `v0_1`, and
  `v0_2`.
- Wasm's `ConfigurationReaderHttp` always deserializes `app-config.json` directly into the
  `v0_2.Configuration` type.
- The reader and `ConfigurationValidator` do not use `schemaVersion` to select a model or reject an
  unsupported value.
- Before Phase 3 completion, the checked-in `app-config.json` said `v0.1` while containing
  `projectStatuses` and `variableTypes`, properties represented by the `v0_2` model.

Before the approved policy was implemented, `schemaVersion` was a label rather than an enforced
compatibility contract. A file labeled `v0.1`, `v0.2`, or `vBanana` was offered to the same
`v0_2` deserializer. Known properties could load, missing lists received their model defaults,
and unknown properties were ignored. That was forgiving, but it could not tell an operator
whether a file was intentionally migrated or merely happened to deserialize.

The decision is therefore not about changing a string alone. It establishes:

1. Which value newly downloaded configurations should declare.
2. Whether old `v0.1` files remain accepted and for how long.
3. Whether unknown or future versions fail clearly instead of loading partially.
4. What migration changes an existing deployment must make.

The approved policy makes `v0.2` current, continues reading `v0.1` during a documented transition,
and rejects unknown values with a useful message. The shared parser enforces that policy for HTTP
loading, CLI validation, and configuration-editor uploads. Parser and validator tests cover both
supported versions, missing and unknown versions, and the documented example. The deployed
default now declares `v0.2`. The version has graduated from decorative JSON jewelry to a promise.

## Implementation sequence

The repository checks in sections 1, 2, 4, and 5 form the documentation acceptance work. The
live-host exercises in section 3 are the deployment runbook: run them when creating or changing a
host, because this repository cannot prove the behavior of an Azure, Netlify, or GitHub Pages
environment it does not control.

### 1. Technical review

- Compare the architecture overview with the current Core, CLI, and Wasm code paths.
- Confirm the crawler list, catalog output behavior, validation ownership, and credential boundary.
- Keep tests for every application-configuration version claimed as supported.
- Verify direct-route refreshes when deploying the P3-10 configuration to a new Azure host.

### 2. Example validation

- Parse every JSON example.
- Deserialize examples through the production configuration readers.
- Run the application configuration validator and resolve errors; review warnings deliberately.
- Run a local CLI crawl from a temporary version of the local example.
- Scan examples and published output for credentials and private endpoints.

### 3. Deployment-time walkthroughs

- Publish Release output from a clean checkout with the pinned SDK.
- Test a root-path deployment and at least one subpath deployment.
- Test direct navigation and refresh on `/editor/dataset`, `/catalog`, and a detail route.
- Replace `catalog.json` without rebuilding and verify that the new catalog loads.
- Test rollback of the complete site and catalog-only rollback.
- Record host-specific deviations in the deployment guide rather than in private notes alone.

### 4. Editorial and accessibility review

- Ask one reader unfamiliar with the implementation to choose the correct README path for a
  user, CLI operator, deployer, and contributor task.
- Read commands independently of surrounding screenshots and confirm no action depends on visual
  interpretation alone.
- Review every image alternative for what the image contributes, not merely what file type it is.
- Check heading hierarchy, table headers, link wording, code-block languages, line length, and
  terminology.
- Keep humor that helps the reader breathe; remove humor that obscures a requirement, error, or
  recovery action.

### 5. Link and clean-checkout validation

- Check every repository-relative link and image target.
- Check external links and record transient or access-controlled failures for manual review.
- For each release candidate, follow the README from a clean checkout through restore, build,
  test, Wasm publish, local CLI setup, and configuration.
- Verify generated output and local configuration remain untracked.

## Acceptance record

| Phase item | Evidence required | Result |
|---|---|---|
| 21 - README entry paths | Four paths are visible and each reaches maintained guidance | Complete; maintainer approved 2026-09-03 |
| 22 - README defects | Intentional wordplay recorded; global-search image confirmed; product names, images, and links checked | Complete; maintainer approved 2026-09-03 |
| 23 - Architecture overview | Core, CLI, Wasm, data flow, versions, crawlers, validation, catalog, and credentials reviewed against code | Complete; technical and maintainer review completed 2026-09-03 |
| 24 - Deployment | Maintained root and subpath procedures, catalog update, cache behavior, host routing, verification, and rollback are documented | Complete; fresh Release publish and Azure routing artifact verified 2026-09-03 |
| 25 - Configuration schemas | Safe examples parse, deserialize, validate, and document compatibility expectations | Complete; production parser and validator tests pass 2026-09-03 |
| 26 - Support and troubleshooting | README reaches CLI and deployment troubleshooting and public/private support channels | Complete; local and external links checked 2026-09-03 |
| 27 - Documentation accessibility | Manual image, heading, table, link, code block, and screenshot-independence audit complete | Complete; maintainer review and structural audit completed 2026-09-03 |

## Phase exit gate

Phase 3 is complete when:

- a first-time reader can choose the right README path without repository knowledge;
- a contributor can explain the Core, CLI, Wasm, data, and credential boundaries;
- the documented configuration examples pass production readers and validators;
- a maintainer can publish to the supported host patterns and update `catalog.json` using only
  repository documentation;
- the published artifact contains the supported-host routing configuration, and the deployment
  guide requires direct-route refresh checks at root and beneath a base path;
- support and troubleshooting are reachable from the README;
- internal links, images, and external links have been checked;
- the manual documentation accessibility audit is recorded above; and
- P3-09 and P3-10 have approved outcomes rather than optimistic punctuation.

## Review outcome

Maintainer review was completed on 2026-09-03, and P3-01 through P3-12 are approved. Phase 3 is
complete. The final validation covered the full solution test suite, Release publication, current
Azure routing output, configuration examples and versions, local documentation targets, external
links, image alternatives, heading structure, tables, and code fences. Application-level axe,
keyboard, and screen-reader testing remains Phase 5 work rather than a Phase 3 documentation gate.
Live-host route, catalog-replacement, and rollback smoke tests were not performed from this
workspace; the deployment guide requires them whenever a host is created or changed.