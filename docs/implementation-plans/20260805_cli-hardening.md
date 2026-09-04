# Implementation Plan: CLI Hardening

| | |
|---|---|
| **Status** | Complete (Phases 1\u20136); items 17, 18, 21, 33 intentionally deferred, see Phase 4 |
| **Started** | 2026-08-05 |
| **Branch** | `march-towards-v1` |
| **Scope** | `Caf.Midden.Cli`, `Caf.Midden.Cli.Tests` |

## Context

`Caf.Midden.Cli` (the Crawler) is the component researchers and data managers run to
produce `catalog.json`. A full architecture review on 2026-08-05 surfaced 37 findings
across security, correctness, performance, and maintainability.

This plan tracks that backlog through to completion. Item numbers are preserved from the
original review so that discussion history stays traceable.

## Goals

1. No credential should need to sit in plain text on a researcher's disk.
2. The crawler must never silently produce an incomplete catalog.
3. A failed crawl must be detectable by an automated job.
4. None of the above may make the tool harder for a non-technical researcher to use.

## Non-goals

- Rewriting the crawlers against a new metadata schema. `Caf.Midden.Core` is untouched.
- Packaging as a .NET tool. Distribution is a downloadable executable and, later, a container.
- Changing the `catalog.json` format consumed by `Caf.Midden.Wasm`.

---

## Design decisions

### D1. Transparency is weighted above defence in depth

Midden's users are researchers, not engineers. A design that is secure but unintuitive will
be worked around, and a worked-around design is not secure.

Consequence: `configuration.json` stays **plain text, hand-editable, and diffable**. It is
the file a researcher opens to check which folder is being crawled, and encrypting it whole
would have destroyed that.

### D2. Secrets are split out of the configuration rather than encrypting it

Only credential *values* move into an encrypted companion file. The configuration references
them by name:

```json
"ClientSecret": "secret:adls-prod"
```

This keeps every non-sensitive setting visible while removing the secret from the file most
likely to be accidentally committed, synced to OneDrive, or emailed to a colleague.

### D3. Resolution order is environment variable, then encrypted store, then literal

| Order | Source | Intended for |
|---|---|---|
| 1 | `MIDDEN_SECRET_<NAME>` environment variable | Containers, Azure App Service cron jobs |
| 2 | Encrypted local store (`secrets.midden`) | Researcher on a desktop |
| 3 | Literal value in `configuration.json` | Backwards compatibility only; warns on use |

Rationale: password-encrypting a file does **not** help an unattended job, because the
password must then live in an environment variable anyway — which is strictly more machinery
for identical exposure. Encryption is therefore a desktop measure, and automation gets a
separate, simpler path that never needs the encrypted file deployed at all.

### D4. DPAPI is the default protection provider on Windows

`ProtectedData` with `DataProtectionScope.CurrentUser` gives encryption at rest with **zero
prompts**. The researcher never learns the store exists. The file is inert if copied to
another machine or opened by another Windows account, which defeats the accidental-disclosure
threats that actually occur in practice.

Password-based AES-256-GCM is the portable fallback (`--password`), used automatically on
non-Windows platforms.

### D5. Prefer identities that need no secret at all

Where the platform allows it, the best secret is no secret:

- **Azure Data Lake** — omitting `ClientSecret` selects `DefaultAzureCredential`, which uses
  managed identity when deployed and an ordinary institutional browser sign-in on a desktop.
- **Google Workspace** — a service account key (`AuthFilePath`) is the supported unattended
  path.

### D6. Interactive sign-in is suppressed automatically when headless

A browser prompt in a scheduled job hangs until timeout. Interactive credentials are disabled
when stdin is redirected or `MIDDEN_NON_INTERACTIVE` is set, producing a clear error instead.

### D7. Google device-code flow was evaluated and rejected

Originally approved, then dropped for three reasons:

1. It requires an OAuth client registered as *"TV and Limited Input device"* — a different
   registration than the installed-app client currently in use. Existing client IDs would not work.
