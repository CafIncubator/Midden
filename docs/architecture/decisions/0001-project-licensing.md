# ADR-0001: Project licensing and federal works

- **Status:** Accepted
- **Date:** 2026-09-04
- **Agency or legal review:** Approved 2026-09-04

## Context

Midden includes work prepared by United States Government employees as part of their official
duties, contributions that may carry copyright or patent rights, and third-party components with
their own licenses. Earlier Midden versions were distributed under the CC0 1.0 Universal public
domain dedication.

A current distribution needs clear terms for rights that can be licensed, including external
contributions and applicable patent rights, without implying that the United States Government
claims copyright in works excluded from domestic copyright protection by 17 U.S.C. 105. Changing
the current license also cannot withdraw permissions or waivers already granted under CC0.

## Decision

Midden is distributed under the Apache License 2.0 to the extent copyright or patent rights apply
and are licensable. The accompanying notice explains the status of United States Government works
and preserves the effect of earlier CC0 grants. Third-party materials remain governed by their own
licenses and notices.

Agency or legal review approved this licensing approach on 2026-09-04.

## Consequences

- Distributions include the Apache license and the Midden legal notice.
- Previously distributed CC0 permissions are not revoked or narrowed.
- Contributions are submitted under the Apache 2.0 terms by default, and recipients receive the
  applicable copyright and patent grants.
- Release preparation must preserve applicable third-party licenses and notices.
- If future agency or legal review requires a different approach, a superseding decision and
  matching repository changes are required before the affected release.

## Authoritative references

- [Apache License 2.0](../../../LICENSE.md)
- [Midden legal notice](../../../NOTICE.md)
- [Third-party notices](../../../THIRD-PARTY-NOTICES.md)
- [Release process](../../../RELEASING.md)