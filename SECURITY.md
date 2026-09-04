# Security policy

Midden's maintainers appreciate responsible reports that help protect researchers, credentials,
and research data.

## Supported versions

Midden supports only the latest published GitHub Release. This policy applies before and after
the first stable release.

| Version | Supported |
|---|---|
| Latest GitHub Release | Yes |
| Older releases | No |
| `develop` and other unreleased builds | No |

Security fixes are normally delivered in a new release rather than backported. Users must
upgrade to the latest release to receive fixes. Reports involving older or development versions
are still welcome when the issue may also affect the latest release.

## Report a vulnerability

Do not report suspected vulnerabilities in a public issue, pull request, discussion, or other
public channel.

Report vulnerabilities through
[GitHub private vulnerability reporting](https://github.com/CafIncubator/Midden/security/advisories/new).
Reports are received by project maintainers. Response times depend on maintainer availability.

Include as much of the following information as is practical:

- The affected component and version or commit
- The conditions required to reproduce the issue
- Reproduction steps or a minimal proof of concept
- The impact to confidentiality, integrity, or availability
- Whether credentials, personal information, or research data may be exposed
- Any known mitigations or workarounds
- A safe way for maintainers to ask follow-up questions

Do not include real credentials, access tokens, private keys, personal information, or sensitive
research data. Use minimal synthetic data where evidence is needed.

## What to expect

Maintainers aim to:

- Acknowledge a report within seven calendar days.
- Provide an initial assessment or request more information within fourteen calendar days.
- Provide updates when the status materially changes.

These are response targets, not a service-level guarantee. Remediation timing depends on
severity, complexity, affected users, release requirements, and maintainer availability.

Maintainers will work with the reporter on a coordinated disclosure date. Please allow a
reasonable remediation period before public disclosure unless users face immediate harm.

When remediation is complete, maintainers will document affected versions, mitigations, fixed
versions, and appropriate credit in a GitHub Security Advisory or release notes. Reporters may
request anonymity.

## Scope

This policy covers code and release artifacts maintained in this repository. Vulnerabilities in
third-party services or dependencies should normally be reported to their maintainers, unless
Midden's use of the dependency creates a distinct vulnerability.

Usage questions, suspected defects without security impact, and feature requests belong in the
channels described in [SUPPORT.md](SUPPORT.md).