2. `Google.Apis.Auth` has no device-flow helper, so it would mean hand-rolling an untested
   OAuth protocol implementation.
3. It does not solve the unattended case anyway, since it still requires a human.

Replaced by `midden login google`, which works with the existing client registration, plus
service accounts for automation. Revisit only if a new Google client registration is created.

---

## Phase 1 — Credential handling (complete)

| # | Item | Status |
|---|---|---|
| 1 | Split config and encrypted secret store; `.gitignore` companion files | Done |
| 2 | Fix Google token cache location; add `midden login google` | Done |
| 5 | Stop echoing raw exception messages that may contain SAS tokens | Done |
| 16 | Hoist per-store try/catch into the collate loop | Done |
| 27 | `--config` option; remove `PackAsTool` and the copy-to-output item | Done |
| 28 | `PublishSingleFile` conflicting with `PackAsTool` | Done |
| 29 | Configuration schema version `1.0.0`; strict member handling | Done |
| 30 | Surface `JsonException` line and position | Done |
| N1 | *(new)* A failing data store aborted the entire run | Done |

**Delivered**

- `Security/ISecretProtector.cs`, `DpapiSecretProtector.cs`, `PasswordSecretProtector.cs`
  (PBKDF2-HMAC-SHA256, 600,000 iterations, AES-256-GCM, key zeroed on dispose).
- `Security/SecretStore.cs` — versioned envelope, atomic temp-file-then-move save,
  `chmod 600` on non-Windows.
- `Security/SecretResolver.cs` — lazy store opening, so a configuration without secret
  references never prompts for a password.
- `Security/AzureCredentialFactory.cs`, `Security/GoogleCredentialFactory.cs`.
- `Actions/Secret.cs` (`set`, `list`, `remove`), `Actions/Login.cs` (`google`).
- `--config` option on `collate` and `secret`.
- Explicit `System.Security.Cryptography.ProtectedData` reference; it had only been
  resolving transitively through `Azure.Identity`.
- 20 unit tests in `SecretStoreTests` and `SecretResolverTests`.

**N1 — regression found during implementation (not in the original review)**

A misconfigured or unreachable data store aborted the entire run with an unhandled
exception. Both crawler construction *and* the crawl itself are now contained per store.
Cloud SDKs authenticate lazily, so failures surface at crawl time rather than at
construction, and both paths needed handling. This also partially satisfies item 16.

**Where the implementation deviates from the original review**

| Review said | What was built | Why |
|---|---|---|
| 1 — env-var substitution syntax `"${MIDDEN_ADLS_SECRET}"` | `"secret:name"` references, resolved from env var → encrypted store → literal | One reference syntax covering both the desktop and automation cases (D2, D3) |
| 2 — put the Google token cache under `LocalApplicationData` | Cache kept beside `configuration.json` | `LocalApplicationData` is wrong for containers and Azure App Service; keeping companion files together means the whole folder is portable |
| 27 — the `AppContext.BaseDirectory` fallback is a hazard | Fallback retained | The hazard was the csproj shipping a populated template, now removed. The fallback is useful for a container with a config mounted beside the binary |

**Backwards compatibility**

Existing plain-text `ClientSecret` values still work, resolving as `Literal` with a warning.
Configurations with no `Version` are treated as `1.0.0`. All five existing test configuration
assets were verified to contain no unmapped members.

**Remaining in this phase**

None. All four items below were closed out in a follow-up pass:

- **Item 1** — `setup` now prints a warning that `configuration.json` may hold secrets, and
  `CreateConfiguration` applies `chmod 600` on non-Windows, matching `secrets.midden`.
- **Item 5** — added `Common/ExceptionSanitizer.cs`, which redacts URL query strings (where a SAS
  token would live) unless `collate --verbose` is passed. Used everywhere `collate` prints an
  `Exception` to the console.
