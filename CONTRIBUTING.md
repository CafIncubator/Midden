# Contributing to Midden

Thank you for helping improve Midden. Contributions from researchers, data managers,
developers, technical writers, and other users are welcome.

Midden uses the [Apache License, Version 2.0](LICENSE.md) for copyrightable material and
contributions. Under Section 5 of that license, unless you explicitly state otherwise, a
contribution intentionally submitted for inclusion in Midden is provided under Apache-2.0
without additional terms. Midden does not require Developer Certificate of Origin (DCO) commit
sign-off or a contributor license agreement (CLA).

Portions prepared by United States Government employees as part of their official duties are
not subject to copyright protection in the United States under 17 U.S.C. 105. Federal funding
or public-access status alone does not give a contribution that status. See
[NOTICE.md](NOTICE.md) for details, including the treatment of earlier CC0 releases.

Only submit work that you created or are authorized to contribute under these terms. Clearly
identify third-party material and its license in the pull request.

## Before you start

- Search [open issues](https://github.com/CafIncubator/Midden/issues) before opening a new one.
- Use [GitHub Discussions](https://github.com/CafIncubator/Midden/discussions) for usage help,
  deployment questions, and ideas that are not yet ready to become defined work.
- Do not report security vulnerabilities in a public issue. Follow [SECURITY.md](SECURITY.md).
- For a substantial change, open or comment on an issue before investing significant effort.
  This gives maintainers and affected users a chance to confirm the scope.

## Prerequisites

- Git
- The .NET SDK selected by [global.json](global.json)
- An editor with C# support, such as Visual Studio, Visual Studio Code, or JetBrains Rider

Cloud accounts and provider credentials are not required for the default build and test suite.

## Set up the repository

1. Fork the repository on GitHub.
2. Clone your fork.
3. Add the upstream repository as a remote.
4. Create a focused branch from the latest `develop` branch.

```powershell
git clone https://github.com/YOUR-ACCOUNT/Midden.git
cd Midden
git remote add upstream https://github.com/CafIncubator/Midden.git
git fetch upstream
git switch --create my-change upstream/develop
```

Restore, build, and run the deterministic test suite from the repository root:

```powershell
dotnet restore Caf.Midden.slnx
dotnet build Caf.Midden.slnx --configuration Release --no-restore
dotnet test Caf.Midden.slnx --configuration Release --no-build
```

These commands must succeed without private credentials or interactive prompts.

## Live cloud integration tests

The tests in `Caf.Midden.Cli.LiveTests` connect to real Azure or Google resources. They are
compiled with the solution but are explicit, so the default test command does not run them.
Most contributions do not need to run these tests.

If a change affects a cloud integration, follow the credential and fixture instructions in
[Caf.Midden.Cli.LiveTests/README.md](Caf.Midden.Cli.LiveTests/README.md), then opt in with:

```powershell
dotnet test Caf.Midden.Cli.LiveTests --configuration Release -- xUnit.Explicit=only
```

Never commit credentials, OAuth tokens, service-account keys, or private research data.

## Choose an issue

Issues labeled [`good first issue`](https://github.com/CafIncubator/Midden/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22)
are intended to have bounded scope and clear acceptance criteria. Issues labeled
[`help wanted`](https://github.com/CafIncubator/Midden/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22)
are ready for community input but may require more project knowledge.

Comment on an issue before starting work so that scope, ownership, and current context are
clear. Maintainers may suggest a smaller first contribution when an issue spans several areas.

## Coding and documentation conventions

- Follow the style of the surrounding code and keep changes focused on the stated problem.
- Preserve nullable-reference-type correctness and do not introduce new build warnings.
- Add or update focused tests when behavior changes.
- Fix concurrency and lifecycle problems deterministically; do not use arbitrary delays as a
  race-condition workaround.
- Prefer transparent behavior and actionable messages because many CLI users are researchers,
  not software specialists.
- `Mippen` is intentional domain terminology and must not be changed to `Midden`.
- Update repository documentation when setup, configuration, commands, or user-visible behavior
  changes.
- Use relative links between repository documents and meaningful alt text for images.
- Do not include secrets, access tokens, identifying research data, or local machine paths in
  tests, screenshots, logs, examples, or documentation.

There is no repository-wide automated formatter configuration at present. Avoid unrelated
formatting changes and let the existing file style guide the change.

## Pull requests

Open ordinary pull requests against `develop`, not `main`. The `main` branch represents
releasable work and receives changes through the documented release process.

A pull request should:

- Explain the problem or context and the chosen approach.
- Link the issue it addresses, when one exists.
- Include focused tests or explain why tests do not apply.
- Report the commands or manual checks used for validation.
- Update affected documentation.
- Include screenshots for visible web-interface changes.
- Avoid unrelated refactoring or generated build output.

CI restores, builds, and tests the solution on Linux and Windows. It also collects coverage and
runs security checks. A maintainer reviews the change after required checks pass. Address review
comments with additional commits; maintainers may squash commits when merging.

The pull request author must confirm that they created the contribution or are authorized to
submit it under Apache-2.0. This confirmation applies to all commits and materials in the pull
request. No `Signed-off-by` commit trailer is required.

Midden's components share the version policy in [VERSIONING.md](VERSIONING.md). Contributors do
not increment development versions in feature branches; GitHub Actions assigns the
`dev.RUN_NUMBER` suffix. Release maintainers make stable version and changelog changes through the process in
[RELEASING.md](RELEASING.md).

## Community expectations

Participation in this project is governed by [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). Support
and reporting routes are summarized in [SUPPORT.md](SUPPORT.md).