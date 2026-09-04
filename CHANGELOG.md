# Changelog

Notable changes to Midden are recorded here. This project follows
[Semantic Versioning](VERSIONING.md).

## [Unreleased]

### Added

- Deterministic Linux and Windows CI, test coverage collection, dependency review, and CodeQL
  analysis.
- CLI validation, configuration setup, provider login, encrypted secret storage, and expanded
  local and cloud crawler support.
- Editor configuration, local draft caching, improved catalog search and insights, spatial
  coverage visualization, and richer data dictionary import and export.
- Contributor, governance, security, support, architecture, deployment, configuration, and
  troubleshooting documentation.

### Changed

- Upgraded the solution to .NET 10 and hardened the CLI's validation, error handling, and
  researcher-facing diagnostics.
- Standardized current configuration examples on schema version `v0.2`; `v0.1` remains a
  documented legacy schema.
- Centralized the product version and replaced manually incremented development versions with
  CI-generated prerelease identifiers.

### Fixed

- Corrected project paging, combined zone filtering and search, malformed crawler input handling,
  static asset fingerprinting, and relative deployment paths.
- Made the default test suite independent of live cloud credentials and normalized corrupt secret
  store failures.

### Migration notes

- Review `docs/examples/app-config.example.json` and `docs/usage-guides/configuration.md` before
  replacing a legacy `v0.1` application configuration with `v0.2`.
- Review `docs/usage-guides/cli-usage.md` before replacing an older CLI; command validation and
  credential handling have changed substantially since `v0.4.0`.

## [0.4.0] - 2025-02-05

### Added

- Dataset CSV downloads, tag browsing, provenance visualization, and project and dataset card
  improvements.

### Changed

- Upgraded the application through .NET 8 and .NET 9 and refreshed dependencies.

### Fixed

- Corrected variable search, relative links, and crawler handling of malformed files.

## [0.3.0] - 2023-09-21

### Added

- Inline variable editing, variable type metadata and filtering, and additional editor guidance.

### Changed

- Improved responsive layouts, catalog navigation, metadata views, and browser cache behavior.

## [0.2.0] - 2022-05-19

### Added

- Project crawling from Google Drive, Markdown metadata editing, project-detail editing, and
  modification-date sorting.

### Changed

- Expanded project and dataset metadata displays and improved empty-state and filtering behavior.

## [0.1.0] - 2021-10-01

### Added

- Initial beta release of the Midden metadata editor, catalog viewer, Core models, and catalog
  generation CLI.

[Unreleased]: https://github.com/CafIncubator/Midden/compare/v0.4.0...develop
[0.4.0]: https://github.com/CafIncubator/Midden/releases/tag/v0.4.0
[0.3.0]: https://github.com/CafIncubator/Midden/releases/tag/v0.3.0
[0.2.0]: https://github.com/CafIncubator/Midden/releases/tag/v0.2.0
[0.1.0]: https://github.com/CafIncubator/Midden/releases/tag/v0.1.0