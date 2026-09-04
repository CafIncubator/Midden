# Web Accessibility Remediation Plan

| | |
|---|---|
| **Status** | Deferred follow-up |
| **Created** | 2026-09-04 |
| **Related plan** | [Phase 5 quality and maintenance](20260904_phase-5-quality-maintenance-plan.md) |

This plan records accessibility limitations found during the lean Phase 5 review. Phase 5 keeps
an automated regression baseline and fixes inexpensive app-owned defects; this follow-up will
address keyboard, focus, screen-reader, semantic, and contrast behavior as one coherent body of
work.

## Logged issues

| ID | Area | Observed limitation | Priority |
|---|---|---|---|
| A11Y-01 | Popovers | Keyboard focus does not reliably enter interactive popup content or return predictably to the trigger when the popup closes. | High |
| A11Y-02 | Multi-select controls | Removing individual selections and clearing all selections is not consistently discoverable or operable from the keyboard. | High |
| A11Y-03 | Search and select semantics | AntDesign-generated controls have missing or invalid labels and required ARIA attributes. | High |
| A11Y-04 | Buttons and icons | Some generated paging controls lack accessible names, and some route-specific decorative icons are exposed as unnamed images. | Medium |
| A11Y-05 | Color contrast | Navigation, status indicators, controls, and catalog metadata links include combinations below axe contrast thresholds. | Medium |

The reviewed axe counts and affected routes are recorded in the Phase 5 plan and enforced as
maximums by `tests/accessibility/axe-reviewed-baseline.json`.

## Recommended approach

Do not patch AntDesign internals locally. For each affected workflow, first test whether a
supported AntDesign property or a maintained package upgrade supplies correct behavior. When it
does not, prefer a small app-owned control or a simpler interaction:

- replace interactive popovers with inline expandable panels where practical;
- otherwise move focus into opened popup content, support `Escape`, and restore focus through
  deterministic component lifecycle or visibility events;
- provide explicit keyboard-reachable `Clear filters` and `Clear selected tags` actions;
- ensure search, select, and paging controls have programmatic names and valid relationships;
- mark decorative icons as presentational and name every icon-only command; and
- update shared color tokens and focused component styles to meet WCAG AA contrast.

Avoid arbitrary delays for popup focus. Focus changes must follow the component's render or
visibility lifecycle.

## Implementation sequence

1. Inventory the AntDesign controls used by editor and catalog workflows and reproduce each
   limitation with keyboard-only Playwright tests.
2. Repair popup focus and multi-select clearing, prioritizing the catalog filter workflow.
3. Repair search/select labels, ARIA relationships, paging names, and decorative icons.
4. Correct shared and route-specific contrast failures.
5. Reduce or remove each corresponding entry from the reviewed axe baseline.
6. Complete a keyboard and NVDA pass on Windows and record browser, NVDA version, routes, and
   findings.

## Exit gate

This follow-up is complete when:

- a keyboard-only user can open, operate, and close each interactive popup with focus restored;
- every selected filter can be removed and all filters can be cleared without a pointer;
- editor and catalog controls expose useful names, roles, states, and relationships;
- shared text and control colors meet the selected WCAG AA contrast criteria;
- Playwright covers the repaired keyboard workflows;
- the serious/critical reviewed axe baseline is removed or every remaining exception has a
  narrower documented reason; and
- a maintainer records a successful keyboard and NVDA review.