- **Item 16** — `LocalFileSystemCrawler.GetMetadatas` now catches per-file parse failures and
  checks for a null `Dataset` before dereferencing it, logging a skip message instead of
  aborting the whole data store. Covered by
  `LocalFileSystemCrawlerTests.GetMetadatas_MalformedFilesPresent_SkipsThemAndReturnsTheValidOnes`.
- **Item 28** — `Caf.Midden.Cli.csproj` now sets `SelfContained` and `RuntimeIdentifiers`
  (`win-x64;linux-x64;osx-x64`) alongside `PublishSingleFile`, so `dotnet publish -r <rid>`
  produces the intended single-file downloadable executable.

**New tests**

- `ExceptionSanitizerTests` (3 tests) — query-string redaction, verbose passthrough, and
  messages with no query string are left untouched.
- `LocalFileSystemCrawlerTests.GetMetadatas_MalformedFilesPresent_SkipsThemAndReturnsTheValidOnes`
  — a folder with an invalid-JSON file, a valid JSON file with an explicit null `dataset`, and a
  well-formed file; only the well-formed file is returned.

---

## Phase 2 — Silent data loss (complete)

These bugs cause the crawler to produce an incomplete catalog **without reporting an error**,
which is the most damaging failure mode for a cataloging tool.

| # | Item | Status |
|---|---|---|
| 8 | `AzureDataLakeCrawler.GetFileNames` enumerates only one directory level, missing root files and anything nested deeper | Done |
| 10 | `GoogleDriveCrawler.GetFiles` requests `nextPageToken` but never loops, truncating at 100 files | Done |
| 7 | `Replace(".midden", "")` replaces every occurrence, mangling paths such as `archive.midden.data/x.midden` | Done |
| 9 | `Contains(".midden")` matches directories and files such as `notes.midden.bak` | Done |
| 3 | Google Drive query injection — `Q = $"name contains '{term}'"` interpolates unescaped input, so an apostrophe or backslash breaks or silently alters the query | Done |
| 6 | `Directory.EnumerateFiles(..., AllDirectories)` follows reparse points, so a symlink can escape the configured root or loop forever | Done |
| 26 | `MiddenFileExtension` / `MippenFileSearchTerm` duplicated verbatim across all four crawlers | Done |

Items 7 and 9 are duplicated across all four crawlers, so fixing them was the natural moment
to land item 26 as well. (`Mippen` is intentional domain terminology, not a typo.)

Item 3 sits here rather than under security because its practical effect is the same as the
others: a query that silently returns the wrong set of files.

**Delivered**

- `Caf.Midden.Core/Services/MiddenFileConventions.cs` — the single source of truth for
  `MiddenFileExtension` / `MippenFileSearchTerm`, plus `TrimSuffix`, a suffix-only replacement
  for `string.Replace` used to strip the `.midden` extension from a `DatasetPath` without
  touching any other occurrence of the substring in the path (item 7). All four crawlers now
  reference this type instead of their own private copies of the constants (item 26).
- `Caf.Midden.Cli/Common/GoogleDriveQuery.cs` — `EscapeTerm` escapes `\` and `'` before a search
  term is interpolated into a Drive `q` expression (item 3), used by both Google crawlers.
- `AzureDataLakeCrawler.GetFileNames` now issues a single `GetPaths(recursive: true)` call and
  filters to non-directory paths whose name ends with the requested extension, instead of one
  call per top-level directory (item 8), which also fixes the `Contains` vs. suffix bug for
  this crawler (item 9).
- `AzureFileShareCrawler` and both Google crawlers now match file names with `EndsWith` instead
  of `Contains` (item 9); the Azure File Share crawler already recursed and already skipped
  directories, so only the match predicate needed to change.
- `GoogleDriveCrawler.GetFiles` and `GoogleWorkspaceSharedDriveCrawler.GetFiles` now loop on
  `NextPageToken` until exhausted instead of returning only the first page of up to 100 results
  (item 10). `GoogleDriveCrawler`'s listing request also sets `SupportsAllDrives` and
  `IncludeItemsFromAllDrives`, matching the flags already set on its `Files.Get` calls.
