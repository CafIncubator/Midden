# Deploy Midden

Midden's editor and catalog are a static Blazor WebAssembly application. A deployment consists of
HTML, CSS, JavaScript, WebAssembly, `app-config.json`, and `catalog.json`; it does not need a web
server with opinions, a database, or a tiny Kubernetes cluster yearning to be useful.

This guide starts with the host-neutral process, then lists the small adaptations required by
GitHub Pages, Netlify, and Azure Static Web Apps.

## Prerequisites

- the .NET SDK selected by the repository's `global.json`;
- an `app-config.json` prepared from the
  [safe example](../examples/app-config.example.json) and reviewed against the
  [configuration reference](configuration.md);
- a `catalog.json` produced by `MiddenCli collate`; and
- a static host that serves `.wasm`, `.json`, `.js`, and `.css` files over HTTPS.

The two JSON files are public. Do not put credentials, private connection details, embargoed
metadata, or anything else in them that should not be downloadable by a visitor.

## Publish the application

From the repository root:

```powershell
dotnet restore Caf.Midden.slnx
dotnet publish Caf.Midden.Wasm/Caf.Midden.Wasm.csproj --configuration Release --no-restore
```

The static site is produced under:

```text
Caf.Midden.Wasm/bin/Release/net10.0/publish/wwwroot
```

Deploy the **contents** of that `wwwroot` directory as the host's site root. Do not commit the
generated publish directory.

Before uploading, replace its `app-config.json` with the reviewed configuration and its
`catalog.json` with the collated catalog. You may instead deploy the default configuration first,
open `/editor/app-configuration`, edit it in the browser, and download a replacement
`app-config.json` for review and redeployment. The static site cannot update its own hosted file.

Keeping deployment inputs outside generated output and copying them in at this point makes it
harder for a clean publish to preserve last month's catalog through sheer nostalgia.

## Choose the base path

The `<base href>` in the published `index.html` controls every relative application URL.

| Public URL | Base value |
|---|---|
| `https://catalog.example.org/` | `/` |
| `https://example.github.io/` | `/` |
| `https://example.github.io/research-catalog/` | `/research-catalog/` |

The value must start and end with `/`. For a subpath deployment, update the element before
uploading:

```html
<base href="/research-catalog/" />
```

Prefer a relative `catalogPath`, normally `catalog.json`. A value beginning with `/` ignores the
base path and asks the domain root for the catalog instead. An absolute HTTPS URL is supported,
but a catalog on another origin must return a CORS header allowing the Midden site's origin. The
[configuration reference](configuration.md#application-configuration) covers both forms.

## Configure SPA fallback

Midden uses client-side routes such as `/editor`, `/catalog`, and `/catalog/projects`. The static
host must return `index.html` when a visitor refreshes or opens one of those routes directly.
Without a fallback, the home page works and bookmarked pages return 404, an unnecessarily
specific way to greet returning users.

### GitHub Pages

GitHub Pages project sites are served at `/<repository>/`, so set the base path accordingly.
Organization or user sites named `<owner>.github.io` normally use `/`.

GitHub Pages has no native SPA rewrite rule. After publishing and setting the base path, copy
`index.html` to `404.html` in the deployed site root. Pages serves that file for an unknown route,
and Blazor then handles the browser URL. Keep both files synchronized on every deployment.

Configure Pages to deploy the prepared `wwwroot` artifact through a GitHub Actions workflow or a
dedicated publishing branch. Do not point Pages at the Wasm source directory: browsers are
talented, but compiling C# from a repository checkout is not among their advertised features.

See GitHub's maintained documentation for
[GitHub Pages site types](https://docs.github.com/en/pages/getting-started-with-github-pages/about-github-pages)
and publishing-source options.

### Netlify

Deploy the published `wwwroot` directory. For a root deployment, add a file named `_redirects` to
that directory:

```text
/*  /index.html  200
```

This is a rewrite, not a browser redirect. Existing static files still win, while unknown paths
serve the Blazor entry point. Put more specific Netlify rules before this final catch-all rule.

See Netlify's maintained
[single-page application rewrite documentation](https://docs.netlify.com/routing/redirects/rewrites-proxies/#history-pushstate-and-single-page-apps).

### Azure Static Web Apps

Deploy the published `wwwroot` directory as the workflow's output location. The repository
publishes `staticwebapp.config.json` at the root of that output with this fallback:

```json
{
  "navigationFallback": {
    "rewrite": "/index.html"
  }
}
```

The repository includes this current `staticwebapp.config.json` in published output. Azure's
older `routes.json` format is deprecated and is no longer used.

When upgrading an existing deployment workspace, remove its old generated publish directory
before publishing once. `dotnet clean` does not remove stale files there, so an old `routes.json`
can otherwise linger despite no longer existing in source. A fresh checkout or CI workspace does
not have this problem.

See Microsoft's maintained reference for
[Azure Static Web Apps configuration](https://learn.microsoft.com/azure/static-web-apps/configuration).

## Update the catalog

The catalog update loop is intentionally small:

1. Run `MiddenCli validate` against the metadata source.
2. Run `MiddenCli collate --silent` and require a zero exit code.
3. Review the generated catalog for unexpected omissions or sensitive metadata.
4. Replace the deployed `catalog.json` without changing `app-config.json` or application files.
5. Open the raw catalog URL and a representative catalog page.

The application appends a unique query value when requesting both configuration and catalog
files, and the published service worker currently performs no offline caching. A CDN or hosting
platform may still cache responses according to its own policy. If an update remains stale,
purge that host cache and compare the raw deployed file with the generated file.

## Post-deployment checks

Use a private browsing window so an existing local session cannot politely conceal a problem:

1. Open `/` and confirm the configured organization and tool names appear.
2. Open `/editor/dataset` directly and refresh it; the SPA fallback should return the editor.
3. Open `/catalog`, a dataset, a project, and global search.
4. In browser developer tools, confirm `app-config.json`, `catalog.json`, `.wasm`, and framework
   requests return successful responses from the expected base path.
5. Download a metadata file from the editor and validate it with the CLI.
6. Check narrow and wide layouts and complete a keyboard-only pass through the primary routes.

Record the host, base path, configuration source, catalog-update owner, and last successful check
in the organization's operational notes. The repository documents how deployment works; each
installation still needs a human who knows that it exists.

## Roll back

Keep the previously deployed static artifact or host deployment available. If a release fails
the checks above, restore the complete prior artifact rather than mixing old framework files with
new ones. If only catalog content is wrong and the application is healthy, restoring the prior
`catalog.json` is sufficient.

For symptoms and fixes, see [Troubleshooting](troubleshooting.md).
