# Phase 2 Community Decisions

| | |
|---|---|
| **Status** | Complete; policies approved and repository settings activated |
| **Created** | 2026-09-02 |
| **Related plan** | [Open-source readiness](20260902_open-source-readiness.md) |

This review sheet collects decisions that cannot be inferred safely from repository history.
Approve or revise each decision before the Phase 2 community files are merged.

| ID | Decision | Recommended starting point | Approval or replacement |
|---|---|---|---|
| P2-01 | Contribution license | Use Apache-2.0 for copyrightable material and contributions, with a separate 17 U.S.C. 105 notice for qualifying United States Government work. Preserve prior CC0 grants. | Approved through agency or legal review on 2026-09-04 |
| P2-02 | Contribution provenance | Rely on Apache-2.0 Section 5 and require the pull request author to confirm that they created the contribution or are authorized to submit it. Require neither DCO commit sign-off nor a CLA. | Approved |
| P2-03 | Conduct reporting | Enable private reports to repository admins for reports involving repository content. Direct reports concerning a maintainer, or reports made when no unconflicted project maintainer is available, to GitHub Support. Keep public wording role-based and record current staffing constraints internally. | Approved; private content reporting confirmed enabled on 2026-09-04 |
| P2-04 | Security reporting | Enable GitHub private vulnerability reporting. State publicly that response times depend on maintainer availability and record current staffing constraints internally. | Approved; enabled and confirmed by the maintainer on 2026-09-04 |
| P2-05 | Security response | Aim to acknowledge within seven calendar days and provide an initial assessment within fourteen calendar days. Treat these as targets rather than an SLA, and provide updates when status materially changes. | Approved |
| P2-06 | Supported versions | Support only the latest published GitHub Release, before and after `v1.0.0`. Deliver security fixes through a new release rather than backports; older and unreleased builds are unsupported. | Approved |
| P2-07 | Repository ownership | Use `@bryancarlsoncafltar` as the sole default CODEOWNER. Do not create placeholder teams or imply additional reviewers exist. | Approved |
| P2-08 | Release authority | The sole maintainer, `@bryancarlsoncafltar`, approves and publishes releases after required CI and release checks pass. | Approved |
| P2-09 | Support commitment | Provide community support through Issues and Discussions as availability and knowledge permit, without guaranteeing a response, resolution, or timeframe. Offer no private technical support. Keep explicit response targets for security reports; review conduct reports without a fixed target. | Approved |

## Review outcome

Maintainer review was completed on 2026-09-03. All decisions are approved. Agency or legal review
approved the Apache-2.0 and federal public-domain notice approach on 2026-09-04.

## Completion record

Phase 2 was completed on 2026-09-04. GitHub's repository community profile confirms that private
content reporting is enabled, and the maintainer confirmed private vulnerability reporting is
enabled. Discussions provide the public support channel. Repository labels cover type, component,
contributor readiness, and blocked work; `priority: high`, `priority: medium`, and `priority: low`
complete the priority category. No `good first issue` tasks were seeded because suitable bounded
work has not yet been identified.