# Versioning policy

Midden uses [Semantic Versioning](https://semver.org/) for the product. The CLI, Core library,
and Wasm application release together with one version because users depend on their shared
models, validation rules, and configuration behavior.

Metadata and configuration schema versions describe data compatibility and do not have to match
the product version.

## Version meaning

Given a version `MAJOR.MINOR.PATCH`:

- `MAJOR` changes when an upgrade can require user action, such as an incompatible CLI, public
  Core API, configuration, deployment, or supported metadata behavior change.
- `MINOR` changes when backward-compatible capabilities are added.
- `PATCH` changes for backward-compatible fixes, documentation corrections shipped with the
  product, and security fixes that do not require an incompatible change.

Versions before `1.0.0` were official beta releases. Their interfaces and behavior were not
considered stable, but their tags and GitHub Releases remain part of the release history.

Breaking changes and required upgrade steps must be called out in `CHANGELOG.md`. Maintainers
should discuss substantial compatibility changes in an issue or discussion before implementation.

## Development versions

`Directory.Build.props` contains the single `VersionPrefix` for the next intended release. Project
files must inherit it unless a component has a documented reason to differ.

Development builds use a SemVer prerelease suffix:

- A local build uses `MAJOR.MINOR.PATCH-dev.local` so it cannot be mistaken for a release.
- GitHub Actions uses `MAJOR.MINOR.PATCH-dev.RUN_NUMBER`, where `RUN_NUMBER` is the workflow's
  monotonically increasing Actions run number.
- Development versions are not tagged, published as GitHub Releases, or supported under the
  security support policy.

The CI build number replaces the former manually incremented `dev.N` value. Contributors do not
edit a development counter when merging feature branches.

Release candidates may use `MAJOR.MINOR.PATCH-rc.N` when maintainers need externally identifiable
prerelease testing. The current release workflow accepts stable tags only; it and the release
runbook must be updated before publishing an `rc.N` tag. Ordinary `develop` builds remain
`dev.RUN_NUMBER` builds.

## Stable releases

Stable tags use `vMAJOR.MINOR.PATCH` and must point to a commit on `main`. The release workflow
rejects a tag when its version does not exactly match `VersionPrefix`.

Release tags are immutable. A transient workflow failure may be rerun against the same commit and
tag. A correction that changes code or release content requires a new patch version and tag.

After a stable release, maintainers select the next intended version, update `VersionPrefix` on
`develop`, and record subsequent work under the changelog's `Unreleased` section.