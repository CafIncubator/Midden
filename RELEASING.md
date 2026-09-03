# Release process

This runbook is for Midden release maintainers. Midden releases the CLI, Core library, and Wasm
application under one version, but the initial automated distribution contains the four supported
self-contained CLI archives.

## One-time repository setup

Complete these settings before pushing the first stable release tag:

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

## Prerequisites

- Phases required for the planned release are complete on `develop`.
- Required checks pass on the exact candidate commit.
- The release maintainer can merge to `main`, create protected `v*` tags, approve the `release`
  environment, and publish GitHub Releases.
- A repository ruleset prevents force updates and deletion of release tags.
- The GitHub `release` environment requires maintainer approval.
- `Directory.Build.props` contains the intended stable `VersionPrefix`.

NuGet, Homebrew, WinGet, containers, and code signing are not part of the initial release process.

## Prepare the candidate

1. Freeze release-related changes on `develop` and record its commit identifier.
2. Confirm that `CHANGELOG.md` accurately describes all changes under `Unreleased`, including
   breaking changes, security fixes, and migration steps.
3. Confirm that the version follows `VERSIONING.md` and update `VersionPrefix` in
   `Directory.Build.props` if needed.
4. Run the clean-checkout validation commands:

   ```powershell
   dotnet restore Caf.Midden.slnx
   dotnet build Caf.Midden.slnx --configuration Release --no-restore --warnaserror
   dotnet test Caf.Midden.slnx --configuration Release --no-build
   ```

5. Confirm the `Release artifacts` workflow ran for the candidate on `develop`. Relevant pushes
   to `develop` create an attested `VERSION-dev.RUN_NUMBER` candidate without publishing a GitHub
   Release. After the workflow reaches the default branch in Phase 6, maintainers can also start
   this validation with its manual trigger.
6. Confirm that all four native smoke-test jobs pass and retain the workflow URL in the release
   issue or pull request.
7. Replace the changelog's release content under `Unreleased` with a dated `[VERSION]` section,
   add a new empty `Unreleased` section, and update its comparison links.
8. Commit the final version and changelog changes to `develop` and rerun required checks.

## Promote and publish

1. Follow the cumulative `develop`-to-`main` promotion process in the open-source readiness plan.
2. Verify that required `main` checks pass and that `main` protection is active.
3. Confirm that the stable version printed by this command matches the planned tag:

   ```powershell
   dotnet run --project Caf.Midden.Cli/Caf.Midden.Cli.csproj --configuration Release `
     -p:Version=VERSION -- --version
   ```

4. Create and push an annotated `vVERSION` tag on the verified `main` commit:

   ```powershell
   git tag -a vVERSION COMMIT -m "Midden vVERSION"
   git push origin vVERSION
   ```

5. Approve the protected `release` environment only after the workflow verifies the tag, version,
   branch ancestry, archives, checksums, smoke tests, and attestations.
6. Confirm that the GitHub Release notes match the dated changelog entry and that these assets are
   present:

   - `MiddenCli-VERSION-win-x64.zip`
   - `MiddenCli-VERSION-linux-x64.tar.gz`
   - `MiddenCli-VERSION-osx-x64.tar.gz`
   - `MiddenCli-VERSION-osx-arm64.tar.gz`
   - `SHA256SUMS`

## Post-release checks

1. Download every release asset into an empty directory and verify the checksum manifest:

   ```powershell
   Get-FileHash MiddenCli-VERSION-* -Algorithm SHA256
   Get-Content SHA256SUMS
   ```

2. Verify each archive's GitHub build provenance:

   ```powershell
   gh attestation verify MiddenCli-VERSION-win-x64.zip -R CafIncubator/Midden
   ```

   Repeat for the Linux and macOS archives.

3. Extract and run `--help` and `--version` on locally available platforms. The release workflow's
   native jobs are authoritative for platforms maintainers do not own.
4. Check the README release, changelog, support, and security links.
5. Record the release URL, tag commit, workflow run, and checksum verification in the release
   issue.
6. Merge release-only `main` changes back into `develop`. Select and commit the next
   `VersionPrefix` before accepting work intended for a later release.

## Failed or defective releases

- If infrastructure fails without changing inputs, rerun the failed workflow against the same tag.
- If validation reveals a code or artifact defect, do not move, delete, or reuse the tag. Correct
  the defect through the normal branch process, increment the patch version, and create a new tag.
- If users may be affected, describe the superseded version in the new release notes and use the
  private security process when appropriate.
- If publication partially succeeds, do not upload locally rebuilt replacement files. Rerun only
  when the workflow inputs and resulting artifact digests remain identical; otherwise publish a
  corrected patch release.