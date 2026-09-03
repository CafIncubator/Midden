# Implementation Plan: Open-Source Readiness

| | |
|---|---|
| **Status** | In progress (Phases 0-1 complete) |
| **Created** | 2026-09-02 |
| **Scope** | Repository governance, contributor experience, CI, releases, security, and documentation |

## Context

Midden has a solid technical base for an open-source project: a clear research use case, a
public-domain dedication, 203 automated tests, nullable reference types, CLI warnings treated
as errors, and a detailed CLI usage guide. A repository review on 2026-09-01 found that the
contributor-facing and release-management surfaces have not yet caught up with the code.

The review established the following baseline:

- `dotnet test Caf.Midden.slnx --configuration Release --no-restore` runs 203 tests, with 198
  passing and 5 failing.
- Four failures are live Google integration tests that depend on ignored credentials or cached
  tokens. One failure is an exception-contract defect in tampered secret-store handling.
- The test run also reports one nullable warning in `ProjectValidatorTests.cs`.
- `dotnet list Caf.Midden.slnx package --vulnerable --include-transitive` reports no known
  vulnerable packages from the configured NuGet sources.
- Credential fixtures and `Caf.Midden.Cli/Publish` outputs are ignored and are not tracked.
- The repository has no CI workflows, contribution guide, security policy, code of conduct,
  issue templates, pull request template, changelog, release guide, CODEOWNERS, or SDK pin.
- The first `.gitignore` entry ignores `.github`, which obstructs normal community-health and
  automation files.
- `docs/architecture` is empty, and the root README still says contribution guidance is
  "coming soon."

This plan converts those findings into a sequence that can be delivered incrementally. The
default path must remain approachable for researchers and occasional contributors, not only
experienced .NET maintainers.

## Goals

1. A new contributor can clone the repository and get a deterministic green build and test run.
2. Users and contributors can quickly find installation, support, contribution, security, and
   governance information.
3. Every pull request receives the same automated build and test checks.
4. Releases are versioned, documented, reproducible, and downloadable for every supported CLI
   platform.
5. Maintenance work such as dependency updates and vulnerability reporting has a documented
   owner and process.
6. Repository policies remain lightweight enough for a small research-focused project.

## Non-goals

- Reworking the Midden metadata schema or catalog format.
- Redesigning the web application.
- Publishing the CLI as a NuGet global tool in the first release-readiness pass.
- Introducing a contributor license agreement unless legal review specifically requires one.
- Requiring live cloud credentials for ordinary development or pull request validation.

---

## Decisions

### D1. The default test suite must be self-contained

`dotnet test Caf.Midden.slnx` must succeed without cloud accounts, cached tokens, private test
data, or interactive prompts. Tests that exercise live Azure or Google resources remain useful,
but they are explicitly marked as integration tests and run only when a maintainer opts in with
documented environment variables or credentials.

CI will initially require deterministic unit and local integration tests. Live cloud tests may
later run in a protected scheduled workflow after credentials, cost, and maintenance ownership
are agreed.

The live tests are isolated in `Caf.Midden.Cli.LiveTests`, use xUnit v3 explicit tests, and remain
compiled as part of the solution. Maintainers opt in with `xUnit.Explicit=only`; selected tests
report missing credential fixtures as skips, and a protected live-test workflow uses
`xUnit.FailSkips=true` so missing infrastructure fails that workflow.

### D2. Develop is hardened before main is changed

Required status checks are enabled on `develop` only after that branch produces a green
baseline. This avoids normalizing ignored or bypassed checks. The first workflow restores,
builds, and tests; coverage and additional security tooling are added after that workflow is
stable.

Midden uses `develop` as its long-lived integration branch and `main` as its releasable branch.
Feature branches merge into `develop`; release changes merge from `develop` into `main`;
after the initial launch, hotfixes merge into `main` and are then merged back into `develop`.
Required CI therefore runs on pull requests and pushes to both long-lived branches.

Because `develop` has diverged substantially from the current pre-release `main`, Phases 0
through 5 are implemented and validated only on `develop`. Do not cherry-pick readiness files or
partially merge application changes into `main`. The complete release candidate is promoted from
`develop` to `main` once, in Phase 6, after all earlier gates pass.

