# Release process

This is the canonical runbook for Midden release maintainers. It covers feature development,
internal beta and release-candidate stages, stable promotion, and production hotfixes. The
examples prepare `v1.1.0`; substitute the version selected for the actual release.

Midden releases the CLI, Core library, and Wasm application under one version. The automated
distribution contains four supported self-contained CLI archives.

## One-time repository setup

Complete these settings before pushing a release tag:

1. In **Settings > Environments**, create an environment named `release`.
2. Require approval from a release maintainer. For a single-maintainer project, leave self-review
   available so an authorized maintainer can approve a workflow they started.
3. Restrict the environment's deployment tags to `v*`.
4. In **Settings > Rules > Rulesets**, create an active tag ruleset targeting `v*`. Restrict tag
   creation to release maintainers and block tag updates and deletions.
5. Confirm that Actions may create attestations and write GitHub Release contents through the
   repository's `GITHUB_TOKEN`. The workflow grants these permissions only to the jobs that need
   them.

Record the environment and ruleset review in the release issue. Workflow files cannot enforce
these repository settings by themselves.

## Branch and version model

- `develop` is the integration branch. Feature and prerelease-fix branches start from `develop`
  and return to `develop` through pull requests.
- `main` contains stable releases. It receives planned releases from `develop` and urgent fixes
  from production-hotfix branches.
- `VersionPrefix` in `Directory.Build.props` is the next stable version, such as `1.1.0`.
- `PrereleaseChannel` is set manually to `beta` during ordinary development and to `rc` at
   feature freeze. Local builds append `.local`; GitHub Actions appends the commit SHA.
- Beta and RC builds are internal development identifiers. They are not tagged or published as
   GitHub Releases. The `v` prefix is used only for Git tags, not the version displayed by the app.
- Stable tags use `v1.1.0`, identify commits on `main`, and are immutable. Stable release notes
   come from the matching dated section in `CHANGELOG.md`.

An ordinary release does not need a separate release branch. Freeze `develop` while preparing the
stable candidate. Use a temporary release branch only when development must continue while an
older release line is stabilized; document the merge and synchronization plan in the release
issue before creating it.

NuGet, Homebrew, WinGet, containers, and code signing are not part of this process.

## Prerequisites

- Planned work is complete on `develop`, and required checks pass on the exact candidate commit.
- Agency or legal approval recorded in
  `docs/architecture/decisions/0001-project-licensing.md` remains current.
- The release maintainer can merge to `main`, create protected `v*` tags, approve the `release`
  environment, and publish GitHub Releases.
- A repository ruleset prevents force updates and deletion of release tags.
- The GitHub `release` environment requires maintainer approval.
- `Directory.Build.props` contains the intended `VersionPrefix` and development stage.

## Start the release line

After the preceding stable release, select the next SemVer version. For a backward-compatible
feature release after `1.0.0`, update these properties on `develop` and retain an empty
`Unreleased` section at the top of `CHANGELOG.md`:

```xml
<VersionPrefix>1.1.0</VersionPrefix>
<PrereleaseChannel>beta</PrereleaseChannel>
```

Create each feature branch from current `develop` and open its pull request back to `develop`:

```powershell
git switch develop
git pull --ff-only origin develop
git switch --create feature/short-description
```

Feature branches inherit local version `1.1.0-beta.local`; CI assigns
`1.1.0-beta.COMMIT_SHA`. Contributors do not edit the version for individual features. The SHA
identifies the source commit consistently in the upstream repository and forks. Add user-visible
changes and migration guidance to the changelog's `Unreleased` section as the work merges.

## Beta development

Every successful CI or `Release artifacts` run on `develop` produces an identifiable beta build,
such as `1.1.0-beta.0123456789abcdef0123456789abcdef01234567`. Record the commit when sharing one
for testing. Do not create a beta tag or GitHub Release.

For another hosting workflow to expose the same pattern in the Wasm application, pass that
provider's stable build number to MSBuild:

```powershell
dotnet publish Caf.Midden.Wasm/Caf.Midden.Wasm.csproj --configuration Release `
   -p:CommitIdentifier=$env:GITHUB_SHA
```

Without `CommitIdentifier`, the application displays `1.1.0-beta.local`. Fixes found during beta
testing branch from `develop`, merge back into `develop`, and update `Unreleased`. They are release
fixes, not production hotfixes.

## Release-candidate development

Move to RC when the intended scope and user-visible behavior are complete. Freeze new features;
accept only release-blocking fixes, documentation corrections, and required dependency changes.
Change only the channel on `develop`:

```xml
<VersionPrefix>1.1.0</VersionPrefix>
<PrereleaseChannel>rc</PrereleaseChannel>
```

Local builds now report `1.1.0-rc.local`, and CI builds report
`1.1.0-rc.COMMIT_SHA`. Run the clean-checkout validation commands:

```powershell
dotnet restore Caf.Midden.slnx
dotnet build Caf.Midden.slnx --configuration Release --no-restore --warnaserror
dotnet test Caf.Midden.slnx --configuration Release --no-build
```

The channel change triggers `Release artifacts`. Confirm all four native smoke-test jobs pass and
inspect one archive for `LICENSE.md`,
`NOTICE.md`, `THIRD-PARTY-NOTICES.md`, `DOTNET-LICENSE.txt`, and
`DOTNET-THIRD-PARTY-NOTICES.txt`. Re-review the dependency license inventory when package versions,
runtime targets, or browser assets changed.

Record the workflow run and exact commit chosen as the candidate. Handle an RC defect through a
focused branch and pull request into `develop`; its build receives the new commit SHA automatically.
Do not create an RC tag or GitHub Release. Return the channel to `beta` only when the release scope
changes materially.

## Prepare the stable release

1. Freeze `develop` and record the candidate commit identifier.
2. Confirm required CI, security, accessibility, and release-artifact checks pass.
3. Confirm `VersionPrefix` is `1.1.0`, clear `PrereleaseChannel`, and confirm the CLI prints
   `1.1.0` when built with that release version.
4. Review `Unreleased` for breaking changes, security fixes, migration steps, and user-visible
   changes. Remove entries that did not ship.
5. Move the release content beneath `## [1.1.0] - YYYY-MM-DD` and leave a new empty
   `## [Unreleased]` section above it.
