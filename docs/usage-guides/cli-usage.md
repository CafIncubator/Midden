# Midden CLI (the Crawler) — Usage Guide

The Midden CLI walks through your data stores, finds every `.midden` metadata file your team
has created with the Editor, and collates them into a single `catalog.json` that the Midden
Catalog website reads.

You run it whenever you want the catalog to pick up new or changed metadata.

> **New to Midden?** Start with the [Wiki](https://github.com/CafIncubator/Midden/wiki) for the
> bigger picture. This guide covers the command line tool only.

## Contents

- [Quick start](#quick-start)
- [Commands](#commands)
- [Checking metadata before you publish](#checking-metadata-before-you-publish)
- [The configuration file](#the-configuration-file)
- [Data store types](#data-store-types)
- [Handling passwords and secrets](#handling-passwords-and-secrets)
- [Signing in to Google](#signing-in-to-google)
- [Signing in to Azure](#signing-in-to-azure)
- [Running on a schedule or in a container](#running-on-a-schedule-or-in-a-container)
- [Files the CLI creates](#files-the-cli-creates)
- [Environment variables](#environment-variables)
- [Troubleshooting](#troubleshooting)
- [Known limitations](#known-limitations)

---

## Quick start

Crawling a folder on your own computer needs no credentials at all.

**1. Make a working folder and create a starter configuration.**

```powershell
mkdir C:\midden
cd C:\midden
MiddenCli setup
```

This writes a `configuration.json` template into the current folder.

**2. Edit `configuration.json`** to point at the folder holding your projects:

```json
{
  "Version": "1.0.0",
  "DataStores": [
	{
	  "Name": "MyProjects",
	  "Type": "LocalFileSystem",
	  "Path": "C:\\Path\\To\\Projects",
	  "ShouldCollateProjects": true
	}
  ]
}
```

**3. Run the crawler.**

```powershell
MiddenCli collate
```

It lists what it is about to crawl and asks you to confirm. When it finishes you will have a
`catalog.json` in the folder you ran it from. Upload that to your Midden Catalog site.

---

## Commands

Run `MiddenCli --help`, or `--help` on any command, to see this at any time.

| Command | What it does |
|---|---|
| `setup` | Create a blank `configuration.json` in the current folder |
| `collate` | Crawl your data stores and write `catalog.json` |
| `validate` | Check `.midden` and `DESCRIPTION.md` files for problems, without building a catalog |
| `secret set <name>` | Store a password or key safely, encrypted |
| `secret list` | Show which secrets are stored (never the values) |
| `secret remove <name>` | Delete a stored secret |
| `login google <datastore>` | Sign in to Google once, so `collate` does not have to |

### `collate` options

| Option | Meaning |
|---|---|
| `-d`, `--datastores <names>` | Crawl only the named data stores. Defaults to all of them |
| `-s`, `--silent` | Do not ask for confirmation. Use this for scheduled runs |
| `-o`, `--outdir <path>` | Where to write the catalog. Defaults to `catalog.json` in the current folder |
| `-c`, `--config <path>` | Use a configuration file somewhere other than the current folder |
| `--verbose`, `-v` | Print full exception details, including data normally redacted (such as SAS query strings). Use when troubleshooting |
| `--strict` | Stop and exit with a failure code on the first data store that fails, instead of continuing with the rest |

If you run `collate` with input redirected (for example, from a script that pipes into it) you
must also pass `--silent`; otherwise the CLI cannot show a confirmation prompt and will exit
with an explanatory message rather than hang.

Examples:

```powershell
# Crawl everything, no prompt
MiddenCli collate --silent

# Crawl just two stores and write somewhere specific
MiddenCli collate -d MyProjects SharedDrive -o C:\web\catalog.json

# Use a configuration kept elsewhere
MiddenCli collate --config D:\configs\midden\configuration.json

# Fail immediately on the first broken data store, for CI
MiddenCli collate --silent --strict
```

When it finishes, `collate` prints a summary — stores succeeded/failed/skipped, datasets and
projects found, and elapsed time — and exits with a non-zero code if any store failed or was
skipped, even without `--strict`. This lets a scheduled job detect a partial catalog from the
exit code alone.

---

## Checking metadata before you publish

`collate` is deliberately forgiving: if a `.midden` file cannot be read, it skips that file and
carries on so one bad file cannot cost you the whole catalog. The downside is that a broken
dataset shows up only as a dataset quietly *missing* from the published catalog, which is easy
to miss and hard to trace back.

`validate` is how you find those problems on purpose:

```powershell
MiddenCli validate C:\Path\To\Projects
```

It reads every `.midden` and `DESCRIPTION.md` file it finds, checks them, and prints what is
wrong and where:

```
C:\Path\To\Projects\Raw\CookEastMet.midden
  error: dataset.name: A dataset name is required.
    The .midden file and the catalog entry are both named from this.
  warning: dataset.tags: This dataset has no tags.
    Tags are how users browse and search the catalog.
Summary: checked 7 file(s). 1 with errors, 3 with warnings, 0 unreadable.
```

You can give it individual files or whole folders; folders are searched all the way down. A
named file is checked whatever it is called, so you can validate a file you have not renamed to
`.midden` yet.

### Options

| Option | Meaning |
|---|---|
| `-a`, `--app-config <path>` | The app configuration file the Editor produces. Supplies your team's zone, project, and tag lists |
| `-w`, `--warnings-as-errors` | Treat quality warnings as failures |
| `-q`, `--quiet` | Only print files that have problems |

Examples:

```powershell
# Check one file you are about to hand over
MiddenCli validate .\CookEastMet.midden

# Check everything, using your team's vocabularies
MiddenCli validate C:\Projects --app-config C:\midden\app-configuration.json

# In CI: only show problems, and insist on clean metadata
MiddenCli validate C:\Projects --quiet --warnings-as-errors
```

### Errors and warnings are different on purpose

**Errors** mean the dataset would break the catalog or the file itself — a missing name, an
unparseable geometry. These always fail the run.

**Warnings** mean the metadata is usable but thin: no tags, a variable with no description.
These do *not* fail the run by default. Documentation is often genuinely incomplete while work
is in progress, and a tool that refuses to proceed until every field is filled in teaches people
to enter junk values to get past it. If your team has agreed on a standard and wants it
enforced, pass `--warnings-as-errors`.

This is the same distinction the Editor makes: errors block the Download button, warnings ask
for a confirmation you can accept.

### Vocabularies need `--app-config`

Checks for zone, project, and tag names compare against the lists in your Midden app
configuration — the file the Configuration editor produces. Those lists are specific to your
organisation, so without `--app-config` those particular checks are **skipped rather than
guessed**. Everything else is still checked.

### Exit codes

| Code | Meaning |
|---|---|
| `0` | Everything checked was clean |
| `1` | At least one file has a problem |
| `2` | The check could not run — a path did not exist, or the app configuration was unreadable |

`2` is separate from `1` so an automated job can tell "your metadata is wrong" apart from "the
check never actually ran". A typo in a scheduled job's path would otherwise look exactly like a
passing build.

### Using it in a scheduled job

Validating before crawling turns a silently incomplete catalog into an explicit failure you can
act on:

```powershell
MiddenCli validate C:\Projects --quiet
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
MiddenCli collate --silent
```

`validate` never writes anything and never needs credentials — it only reads files you already
have. It is always safe to run.

---

## The configuration file

`configuration.json` is plain text on purpose. You are meant to be able to open it, read it,
and change it. It holds no passwords unless you put them there (and you should not — see
[Handling passwords and secrets](#handling-passwords-and-secrets)).

```json
{
  "Version": "1.0.0",
  "DataStores": [ ... ]
}
```

`Version` describes the format of the file itself. Leave it at `1.0.0`. If you omit it the
CLI assumes `1.0.0`.

### Where the CLI looks for it

1. The path given with `--config`, if you used it
2. `configuration.json` in the folder you are currently in
3. `configuration.json` next to `MiddenCli.exe`

### Data store settings

Every data store has a `Name` and a `Type`. The rest depend on the type.

| Setting | Used by | Meaning |
|---|---|---|
| `Name` | all | A label you choose. Appears in catalog paths as `[Name]` |
| `Type` | all | See [Data store types](#data-store-types) |
| `ShouldCollateProjects` | all | Also read `DESCRIPTION.md` project files. Defaults to `false` |
| `Path` | local, file shares | Folder to crawl |
| `AccountName` | Azure Data Lake | Storage account name |
| `AzureFileSystemName` | Azure Data Lake | Container / file system name |
| `TenantId` | Azure | Your organisation's Entra tenant |
| `ClientId` | Azure, Google | Application ID, from your IT administrator |
| `ClientSecret` | Azure, Google | Application password. **Use a `secret:` reference** |
| `Uri` | Azure file shares | Share URL |
| `SharedAccessSignature` | Azure file shares | SAS token. **Use a `secret:` reference** |
| `ApplicationName` | Google | Application name registered with Google |
| `AuthFilePath` | Google | Path to a service account key file |
| `AzureEndpointSuffix` | Azure Data Lake | Only needed for sovereign clouds (e.g. Azure Government, Azure China). Defaults to `dfs.core.windows.net` |
| `AzureAuthorityHost` | Azure Data Lake | Only needed for sovereign clouds, alongside `AzureEndpointSuffix`. Defaults to the public Azure AD authority |

If you misspell a setting name the CLI now tells you, rather than quietly ignoring it.

---

## Data store types

| `Type` value | Status |
|---|---|
| `LocalFileSystem` | Supported |
| `AzureDataLakeGen2` | Supported |
| `AzureFileShares` | Supported |
| `GoogleDrive` | Supported |
| `GoogleWorkspaceSharedDrive` | Supported |
| `GithubOrganization` | Not implemented yet |
| `FileTransferProtocol` | Not implemented yet |
| `Office365OneDrive` | Not implemented yet |

---

## Handling passwords and secrets

Some data stores need a password, key, or token. Rather than typing it into
`configuration.json` where it can be committed to git, backed up to OneDrive, or shared with
a colleague by accident, store it encrypted.

**1. Store the secret.** You are prompted for the value and it is never shown on screen:

```powershell
MiddenCli secret set adls-prod
```

**2. Reference it from `configuration.json`** using `secret:` followed by the name:

```json
{
  "Name": "Production",
  "Type": "AzureDataLakeGen2",
  "AccountName": "mystorageaccount",
  "AzureFileSystemName": "data",
  "TenantId": "00000000-0000-0000-0000-000000000000",
  "ClientId": "11111111-1111-1111-1111-111111111111",
  "ClientSecret": "secret:adls-prod"
}
```

That is the whole workflow. `collate` picks the value up automatically.

### On Windows there is no password to remember

The secret store is encrypted using Windows' built-in protection, tied to your Windows
account. You are never asked for a password, and the file cannot be read by anyone else or on
any other computer — so if it is copied or synced somewhere by mistake, it is useless to
whoever finds it.

### On macOS and Linux, or to move the store between machines

Use a password instead:

```powershell
MiddenCli secret set adls-prod --password
```

You will be asked to choose a password and confirm it.

> **There is no password recovery.** If you forget it, run `secret set` again for each secret.

### Checking what is stored

```powershell
MiddenCli secret list      # names only, never values
MiddenCli secret remove adls-prod
```

### Putting a secret directly in the configuration

This still works, so nothing breaks if you upgrade an existing setup. The CLI prints a
warning each run telling you which setting is exposed and how to move it.

---

## Signing in to Google

Google Drive and Google Workspace Shared Drives need you to sign in through a browser the
first time.

Do it once, up front:

```powershell
MiddenCli login google SharedDrive
```

where `SharedDrive` is the `Name` of the data store in your configuration. A browser opens,
you sign in, and the CLI remembers it. Later `collate` runs reuse that sign-in without
prompting.

For scheduled or automated runs, use a **service account** instead of a personal sign-in.
Ask your IT administrator for a service account key file, share the drive with the service
account's email address, and point at the key:

```json
{
  "Name": "SharedDrive",
  "Type": "GoogleWorkspaceSharedDrive",
  "ApplicationName": "Midden",
  "AuthFilePath": "C:\\midden\\service-account.json",
  "ShouldCollateProjects": true
}
```

Service accounts never require a browser, so this is the right choice for anything unattended.

---

## Signing in to Azure

For Azure Data Lake, the simplest and safest setup is to **leave `ClientSecret` out entirely**:

```json
{
  "Name": "Production",
  "Type": "AzureDataLakeGen2",
  "AccountName": "mystorageaccount",
  "AzureFileSystemName": "data",
  "TenantId": "00000000-0000-0000-0000-000000000000"
}
```

The CLI then signs you in the way you are used to signing in to any other Microsoft site. It
tries, in order: environment variables, the managed identity of the machine it is running on,
an Azure sign-in you already have on your computer, and finally a browser prompt.

This means no password is stored anywhere. Your data administrator only has to grant your
normal account read access to the storage container.

Supply `ClientId` and `ClientSecret` only if your administrator has specifically issued you an
application registration to use.

---

## Running on a schedule or in a container

For unattended runs — Windows Task Scheduler, cron, Azure App Service, a container — follow
these three rules.

**1. Always pass `--silent`,** otherwise the confirmation prompt will stall the job.

**2. Supply secrets through environment variables** rather than deploying the encrypted store.
For a secret referenced as `secret:adls-prod`, set `MIDDEN_SECRET_ADLS_PROD`. The name is
upper-cased with anything that is not a letter or digit replaced by an underscore.

In Azure App Service these are Application Settings, which are encrypted at rest and can be
backed by Key Vault references without any change to your configuration file.

**3. Set `MIDDEN_NON_INTERACTIVE=1`** so that a missing credential fails immediately with a
clear message instead of waiting for a browser sign-in that nobody will complete.

```powershell
$env:MIDDEN_NON_INTERACTIVE = "1"
$env:MIDDEN_SECRET_ADLS_PROD = "<from your secret manager>"
MiddenCli collate --silent --outdir /out/catalog.json
```

For Azure data stores in App Service, prefer enabling a **managed identity** and omitting the
secret entirely — then there is nothing to configure at all.

---

## Files the CLI creates

`secrets.midden` and `.midden-google-tokens/` are always kept next to your
`configuration.json`, so the three travel together if you move the folder. `catalog.json` is
written to the folder you ran `collate` from, unless you passed `--outdir`.

| File | What it is | Commit to git? |
|---|---|---|
| `configuration.json` | Your settings. Plain text, safe to read and edit | No — it may contain literal secrets |
| `secrets.midden` | Encrypted credentials | **Never** |
| `.midden-google-tokens/` | Cached Google sign-in | **Never** |
| `catalog.json` | The output you publish | Usually no |

If you keep your working folder inside a git repository, add these to its `.gitignore`. The
Midden source repository already excludes them.

---

## Environment variables

| Variable | Purpose |
|---|---|
| `MIDDEN_SECRET_<NAME>` | Supplies the secret named `<NAME>`, taking priority over the encrypted store |
| `MIDDEN_STORE_PASSWORD` | Password for a password-protected secret store, so scripts are not prompted |
| `MIDDEN_NON_INTERACTIVE` | Any non-empty value disables browser sign-in prompts |

---

## Troubleshooting

**"Unable to find 'configuration.json'."**
You are not in the folder holding your configuration. Either `cd` into it, run `MiddenCli setup`
to create one, or pass `--config` with the full path.

**"The secret 'x' could not be found."**
Either run `MiddenCli secret set x`, or set the `MIDDEN_SECRET_X` environment variable. The
message tells you both options and the exact variable name to use.

**"Unable to decrypt 'secrets.midden'. It was created by a different Windows user or on a
different machine."**
The Windows-protected store only works for the account that created it. Either run as that
account, or recreate the secrets with `--password` to make a portable store.

**"Unable to decrypt... The password is incorrect, or the file has been modified."**
Wrong password. There is no recovery — run `secret set` again for each secret.

**"No cached Google credentials were found... and interactive sign in is disabled."**
Either run `MiddenCli login google <datastore>` on a machine with a browser, or switch that
data store to a service account with `AuthFilePath`.

**"Configuration file ... could not be parsed (line N, position M)."**
A JSON syntax error, usually a missing comma or an unescaped backslash in a Windows path.
Remember paths need doubled backslashes: `"C:\\Path\\To\\Projects"`.

**A data store failed but the run continued.**
That is deliberate — one unreachable store no longer stops the others. The run still exits with
a non-zero code and the summary reports it as failed, so an automated job can detect the partial
catalog; check the messages before publishing it, since it will be missing that store's
datasets. Pass `--strict` if you would rather the run stop immediately on the first failure.

**"Two data stores produced the same dataset path."**
`collate` detects when two data stores would overwrite each other's dataset in the catalog and
reports the collision instead of silently keeping only one. Rename one of the data stores, or
adjust its `Path`, so the two no longer overlap.

**A dataset is missing from the catalog and nothing looked wrong.**
`collate` skips files it cannot read so that one bad file does not cost you the whole run, which
means a broken `.midden` file disappears quietly. Run `MiddenCli validate` over that folder to
see the actual problem and which file it is in.

**`validate` reports problems in a file the Editor was happy with.**
Both use the same rules, so this usually means the file was edited by hand afterwards, or it was
written by an older version of the Editor. Opening it in the Editor will show the same issues in
the tab that owns them.

**`validate` says a zone, project, or tag is unrecognised.**
Those lists come from your Midden app configuration, so pass `--app-config` pointing at it. If
you already did, the value genuinely is not in your team's vocabulary — either correct the file
or add the value to the configuration in the Editor.

---

## Known limitations

Most items from the [CLI hardening plan](../implementation-plans/2026-08-05-cli-hardening.md)
are now resolved: `collate` exits non-zero when a store fails or is skipped, Azure Data Lake
crawling descends the full folder tree, Google Drive crawling pages past 100 files, and paths
containing `.midden` more than once are trimmed correctly. A few items remain deferred by
design, tracked in that plan's Phase 4:

- **The pipeline is synchronous.** Crawling and downloads happen one file at a time rather than
  in parallel, so large data stores take longer than they need to.
- **The whole catalog is built in memory before it is written.** For most research teams this is
  fine; an extremely large catalog could use more memory than expected.
- **No incremental crawl.** Every run re-reads every file; there is no cache of what changed
  since the last run.

None of these affect correctness — they are performance characteristics to be aware of for very
large data stores.