- `LocalFileSystemCrawler` no longer uses `Directory.EnumerateFiles(..., AllDirectories)`.
  It walks the tree itself and skips any entry with the `ReparsePoint` attribute, so a symlink
  or junction under the configured root can neither escape the root nor cause infinite
  recursion (item 6).

**New tests**

- `Caf.Midden.Core.Tests/MiddenFileConventionsTests.cs` — suffix-only trimming, including the
  `archive.midden.data/x.midden` pathological case from the review.
- `Caf.Midden.Cli.Tests/GoogleDriveQueryTests.cs` — apostrophe and backslash escaping.
- `Caf.Midden.Cli.Tests/LocalFileSystemCrawlerTests.cs` — a fixture directory containing
  `notes.midden.bak` (must not match), a directory named `SomeFolder.midden` (must not be
  returned as a file), `archive.midden.data/x.midden` (suffix trim must preserve the directory
  segment), and a real, best-effort symbolic-link test that is skipped gracefully on machines
  where creating a symlink requires elevation.

---

## Phase 3 — Reliability and reporting

| # | Item | Acceptance criteria |
|---|---|---|
| 13 | Partial failure still exits `0`, so CI cannot detect a half-empty catalog | Non-zero exit when any store fails; `--strict` to fail on first error | Done |
| 14 | `File.WriteAllText` truncates the existing catalog before serializing, so a crash leaves a corrupt file | Temp file then `File.Move(overwrite: true)` | Done |
| 11 | Misconfigured stores are skipped with a generic message | Report the specific missing property name. Secret-resolution failures already name the secret and the environment variable | Done |
| 12 | Duplicate store names silently resolve to the first match | Validate uniqueness at load; dedupe requested names | Done |
| 15 | `ShouldContinue` aborts confusingly when stdin is redirected | Detect `Console.IsInputRedirected` and require `--silent` | Done |
| 16 | Malformed `.midden` still dereferences a possibly-null `Dataset` | Null check in the parser path (try/catch already hoisted, see Phase 1) | Done (already covered by Phase 1/2 null checks; verified still in place) |
| 4 | `DatasetPath` comes from crawled content and is never validated | Validate resolved paths stay under the store root, so nothing downstream can be induced to write outside it | Done |
| 32 | Two stores can contribute the same `DatasetPath` with no warning | Detect and report collisions | Done |
| 37 | No run summary | Report found/parsed/skipped/errors and elapsed time | Done |

Item 13 was previously observable: a smoke test with one failing and one succeeding store
printed the failure and still exited `0`.

**Delivered**

- `CollateCommand` adds a `--strict` option. Without it, a store that fails to resolve
  credentials, construct a crawler, or crawl still lets the remaining stores run, but the
  process now exits `1` if any store failed or was skipped/unknown (item 13). With `--strict`,
  the run returns `1` immediately on the first such failure instead of continuing.
- `HandleCollate` now writes the catalog through `WriteCatalogAtomically`, which serializes to
  a `<output>.<guid>.tmp` file beside the destination and then `File.Move(..., overwrite: true)`s
  it into place, deleting the temp file if serialization throws (item 14). A crash mid-write can
  no longer leave a half-written `catalog.json`.
- `GetMissingProperties` replaces the generic "does not have enough configuration" message with
  the specific missing property name(s) per data store type (item 11), e.g. `Path` for
  `LocalFileSystem` or `Uri, Path, SharedAccessSignature` for `AzureFileShares`.
- `ConfigurationService.GetConfiguration` now throws `InvalidDataException` when two data stores
  share a name (case-insensitive) (item 12, load-time half). `HandleCollate` also de-duplicates
  the `--datastores` list via `Distinct(StringComparer.OrdinalIgnoreCase)` so a store requested
  twice on the command line is only crawled once (item 12, request-time half).
- `HandleCollate` checks `Console.IsInputRedirected` before prompting and fails fast with a
  message pointing at `--silent` instead of silently reading past EOF (item 15).
