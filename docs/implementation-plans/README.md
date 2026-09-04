# Implementation plans

Implementation plans capture the scope, reasoning, and acceptance evidence for bounded bodies of
work. They are not the authoritative description of current product behavior; use the
[documentation index](../README.md) to find maintained architecture and usage guidance.

## Current and deferred work

| Plan | Status | Outcome or next step |
|---|---|---|
| [Open-source readiness](20260902_open-source-readiness.md) | In progress | Phases 0-5 are complete; Phase 6 promotion remains |
| [Web accessibility remediation](20260904_web-accessibility-remediation-plan.md) | Deferred follow-up | Address the recorded keyboard, focus, semantics, contrast, and NVDA gaps |

## Completed work

| Plan | Status | Recorded outcome |
|---|---|---|
| [CLI hardening](20260805_cli-hardening.md) | Complete | Credential, failure-handling, validation, and release hardening shipped; bounded items remain deferred in the plan |
| [Autosave and draft recovery](20260810_autosave-and-draft-recovery.md) | Shipped | Browser draft persistence and visible save state shipped for the editors |
| [Validation improvements](20260810_validation-improvements.md) | Complete | Core validators, editor validation surfaces, completeness reporting, and `midden validate` shipped |
| [Home dashboard improvements](20260811_home-dashboard-improvements.md) | Shipped | Search, health indicators, work queues, and shared metric definitions shipped |
| [Phase 2 community decisions](20260902_phase-2-community-decisions.md) | Complete | Community policies and repository settings were approved and activated |
| [Phase 3 documentation](20260903_phase-3-documentation-plan.md) | Complete | Maintained usage, architecture, deployment, configuration, and troubleshooting guidance shipped |
| [Phase 5 quality and maintenance](20260904_phase-5-quality-maintenance-plan.md) | Complete locally | Quality baselines, accessibility automation, license review, maintenance cadence, and ADRs were recorded; hosted validation remains in Phase 6 |

## Lifecycle

1. Add a dated plan for work that needs explicit sequencing, tradeoffs, or acceptance evidence.
2. Keep active and deferred plans in this directory and update their status when scope changes.
3. When work ends, summarize its outcome in this register and update maintained guides, ADRs,
   changelogs, or policies that own the resulting behavior.
4. Retain a completed plan when it contains useful rationale or acceptance evidence that has no
   better durable home. Mark it historical so readers do not treat its starting state as current.
5. Delete a disposable checklist when its outcome is fully represented elsewhere and no tracked
   document links to it. Git history is a recovery mechanism, not the project history index.

At the current scale, keeping the completed plans listed above is inexpensive and preserves useful
context. If this directory becomes difficult to scan, move retained completed plans into an
`archive` subdirectory without changing this register's role.