## Context

<!-- What problem or user need does this change address? Why is the change needed? -->

<!-- Link an issue when one exists, for example: Closes #123 -->

## Changes

<!-- Summarize the implementation and important design decisions. -->

## Validation

<!-- List automated commands and manual checks, including relevant operating systems or browsers. -->

```text
dotnet restore Caf.Midden.slnx
dotnet build Caf.Midden.slnx --configuration Release --no-restore
dotnet test Caf.Midden.slnx --configuration Release --no-build
```

## Documentation and compatibility

<!-- Describe documentation changes, configuration or metadata compatibility, and migration needs. -->

- [ ] User-facing or contributor documentation is updated, or documentation is not affected.
- [ ] Breaking changes and migration steps are identified, or this change is backward compatible.

## Visual changes

<!-- For web-interface changes, include before and after screenshots at relevant desktop and mobile sizes. Remove this section when it does not apply. -->

## Checklist

- [ ] The pull request targets `develop` unless it is an approved release or hotfix change.
- [ ] The change is focused and does not include unrelated formatting or refactoring.
- [ ] Tests cover changed behavior, or the validation section explains why tests do not apply.
- [ ] Logs, examples, screenshots, and fixtures contain no credentials or sensitive research data.
- [ ] I created this contribution or am authorized to submit it under Apache-2.0.
- [ ] I have read and will follow `CONTRIBUTING.md` and `CODE_OF_CONDUCT.md`.