- The null-`Dataset` guards added across all four crawlers in Phase 1/2 remain the fix for item
  16; verified still present during this pass, no further change needed.
- `IsDatasetPathSafe` rejects any crawled metadata whose `DatasetPath` is rooted or contains a
  `..` path segment before it is added to the catalog, logging which store contributed it
  (item 4).
- `ReportDatasetPathCollisions` groups the final metadata list by `DatasetPath`
  (case-insensitive) and warns for every path contributed by more than one entry (item 32).
- `HandleCollate` now tracks per-store success/failure/skip counts and elapsed time via a
  `Stopwatch`, printing a summary line (stores succeeded/failed/skipped, datasets and projects
  found, elapsed seconds) after the catalog is written (item 37).
- New tests: `Caf.Midden.Cli.Tests/ConfigurationServiceTests.cs` covers the duplicate/unique
  data store name validation (item 12).

---

## Phase 4 — Performance

| # | Item | Notes |
|---|---|---|
| 17 | Entire pipeline is synchronous; `GetAwaiter().GetResult()` in constructors is deadlock-prone | Deferred — requires making `ICrawl` async and threading a real `CancellationToken` through all five crawlers, `Collate.cs`, and every caller. This is a larger interface-shape change than the other Phase 4 items and is best done on its own, together with item 18 |
| 18 | Serial network round trips, one blocking download per file | Deferred — depends on item 17 (`Parallel.ForEachAsync` needs an async `ICrawl`) |
| 19 | Remote listing is enumerated twice when `ShouldCollateProjects` is true | Done |
| 20 | `ToArray()` + `Encoding.UTF8.GetString` double-allocates every file and does not strip a BOM | Done |
| 21 | Whole catalog held in memory before serializing | Deferred — the catalog is already fully built in memory before item 32's collision check and item 14's atomic write run, both of which need the complete list; streaming would need those checks re-designed first |
| 22 | `AppendDataStoreNameToPath` is a second mutating pass | Done |
| 33 | No incremental crawl | Deferred — needs a persisted cache of ETag/`modifiedTime` per data store between runs, a larger feature than the rest of Phase 4 |
| 34 | No retry policy for Google 403 `userRateLimitExceeded` | Done |

Sequence these behind Phase 2. Parallelising a crawler that silently drops files would only
produce incomplete catalogs faster.

**Delivered**

- `AzureDataLakeCrawler` now caches the single recursive `GetPaths` listing the first time
  `GetFileNames` is called, so `GetMetadatas` followed by `GetProjects` (when
  `ShouldCollateProjects` is set) issues one remote listing call instead of two (item 19).
- `AzureFileShareCrawler.EnumerateFiles` caches its recursive directory walk the same way, so
  `GetMetadatas` and `GetProjects` share one walk instead of one each (item 19).
- `AzureDataLakeCrawler.GetMetadatas`, `AzureFileShareCrawler.GetMetadatas`,
  `GoogleDriveCrawler.DownloadFileText`, and `GoogleWorkspaceSharedDriveCrawler.DownloadFileText`
  now read JSON text via a `StreamReader` with BOM detection directly over the response stream,
  instead of `stream.CopyTo(memoryStream)` followed by `Encoding.UTF8.GetString(memoryStream.ToArray())`.
  This removes a second full-file byte array allocation per file and correctly strips a leading
  UTF-8 BOM instead of leaving it as a stray character in the parsed JSON (item 20).
- `Caf.Midden.Cli/Actions/Collate.cs` now prepends the data store name to `DatasetPath` in the
  same loop that filters unsafe paths (item 4 from Phase 3), instead of a separate
  `AppendDataStoreNameToPath` pass over the already-built list afterward (item 22). The helper
  method has been removed.
- `Caf.Midden.Cli/Common/GoogleDriveServiceFactory.cs` (new) configures a `BackOffHandler` with
  `ExponentialBackOff` on both unsuccessful-response and exception handling for the underlying
  `ConfigurableHttpClient`, applied to all three `DriveService` construction points across
  `GoogleDriveCrawler` and `GoogleWorkspaceSharedDriveCrawler`. A transient `403
  userRateLimitExceeded`, `429`, or `5xx` from the Drive API is now retried with backoff instead
  of aborting the store's crawl (item 34).

