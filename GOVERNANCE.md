# Governance

Midden uses lightweight maintainer governance intended to keep decisions transparent without
creating unnecessary process for a small research-focused project.

Midden is a community open-source project. Current maintainers coordinate reviews, releases,
security reports, and community moderation. Contributors who demonstrate sustained involvement
may be invited to become maintainers.

## Roles

### Contributors

Contributors improve code, tests, documentation, issue reports, research workflows, and community
discussion. A contributor does not need commit access or a formal project role.

### Maintainers

Maintainers have repository write access and are responsible for:

- Triaging issues, discussions, and pull requests
- Reviewing changes in their areas of responsibility
- Maintaining a deterministic build and test baseline
- Applying the Code of Conduct
- Triaging private security reports
- Keeping project documentation and repository settings current

### Release maintainers

Release maintainers may approve version changes, promote release candidates, create protected
tags, and publish GitHub Releases. Release authority does not bypass required CI or release
checks.

### Security and conduct responsibilities

Designated maintainers receive private security and repository conduct reports and must protect
reporter privacy. Reports concerning a maintainer may use the external GitHub Support route
documented in the Code of Conduct.

## Current maintainers

| Maintainer | Responsibilities |
|---|---|
| [@bryancarlsoncafltar](https://github.com/bryancarlsoncafltar) | Current project maintainer; code review, security triage, conduct moderation, and releases |

## Decisions and review

Routine changes are decided through pull-request review. Normal development targets `develop`;
`main` represents releasable work and receives changes through the release process.

For substantial or potentially breaking changes, maintainers should seek input in an issue or
discussion before implementation. Decisions should favor the project's research use case,
transparent behavior for non-technical users, maintainability, and compatibility with documented
metadata and configuration formats.

Maintainers should document significant alternatives and tradeoffs and seek contributor or user
input when practical. Project decisions are made by the current maintainers and recorded in the
relevant issue, pull request, or project documentation. Security and Code of Conduct decisions
follow their private processes.

## Maintenance cadence

The current maintainer, [@bryancarlsoncafltar](https://github.com/bryancarlsoncafltar), owns the
following reviews. These intervals are reminders rather than service-level commitments. A dated
issue, pull request, release checklist, or documented settings review is sufficient evidence.

| Activity | Interval |
|---|---|
| Dependency updates and vulnerability alerts | Monthly and when GitHub raises an alert |
| Issue, discussion, and pull-request triage | Monthly |
| Release planning, documentation, and dependency-license review | Before each release |
| Repository access, CODEOWNERS, private reporting, and security settings | Annually and whenever maintainer roles change |

Midden does not automatically close inactive issues. An older research request may remain valid
even when current maintainer capacity is limited.

## Becoming or leaving a maintainer

Current maintainers may invite a contributor who has demonstrated sustained, constructive work
and sound judgment. Any accepted maintainer role should be recorded publicly unless privacy or
organizational policy requires otherwise.

A maintainer may step down at any time. Access should be reviewed when responsibilities change
or an account becomes inactive. Ownership rules and private reporting access must be updated
promptly when maintainers change.