Workflow definitions may include future `main` triggers while they are developed on `develop`.
Those triggers do not change `main`. GitHub reads Dependabot configuration and scheduled
workflows from the default branch, so those capabilities are prepared earlier but cannot be
considered active until Phase 6.

### D3. Repository documentation is authoritative

The Wiki may provide tutorials and screenshots, but cloning a particular revision must also
provide the setup, contribution, architecture, security, and release information relevant to
that revision. Root documents stay short and link into `docs` for detail.

### D4. GitHub Releases are the initial distribution channel

The CLI remains a set of self-contained downloadable executables. Tagged releases produce
archives for `win-x64`, `linux-x64`, `osx-x64`, and `osx-arm64`, plus SHA-256 checksums and
release notes. Generated `Publish` directories remain untracked.

### D5. Contributor policy should be explicit but lightweight

Use issue and pull request templates, a standard code of conduct, and CODEOWNERS. Rely on
Apache-2.0 Section 5 for contribution terms and require pull request authors to confirm that they
created their contribution or are authorized to submit it. Do not require DCO commit sign-off or
a CLA; both add friction and administration that this project does not currently need.

### D6. Use Apache-2.0 with a federal public-domain notice

The provisional licensing approach, pending agency or legal review, uses the OSI-approved
Apache License 2.0 for copyrightable material and contributions. A separate notice explains that
portions prepared by United States Government employees as part of their official duties are
not subject to copyright protection in the United States under 17 U.S.C. 105.

Federal funding or public-access status alone does not make external contributions United States
Government works. Apache-2.0 supplies consistent inbound and outbound contribution terms and an
express patent grant for rights a contributor is authorized to license. Earlier CC0 grants and
dedications remain unaffected.

Record this approach in `LICENSE.md`, `NOTICE.md`, package metadata, and `CONTRIBUTING.md`.
Obtain agency or legal review before the final release and revise the documents if that review
requires a different approach.

---

## Phase 0 - Establish a green baseline (complete)

**Outcome:** a fresh clone can build and test without private infrastructure.

| # | Item | Acceptance criteria |
|---|---|---|
| 1 | Separate live cloud integration tests from the default suite | Google and Azure tests that require credentials have an xUnit trait/category and an explicit opt-in mechanism; missing credentials produce a clear skip or exclusion, not a silent passing return |
| 2 | Fix tampered secret-store exception handling | Invalid Base64 and failed authenticated decryption both surface as the documented `InvalidDataException`; focused tests pass |
| 3 | Resolve the nullable test warning | Release build and test output contains no compiler warnings caused by repository code |
| 4 | Pin the .NET SDK | A root `global.json` selects the supported .NET 10 feature band with an intentional roll-forward policy |
| 5 | Document the baseline commands | `dotnet restore`, `dotnet build --configuration Release`, and the default and opt-in test commands are documented and work from the repository root |

**Exit gate**

```powershell
dotnet restore Caf.Midden.slnx
dotnet build Caf.Midden.slnx --configuration Release --no-restore
dotnet test Caf.Midden.slnx --configuration Release --no-build
```

All three commands complete successfully on a clean checkout without cloud credentials.

## Phase 1 - Harden develop with continuous integration (complete)

**Outcome:** every change proposed for `develop` receives consistent automated validation, and
the same automation is ready to activate on `main` during the final promotion.

| # | Item | Acceptance criteria |
|---|---|---|
| 6 | Stop ignoring `.github` | Remove the root `.github` ignore entry without changing unrelated ignore rules |
| 7 | Add pull request CI | A workflow restores, builds with warnings enforced, and runs the deterministic suite on pull requests and pushes to `develop`; it is preconfigured to do the same for `main` after Phase 6 |
| 8 | Validate supported operating systems | CI covers Linux and Windows; add macOS where platform-specific behavior justifies its runner cost |
| 9 | Collect test coverage | Existing Coverlet collectors produce a report; publishing to an external service is optional and requires an ownership decision |
| 10 | Prepare dependency maintenance | Commit a low-noise Dependabot configuration for NuGet and GitHub Actions updates targeting `develop`; activation waits until the configuration reaches the default branch in Phase 6 |
| 11 | Add security scanning on develop | Dependency review runs on pull requests to `develop`, and CodeQL runs on pushes and pull requests to `develop`; future `main` and scheduled triggers are present but activate in Phase 6 |
| 12 | Protect develop | Require stable CI checks, at least one approving review, and resolved conversations on `develop`; defer new `main` checks and protection changes to Phase 6 |

