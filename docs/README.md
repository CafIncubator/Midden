# Midden documentation

This index separates maintained guidance from design history. Start with a usage guide when
operating Midden, the architecture overview when changing it, and the implementation-plan
register only when researching why or when a body of work was undertaken.

## Use and operate Midden

| Document | Purpose |
|---|---|
| [CLI usage](usage-guides/cli-usage.md) | Install, configure, authenticate, crawl, validate, and automate the CLI |
| [Configuration reference](usage-guides/configuration.md) | Understand supported application and CLI configuration formats |
| [Deployment guide](usage-guides/deployment.md) | Publish and update the static editor and catalog |
| [Troubleshooting](usage-guides/troubleshooting.md) | Diagnose CLI, configuration, crawl, catalog, and deployment failures |
| [Safe examples](examples/) | Start from minimal non-sensitive CLI and application configurations |

## Understand and maintain Midden

| Document | Purpose |
|---|---|
| [Architecture overview](architecture/overview.md) | Understand components, data flow, validation, compatibility, and credential boundaries |
| [Architecture decisions](architecture/decisions/README.md) | Review durable cross-cutting decisions and their consequences |
| [Dependency license inventory](maintenance/dependency-license-inventory.md) | Review release dependency versions, licenses, and notice decisions |
| [Implementation-plan register](implementation-plans/README.md) | Find active, deferred, and historical implementation plans |

Project-wide contribution, governance, release, security, support, and versioning policies remain
at the repository root because GitHub and contributors expect to find them there. Begin with the
[project README](../README.md) or [contributor guide](../CONTRIBUTING.md).

## Document authority

Usage guides and root policies describe current supported behavior. Architecture decision records
preserve durable decisions. Implementation plans capture intent and acceptance evidence from a
particular effort; when a plan conflicts with maintained guidance or the current implementation,
the maintained guidance and tested behavior take precedence.