---

## Phase 5 — Architecture

| # | Item | Notes |
|---|---|---|
| 23 | `ICrawl.GetFileNames` is an implementation detail with different semantics per crawler — local returns full paths, ADLS relative paths, Drive returns file *IDs* | Done |
| 24 | `DriveService` and its `HttpClient` are never disposed | Done |
| 25 | No DI, no logging abstraction, `Console.WriteLine` throughout the service layer | Deferred — a larger seam (`ILogger`, `ICrawlerFactory`, `IFileSystem`) than the rest of Phase 5; tracked separately so it can be reviewed on its own |
| 31 | Incomplete assembly metadata | Done |
| 35 | Nothing verifies the catalog is loadable by `Caf.Midden.Wasm` | Done |
| 36 | `.dfs.core.windows.net` is hardcoded | Done |

Item 25 unlocks meaningful unit testing of the crawlers, which currently cannot be tested
without live credentials. Six of the ten CLI tests are integration tests requiring real
cloud accounts.

**Delivered**

- `ICrawl.GetFileNames` was removed from the public interface. Each crawler still has its own
  `GetFileNames` with crawler-specific semantics (full path, relative path, or file ID), but it
  is now `internal` rather than exposed through `ICrawl`, and visible to
  `Caf.Midden.Cli.Tests` via `InternalsVisibleTo` so the existing unit tests are unaffected
  (item 23).
- `ICrawl` now extends `IDisposable`. `GoogleDriveCrawler` and `GoogleWorkspaceSharedDriveCrawler`
  dispose their `DriveService` (which also disposes its underlying `HttpClient`); the other three
  crawlers have no unmanaged resources and implement an empty `Dispose()` for contract symmetry.
  `Collate.cs` wraps each crawler in a `using` per data store so it is disposed as soon as that
  store's crawl finishes (item 24).
- `Directory.Build.props` now sets `InvariantGlobalization`, `Authors`, and a single `Version`
  property shared by every project. `Caf.Midden.Cli.csproj` adds `Description` and
  `PackageLicenseExpression` (originally CC0-1.0 and later updated with the repository license)
  and scopes
  `TreatWarningsAsErrors` to itself only, since applying it solution-wide surfaced an unrelated
  pre-existing warning in `Caf.Midden.Wasm`. The duplicate `AssemblyVersion`/`FileVersion`/
  `Version` properties were removed in favor of the single `Version` from `Directory.Build.props`
  (item 31).
- `Collate.cs` now deserializes the freshly serialized catalog JSON back into a `Catalog` and
  checks the metadata/project counts match before the atomic write proceeds, so a serialization
  bug that would leave `Caf.Midden.Wasm` unable to load the catalog is caught immediately instead
  of surfacing later (item 35).
- `DataStore` gained `AzureEndpointSuffix` and `AzureAuthorityHost`, threaded through
  `AzureDataLakeCrawler`'s constructors and `AzureCredentialFactory.CreateDefaultCredential`, so
  Azure Government, Azure China, or other sovereign clouds can be targeted instead of the
  hardcoded `dfs.core.windows.net` suffix and public authority host (item 36).

---

## Phase 6 — Testability seams (item 25)

Phase 5 deferred item 25 because it is a larger change than the rest of that phase. It is
promoted to its own phase here because it is the prerequisite for testing the crawlers
without live cloud credentials.

| # | Item | Notes |
|---|---|---|
| 25a | `Console.WriteLine`/`Console.Error.WriteLine` throughout the service layer | Done |
| 25b | `LocalFileSystemCrawler` cannot be tested without touching a real disk | Done |
| 25c | `Collate.CreateCrawler` is a hardcoded `switch` over `DataStoreTypes` | Done |

### Design constraints

