# Phase 2 Community Decisions

| | |
|---|---|
| **Status** | Policy decisions approved; activation and legal review pending |
| **Created** | 2026-09-02 |
| **Related plan** | [Open-source readiness](20260902_open-source-readiness.md) |

This review sheet collects decisions that cannot be inferred safely from repository history.
Approve or revise each decision before the Phase 2 community files are merged.

| ID | Decision | Recommended starting point | Approval or replacement |
|---|---|---|---|
| P2-01 | Contribution license | Use Apache-2.0 for copyrightable material and contributions, with a separate 17 U.S.C. 105 notice for qualifying United States Government work. Preserve prior CC0 grants. | Provisionally approved; agency or legal review pending |
| P2-02 | Contribution provenance | Rely on Apache-2.0 Section 5 and require the pull request author to confirm that they created the contribution or are authorized to submit it. Require neither DCO commit sign-off nor a CLA. | Approved |
| P2-03 | Conduct reporting | Enable private reports to repository admins for reports involving repository content. Direct reports concerning a maintainer, or reports made when no unconflicted project maintainer is available, to GitHub Support. Keep public wording role-based and record current staffing constraints internally. | Approved; repository setting must be enabled and tested |
| P2-04 | Security reporting | Enable GitHub private vulnerability reporting. State publicly that response times depend on maintainer availability and record current staffing constraints internally. | Approved; repository setting must be enabled and tested |
| P2-05 | Security response | Aim to acknowledge within seven calendar days and provide an initial assessment within fourteen calendar days. Treat these as targets rather than an SLA, and provide updates when status materially changes. | Approved |
| P2-06 | Supported versions | Support only the latest published GitHub Release, before and after `v1.0.0`. Deliver security fixes through a new release rather than backports; older and unreleased builds are unsupported. | Approved |
| P2-07 | Repository ownership | Use `@bryancarlsoncafltar` as the sole default CODEOWNER. Do not create placeholder teams or imply additional reviewers exist. | Approved |
| P2-08 | Release authority | The sole maintainer, `@bryancarlsoncafltar`, approves and publishes releases after required CI and release checks pass. | Approved |
| P2-09 | Support commitment | Provide community support through Issues and Discussions as availability and knowledge permit, without guaranteeing a response, resolution, or timeframe. Offer no private technical support. Keep explicit response targets for security reports; review conduct reports without a fixed target. | Approved |

## Review outcome

Maintainer review was completed on 2026-09-03. P2-02 through P2-09 are approved. P2-01 uses
Apache-2.0 with a federal public-domain notice provisionally and remains subject to agency or
legal review before the final release.

## Repository setting actions

- Enable private content reporting to repository admins and test the reporter and maintainer
	views before publishing the Code of Conduct.
- Enable private vulnerability reporting and test the advisory form before publishing the
	security policy.