6. Update the changelog comparison links: `Unreleased` starts at `v1.1.0`, and `1.1.0` compares
   the preceding stable tag with `v1.1.0`.
7. Commit these final release changes to `develop` through a pull request and rerun all required
   checks. If anything changes afterward, rerun the relevant checks and return to the RC stage.

The stable changelog heading must exactly match `## [1.1.0] - YYYY-MM-DD`; the publication
workflow uses it as the stable GitHub Release notes.

## Promote and publish the stable release

1. Open a pull request from `develop` to `main`. Review the complete release diff, not only the
   final changelog commit.
2. Merge only after the exact `develop` candidate passes required checks.
3. Wait for required `main` checks, including the native release-artifact validation, to pass on
   the resulting commit. Confirm `main` protection is active.
4. Confirm the CLI version:

   ```powershell
   dotnet run --project Caf.Midden.Cli/Caf.Midden.Cli.csproj --configuration Release `
     -p:Version=1.1.0 -- --version
   ```

5. Create and push the stable annotated tag on that verified `main` commit:

   ```powershell
   git switch main
   git pull --ff-only origin main
   git tag -a v1.1.0 HEAD -m "Midden v1.1.0"
   git push origin v1.1.0
   ```

6. Approve the protected `release` environment only after the workflow verifies the tag, version,
   branch ancestry, archives, checksums, smoke tests, and attestations.
7. Let the workflow create the GitHub Release and attach its assets. Do not manually create the
   release or upload artifacts from an earlier run.

The release must contain:

- `MiddenCli-1.1.0-win-x64.zip`
- `MiddenCli-1.1.0-linux-x64.tar.gz`
- `MiddenCli-1.1.0-osx-x64.tar.gz`
- `MiddenCli-1.1.0-osx-arm64.tar.gz`
- `SHA256SUMS`

## Verify and synchronize

1. Confirm the GitHub Release notes match the dated changelog section.
2. Download every release asset into an empty directory and compare the reported hashes with
   `SHA256SUMS`:

   ```powershell
   Get-FileHash MiddenCli-1.1.0-* -Algorithm SHA256
   Get-Content SHA256SUMS
   ```

3. Verify each archive's GitHub build provenance, repeating for Linux and macOS:

   ```powershell
   gh attestation verify MiddenCli-1.1.0-win-x64.zip -R CafIncubator/Midden
   ```

4. Extract and run `--help` and `--version` on available platforms. The workflow's native jobs
   are authoritative for platforms maintainers do not own.
5. Confirm the five legal and third-party notice files are present in each archive.
6. Check the README release, changelog, support, and security links. Record the release URL, tag
   commit, workflow run, and verification in the release issue.
7. Merge `main` back into `develop` if the release process added anything not already present.
8. Select the next intended version on `develop`, update `VersionPrefix`, set
   `PrereleaseChannel` to `beta`, and keep subsequent work under `Unreleased` before accepting new
   features.

## Production hotfixes

A production hotfix corrects the latest stable release before the next planned release is ready.
It is different from a beta or RC fix.

1. Create `hotfix/1.1.1-short-description` from current `main`.
2. Make the smallest safe correction and add focused tests.
3. Update `VersionPrefix` to `1.1.1`, add a dated `1.1.1` changelog section, and update comparison
   links on the hotfix branch.
4. Open the pull request into `main`, obtain release-maintainer approval, and require all checks.
5. Merge, verify the resulting `main` commit, and publish `v1.1.1` through the same stable tag
   process.
6. Immediately merge `main` back into `develop` through a pull request so the fix and release
   history cannot be lost. Resolve version conflicts by retaining the next planned
   `VersionPrefix` on `develop` while retaining the `1.1.1` changelog entry.

Never merge a production hotfix only into `develop`: that leaves current users without a release.
Conversely, do not send ordinary features directly to `main` under the hotfix exception.

## Failed or defective releases

- If infrastructure fails without changing inputs, rerun the failed workflow against the same tag.
- If validation reveals a code or artifact defect, do not move, delete, or reuse the tag. Correct
  the defect through the normal branch process, increment the patch version, and create a new tag.
- If users may be affected, describe the superseded version in the new release notes and use the
  private security process when appropriate.
- If publication partially succeeds, do not upload locally rebuilt replacement files. Rerun only
  when the workflow inputs and resulting artifact digests remain identical; otherwise publish a
  corrected patch release.