Carried forward from D1: the tool must not become harder for a non-technical researcher to
use. Concretely, that rules out a few otherwise-idiomatic choices.

- **No `Microsoft.Extensions.Hosting`/`IServiceProvider` bootstrap.** A generic host adds
  startup config sources, environment-variable binding, and a second place where settings can
  come from, all of which contradict "`configuration.json` is the one file you open". Manual
  constructor injection from `Program`/the command handlers is enough for three seams.
- **Console output must stay human-readable by default.** The logging abstraction is a thin
  interface over the current messages, not structured JSON logging. `--verbose` and `--silent`
  keep their existing meaning; the abstraction just gives them one place to be honored instead
  of scattered `if (verbose)` checks.
- **`IFileSystem` covers only what the crawlers actually call.** Not a general-purpose file
  system abstraction — enumerate entries, check for reparse points, open a read stream, read
  text. A large surface would be a maintenance cost with no test benefit.

### Sequencing

25a first, because it touches every file the other two touch and is the lowest-risk of the
three. Then 25b, which makes the local crawler's existing tests independent of the `Assets/`
folder. Then 25c, which is only worth doing once there are fake crawlers to hand it.

### Acceptance

- `LocalFileSystemCrawler` has unit tests that construct it over a faked file system, including
  the symlink case that is currently skipped when symlink creation is unavailable.
- `Collate` orchestration (partial failure, `--strict`, collision reporting, run summary) has
  tests driven by fake crawlers rather than requiring a configured data store.
- No `Console.` call remains in `Caf.Midden.Cli/Services/`.
- Offline test count increases; no existing test is deleted to accommodate the refactor.

**Delivered (25a)**

- `Caf.Midden.Cli/Common/ICrawlLogger.cs` (new) defines a two-method seam, `Info` for progress
  and `Warning` for a recoverable problem such as a skipped file. Kept deliberately narrow per
  the phase's design constraints rather than adopting a structured-logging framework.
- `Caf.Midden.Cli/Common/ConsoleCrawlLogger.cs` (new) is the default implementation, preserving
  the existing behavior of progress on stdout and problems on stderr so a researcher's output is
  unchanged. It is exposed as a shared `Instance` and used as the fallback when no logger is
  injected, so every existing call site keeps working without modification.
- All five crawlers take an optional `ICrawlLogger` constructor argument and route their output
  through it. `grep` for `Console.` in `Caf.Midden.Cli/Services/` now returns nothing.
- `Caf.Midden.Cli.Tests/RecordingCrawlLogger.cs` (new) captures messages in memory, and
  `LocalFileSystemCrawlerTests.GetMetadatas_MalformedFilesPresent_ReportsEachSkipAsAWarningOnTheInjectedLogger`
  asserts that both malformed-file skips are reported, which was previously unobservable without
  redirecting the console.

**Delivered (25b)**

- `Caf.Midden.Cli/Common/IFileSystem.cs` (new) declares exactly the five operations
  `LocalFileSystemCrawler` performs: `DirectoryExists`, `GetFileSystemEntries`, `GetAttributes`,
  `ReadAllText`, and `OpenRead`. A recursive enumeration member is deliberately absent, since the
  crawler must walk the tree itself to skip reparse points (Phase 2, item 9).
- `Caf.Midden.Cli/Common/PhysicalFileSystem.cs` (new) is a direct pass-through to `System.IO`,
  exposed as a shared stateless `Instance` and used as the default, so behavior against a
  researcher's real disk is unchanged.
- `LocalFileSystemCrawler` takes an optional `IFileSystem` and routes every disk call through it.
  `EnumerateFiles` and `SafeGetFileSystemEntries` became instance methods as a result.
- `Caf.Midden.Cli.Tests/FakeFileSystem.cs` (new) is an in-memory tree supporting
  `AddDirectory`, `AddFile`, and `AddReparsePointDirectory`. The last of these is the real win:
  `GetFileNames_ReparsePointDirectoryPresent_DoesNotFollowIt` now covers the symlink-escape case
  on every machine, whereas the existing on-disk
  `GetFileNames_SymlinkedDirectoryPresent_DoesNotFollowIt` silently returns early on machines
  without elevation or Developer Mode. Both tests are kept.
