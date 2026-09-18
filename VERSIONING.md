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

`Directory.Build.props` contains the `VersionPrefix` for the next intended release and a
`PrereleaseChannel` describing the current development stage. Project files must inherit them
unless a component has a documented reason to differ.

Development builds use a SemVer prerelease suffix:

- After a stable release, `develop` advances to the next `VersionPrefix` with channel `beta`.
- A local build uses `MAJOR.MINOR.PATCH-beta.local`.
- GitHub Actions uses `MAJOR.MINOR.PATCH-beta.COMMIT_SHA`.
- At feature freeze, the maintainer changes the channel to `rc`; local and Actions builds become
  `MAJOR.MINOR.PATCH-rc.local` and `MAJOR.MINOR.PATCH-rc.COMMIT_SHA`.
- When no channel is selected, local and Actions builds use `dev.local` and `dev.COMMIT_SHA`.
- Development versions are not tagged, published as GitHub Releases, or supported under the
  security support policy.

The commit SHA makes a development version reproducible across the upstream repository and forks.
Contributors do not edit the channel or identifier in feature branches.

## Stable releases

Stable tags use `vMAJOR.MINOR.PATCH` and must point to a commit on `main`. The release workflow
rejects a tag when its version does not exactly match `VersionPrefix`.

Release tags are immutable. A transient workflow failure may be rerun against the same commit and
tag. A correction that changes code or release content requires a new patch version and tag.

After a stable release, maintainers select the next intended version, update `VersionPrefix`, set
the channel to `beta` on `develop`, and record subsequent work under the changelog's `Unreleased`
section.

Production hotfixes branch from the latest stable commit on `main`, increment `PATCH`, and return
to `main` through a pull request. After publication, `main` is merged back into `develop`; any
version conflict keeps the next planned `VersionPrefix` on `develop`. The complete operational
sequence is maintained in [RELEASING.md](RELEASING.md).