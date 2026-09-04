# ADR-0003: Explicit opt-in live cloud tests

- **Status:** Accepted
- **Date:** 2026-09-04

## Context

Midden crawls local storage and external Azure and Google services. Real-provider tests can detect
authentication, permission, and API-integration problems that fakes cannot. They also require
credentials and stable remote fixtures, can consume service quotas, and can fail because of an
external service or account rather than a product regression.

The ordinary contributor and continuous-integration experience must remain deterministic and must
not require access to research infrastructure. Credentials and tokens must never enter source
control or ordinary test artifacts.

## Decision

Live cloud integration tests are compiled with the solution but marked explicit. The default test
suite uses local fixtures and provider gateways and does not run live tests. A maintainer opts in
to live tests with the documented command and supplies credentials through ignored local files or
a separately protected CI environment.

## Consequences

- Contributors can build and test the repository without cloud accounts or secrets.
- Default CI failures are attributable to deterministic repository inputs.
- Provider integration confidence requires a deliberate live-test run before relevant releases or
  after significant provider changes.
- A protected live-test job must treat missing configuration as a failure rather than a successful
  skip.
- Live-test results must be interpreted separately from deterministic unit and contract tests.

## Authoritative references

- [CLI live integration tests](../../../Caf.Midden.Cli.LiveTests/README.md)
- [Security policy](../../../SECURITY.md)
- [Architecture overview](../overview.md)