- `GetMetadatas_FakedTree_ReadsThroughTheInjectedFileSystem` confirms parsing and dataset-path
  trimming work end to end against a faked tree with no `Assets/` dependency.

**Delivered (25c)**

- `Caf.Midden.Cli/Common/ICrawlerFactory.cs` (new) declares a single `Create` method that maps a
  `DataStore` plus its resolved secrets to an `ICrawl?`, mirroring the parameters the old in-file
  `switch` closed over.
- `Caf.Midden.Cli/Services/CrawlerFactory.cs` (new) is the production implementation, carrying the
  `switch` over `DataStoreTypes` (including the Azure Data Lake default-credential path) out of
  `Collate.cs` unchanged in behavior.
- `CollateCommand.HandleCollate` takes an optional `ICrawlerFactory`, defaulting to
  `CrawlerFactory` so every existing call site is unaffected, and is now `internal` (with
  `InternalsVisibleTo("Caf.Midden.Cli.Tests")`) instead of `private` so tests can drive it directly.
- `Caf.Midden.Cli.Tests/FakeCrawler.cs` (new) provides an `ICrawl` that returns canned metadata or
  throws on demand and records whether it was disposed, plus a `FakeCrawlerFactory` that maps data
  store names to prebuilt fakes.
- `Caf.Midden.Cli.Tests/CollateOrchestrationTests.cs` (new) exercises `HandleCollate` against fake
  crawlers: a failing store alongside a succeeding one still writes the succeeding store's dataset
  and reports a non-zero "partial" exit code, `--strict` behaves the same for this case (still 1,
  since a partial run was already non-zero), an unrecognized requested store name is skipped rather
  than aborting the run, and a successful crawl is disposed. None of this requires a configured
  data store or cloud credentials.

---

## Coverage index

All 37 review items, mapped to the phase that owns them. Nothing is dropped without a
recorded reason.

| Phase | Items |
|---|---|
| 1 — Credential handling | 1, 2, 5, 16, 27, 28, 29, 30, N1 |
| 2 — Silent data loss | 3, 6, 7, 8, 9, 10, 26 |
| 3 — Reliability and reporting | 4, 11, 12, 13, 14, 15, 16, 32, 37 |
| 4 — Performance | 17, 18, 19, 20, 21, 22, 33, 34 |
| 5 — Architecture | 23, 24, 25, 31, 35, 36 |
| 6 — Testability seams | 25 |

Items 5, 16 and 28 appear in two phases because each was only partly resolved by the
credential work. Item 26 was pulled forward into Phase 2 because that phase already edits all
four crawlers. Item 25 appears in both Phase 5 and Phase 6: Phase 5 owned it originally and
deferred it, Phase 6 carries it to completion. N1 is a regression found during implementation,
not part of the original list.

---

## Verification

Every phase must leave the solution in this state:

- `dotnet build` succeeds for the full solution.
- `Caf.Midden.Core.Tests` — 8 tests passing.
- The guaranteed-offline CLI tests cover security, configuration, exception sanitization, Google
  Drive queries, the local crawler, and collate orchestration. Live Azure and Google smoke tests
  now reside in `Caf.Midden.Cli.LiveTests` and are explicit xUnit v3 tests, so the default solution
  test command compiles but does not execute them.
- Maintainers run the live tests explicitly after populating the ignored
  `Caf.Midden.Cli.LiveTests/Assets/CliConfigurationSecrets/` directory. Missing credential files
  are reported as skips rather than silent passes.

Credential material must never be committed. `Caf.Midden.Cli.LiveTests/Assets/CliConfigurationSecrets/`
is git-ignored and was confirmed on 2026-08-05 to have no history in `git log --all`.

## Related documents

- [CLI usage guide](../usage-guides/cli-usage.md)