**Exit gate**

- The Phase 0 commands pass from a clean checkout of `develop` on Linux and Windows.
- A pull request into `develop` passes build, test, coverage, dependency review, and CodeQL checks.
- Required checks and review rules protect `develop`.
- No readiness changes have been merged or cherry-picked into `main`.

Dependabot version updates and scheduled CodeQL runs are intentionally not part of this exit gate
because GitHub activates them from the default branch. Their configuration is reviewed here and
verified after the final promotion.

**Completion record:** PR #164 merged to `develop` at commit
`5c73ab9a2a6e061c2f410e651473baf4b00f517f`. Its Linux and Windows builds, deterministic tests,
coverage, dependency review, and CodeQL checks passed. GitHub reports `develop` as protected, and
the Phase 1 readiness commits are not present on `main`.

Avoid adding badges until their workflows and links are stable. A red or stale badge is worse
than no badge.

## Phase 2 - Create the contributor and community surface

**Outcome:** people know how to participate and where to ask for help.

| # | Item | Acceptance criteria |
|---|---|---|
| 13 | Add `CONTRIBUTING.md` | Covers prerequisites, setup, build/test commands, integration tests, issue selection, coding conventions, documentation expectations, DCO/CLA decision, and PR review flow |
| 14 | Add a code of conduct | Adopt an established text such as Contributor Covenant and provide a real private enforcement contact |
| 15 | Add `SECURITY.md` | Lists supported versions, private reporting through GitHub Security Advisories or an organizational address, expected acknowledgement window, and disclosure process |
| 16 | Add issue forms | Provide focused bug, feature, and documentation forms plus a config directing security reports away from public issues |
| 17 | Add a pull request template | Requests problem/context, change summary, validation, documentation impact, screenshots for UI changes, and linked issues |
| 18 | Identify ownership | Add CODEOWNERS and a short maintainer/governance section describing review and release authority |
| 19 | Establish support channels | State what belongs in Issues versus Discussions or another support channel, and enable the selected repository feature |
| 20 | Create useful labels | Define a small label set for type, priority, component, contributor readiness, and blocked work; seed several bounded `good first issue` tasks |

Private email addresses and maintainer handles cannot be invented in code. Their owners must be
confirmed before these documents are merged.

## Phase 3 - Repair onboarding and technical documentation (complete)

**Outcome:** users and developers can understand and operate the project without relying on
tribal knowledge.

| # | Item | Acceptance criteria |
|---|---|---|
| 21 | Rewrite the README entry paths | Separate "Use Midden," "Run the CLI," "Deploy your own," and "Develop Midden" paths; link the CLI guide and contribution guide |
| 22 | Correct README defects | Fix unintentional wording errors while preserving intentional wordplay, use the actual global-search screenshot, standardize GitHub/Netlify naming, and verify every image and external link |
| 23 | Add an architecture overview | Explain Core, CLI, Wasm, data flow, metadata versions, crawler abstractions, validation, catalog generation, and credential boundaries in `docs/architecture` |
| 24 | Document deployment | Give maintained steps for static hosting, app configuration, catalog updates, base paths, and service-worker/cache considerations |
| 25 | Document configuration schemas | Provide safe example files with placeholders and explain compatibility/versioning expectations |
| 26 | Add support and troubleshooting links | Make operational troubleshooting discoverable from the README and relevant error messages |
| 27 | Audit documentation accessibility | Every meaningful image has useful alt text; headings and tables are navigable; instructions do not depend on screenshots alone |

