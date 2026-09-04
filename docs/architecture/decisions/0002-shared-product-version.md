# ADR-0002: Shared product version

- **Status:** Accepted
- **Date:** 2026-09-04

## Context

Midden has three deliverables: the shared Core library, the CLI crawler and collator, and the Wasm
editor and catalog. They are deployed differently, but they implement one research-data workflow
and share metadata models, validation rules, and configuration behavior. Independent component
versions would make compatibility harder for researchers and maintainers to understand.

Metadata files and application or CLI configuration also have format versions. Those versions
describe data compatibility and should not change merely because the product is released.

## Decision

Core, CLI, and Wasm release together under one Semantic Versioning product version inherited from
the repository's central `VersionPrefix`. Metadata and configuration format versions remain
independent and change only when their respective formats change.

## Consequences

- A release has one version for users, documentation, source tags, and supported behavior.
- A breaking change in any product component may require a product major-version increment.
- Components are tested and promoted as one release candidate even when only one changed.
- Format compatibility can remain stable across product releases and evolve on its own schedule.
- Publishing components independently would require a superseding decision and an explicit
  compatibility policy.

## Authoritative references

- [Versioning policy](../../../VERSIONING.md)
- [Release process](../../../RELEASING.md)
- [Architecture overview](../overview.md)