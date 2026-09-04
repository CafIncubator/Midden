# ADR-0004: GitHub Release CLI archives

- **Status:** Accepted
- **Date:** 2026-09-04

## Context

The CLI is intended for researchers and data stewards on Windows, Linux, and macOS. Requiring a
matching .NET installation would add setup and support work for non-technical users. At the same
time, maintaining package-manager feeds, installers, containers, and signing infrastructure would
create disproportionate release overhead for the initial single-maintainer project.

Core and Wasm share the product version, but they do not yet need independent package publication.
Wasm deployments can continue to be built from source for their hosting environment.

## Decision

The initial distribution channel is GitHub Releases. Each stable release publishes self-contained
CLI archives for `win-x64`, `linux-x64`, `osx-x64`, and `osx-arm64`, together with checksums and
GitHub build attestations. Stable tags are immutable and identify commits on `main`.

## Consequences

- Users can run the CLI without installing .NET separately.
- Four platform-specific archives increase release size but provide a simpler installation path.
- The release workflow must build and smoke-test each target on a native runner.
- Archives must include the project and runtime legal notices used by that build.
- NuGet, Homebrew, WinGet, containers, installers, and code signing remain outside the initial
  distribution scope and can be added later when demand justifies their maintenance cost.

## Authoritative references

- [Release process](../../../RELEASING.md)
- [Versioning policy](../../../VERSIONING.md)
- [Release workflow](../../../.github/workflows/release.yml)