The comprehensive `docs/usage-guides/cli-usage.md` remains the canonical CLI operating guide
and should be linked rather than duplicated.

Approved decisions, implementation steps, and validation evidence are recorded in the
[Phase 3 documentation plan](20260903_phase-3-documentation-plan.md).

**Completion record:** Maintainer review was completed on 2026-09-03. The README entry paths,
architecture, deployment, configuration, troubleshooting, and accessibility work are complete.
Configuration versions are enforced as `v0.2` current and `v0.1` legacy, Azure Static Web Apps
uses `staticwebapp.config.json`, and the Phase 3 validation record is maintained in the linked
documentation plan.

## Phase 4 - Define versioning and automate releases

**Outcome:** users can identify, obtain, verify, and upgrade between supported versions.

| # | Item | Acceptance criteria |
|---|---|---|
| 28 | Adopt a version policy | Document SemVer interpretation for the combined product and decide whether CLI, Core, and Wasm release together |
| 29 | Centralize version metadata | Remove conflicting project-local versions or document why a component intentionally differs |
| 30 | Add `CHANGELOG.md` | Follow a consistent format with an Unreleased section, dated releases, breaking changes, and migration notes |
| 31 | Add a release runbook | `RELEASING.md` covers prerequisites, version update, validation, tag format, artifacts, release notes, rollback, and post-release checks |
| 32 | Automate release artifacts | A protected tag or manual workflow publishes four self-contained CLI archives and SHA-256 checksums without committing binaries |
| 33 | Add artifact smoke tests | Each built CLI starts and reports help/version on its native runner before release publication |
| 34 | Publish provenance where practical | GitHub artifact attestations or equivalent provenance are attached to release artifacts |
| 35 | Define support policy | README and SECURITY identify supported releases and how long critical fixes are backported |

NuGet, Homebrew, WinGet, containers, and code signing are follow-up distribution decisions.
Evaluate them from demonstrated user demand rather than making the first release workflow
depend on all package ecosystems.

## Phase 5 - Quality, accessibility, and maintenance maturity

**Outcome:** quality work is visible, repeatable, and proportionate to project risk.

| # | Item | Acceptance criteria |
|---|---|---|
| 36 | Establish coverage expectations | Report coverage trends and set thresholds only after measuring a stable baseline; do not reward low-value line coverage |
| 37 | Add a web accessibility check | Run an automated axe or Lighthouse check against representative editor and catalog routes, followed by a documented keyboard/screen-reader manual pass |
| 38 | Audit unfinished markers | Convert actionable TODOs into issues with context and remove stale comments; prioritize validation and visible error-state TODOs |
| 39 | Review dependency licenses | Generate and review direct/transitive dependency license information for release artifacts; document required notices |
| 40 | Define maintenance cadence | Record owners and intervals for dependency review, stale issue triage, release planning, access review, and documentation checks |
| 41 | Add repository metadata | Configure description, homepage, topics, social preview, funding metadata if applicable, Discussions, and private vulnerability reporting in GitHub settings |
| 42 | Record key decisions | Introduce short architecture decision records for licensing, versioning, integration-test policy, and release distribution |

## Phase 6 - Promote the release candidate and activate main

**Outcome:** the fully reviewed release candidate becomes the protected default branch and can
be launched as `v1.0.0` without any earlier partial synchronization to `main`.

| # | Item | Acceptance criteria |
|---|---|---|
| 43 | Freeze the release candidate | Phases 0 through 5 are complete on `develop`; the candidate commit passes all required checks, release smoke tests, documentation review, and the clean-checkout gate |
| 44 | Review the cumulative promotion | Open one `develop`-to-`main` pull request, review the complete branch diff and migration impact, and approve it as the `v1.0.0` release candidate; do not merge partial subsets |
| 45 | Promote develop to main | Merge the approved release candidate once; record the source and resulting commit identifiers, and make no unrelated direct changes to `main` |
| 46 | Verify default-branch automation | The post-merge `main` CI and CodeQL runs pass on Linux and Windows; coverage is uploaded; Dependabot accepts its configuration and targets version updates to `develop` while security updates target `main`; scheduled workflows and security features report healthy status |
| 47 | Protect main | After the new checks have produced stable check names, require them on `main` together with at least one approving review and resolved conversations; block force pushes and deletion |
| 48 | Launch and synchronize | Create the protected `v1.0.0` tag and GitHub Release only after items 46 and 47 pass, verify published artifacts and checksums, then merge any release-only `main` changes back into `develop` |

The new workflows cannot be relied on as required checks for the promotion pull request if their
definitions do not yet exist on `main`. Compensate by requiring the exact candidate commit to pass
all checks on `develop`, reviewing the full promotion diff, and withholding the release tag until
the post-merge `main` runs succeed. Do not configure a required `main` check until GitHub has
reported that check at least once.

---

## Delivery order and milestones

### Milestone 1 - Develop contribution-ready

Complete Phases 0 and 1 on `develop`. A clean checkout is green, pull requests receive required
checks, and `main` remains unchanged.

### Milestone 2 - Community-ready

Complete Phase 2 and the README/architecture portions of Phase 3 on `develop`. Contribution,
conduct, security, ownership, support, and technical orientation are ready for promotion.

### Milestone 3 - Release-candidate ready

Complete Phases 3 through 5 on `develop`. A maintainer can create a versioned, documented,
verifiable release from the candidate without local build artifacts, but no release tag is
created yet.

### Milestone 4 - Version 1.0.0 launched

Complete Phase 6. The release candidate is promoted once, default-branch automation and
protection are healthy, `v1.0.0` is published, and any release-only changes are synchronized back
to `develop`.

## Suggested issue breakdown

Keep pull requests reviewable by opening separate issues for these work streams:

1. Deterministic default tests and live-integration test policy.
2. Secret-store invalid-payload exception contract.
3. SDK pin and documented developer commands.
4. Develop CI and branch protection.
5. Dependabot configuration, dependency review, and CodeQL preparation.
6. Licensing decision and contributor provenance.
7. Community health files and templates.
8. README and link/image cleanup.
9. Architecture and deployment documentation.
10. Versioning, changelog, and release runbook.
11. Release artifact workflow and smoke tests.
12. Accessibility and dependency-license audits.
13. Final `develop`-to-`main` promotion, default-branch activation, and `v1.0.0` launch.

## Definition of done

The open-source readiness initiative is complete when:

- A first-time contributor can follow repository documentation from clone to a green test run.
- Default CI requires no private credentials and passes on the protected default branch.
- Contribution, conduct, support, security, ownership, and licensing expectations are explicit.
- The README accurately routes researchers, deployers, and developers.
- Architecture and deployment documentation cover the supported product surface.
- A version tag produces tested, checksummed release artifacts and a changelog entry.
- Dependency and code scanning run automatically with a named triage owner.
- Repository settings and recurring maintenance tasks are documented and assigned.
- All readiness work was prepared on `develop`, and `main` changed only through the final
  reviewed promotion.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| Community files name contacts or policies that are not actively monitored | Confirm owners and response expectations before publication |
| Live integration tests become permanently neglected | Assign an owner and scheduled cadence, or replace them with deterministic contract tests |
| CI grows slow or expensive | Keep the required path focused; schedule broader platform and security checks where appropriate |
| The cumulative promotion to `main` is too large to review safely | Freeze `develop`, require every earlier phase gate, review the complete branch diff, and record the exact validated candidate commit |
| New workflows cannot run as required checks before they exist on `main` | Validate the exact candidate on protected `develop`, review the promotion diff, and require successful post-merge `main` runs before tagging the release |
| Release automation publishes an unintended build | Require protected tags/environments, smoke tests, checksums, and maintainer approval |
| Governance becomes burdensome for a small team | Start with the minimum policy set and revisit it after real contributor feedback |
| Licensing language conflicts with organizational policy | Treat legal approval as a gate before soliciting external contributions |

## Verification record

The 2026-09-01 audit was read-only. No credentials or generated publish artifacts were found in
Git tracking, and the NuGet vulnerability query was clean at that point in time. These results
are a baseline, not a substitute for the automated recurring checks proposed above.