# Configuration reference

Midden has two configuration files with different jobs:

| File | Read by | Purpose | Publish it? |
|---|---|---|---|
| `configuration.json` | Midden CLI | Selects data stores and their connection settings | No |
| `app-config.json` | Wasm application and CLI validation | Names the organization, catalog, and controlled vocabularies used by the editor | Yes |

They are not interchangeable, despite both being JSON and both being fond of lists. Never put a
password, token, connection string, or private endpoint in `app-config.json`; every visitor can
download that file from the static site.

## CLI configuration

The canonical operating instructions, data-store settings, and authentication examples are in
[the CLI usage guide](cli-usage.md#the-configuration-file). Start from
[`configuration.local.example.json`](../examples/configuration.local.example.json), copy it to a
working folder as `configuration.json`, and adjust the path.

```json
{
  "Version": "1.0.0",
  "DataStores": [
    {
      "Name": "ResearchProjects",
      "Type": "LocalFileSystem",
      "Path": "C:\\Research\\Projects",
      "ShouldCollateProjects": true
    }
  ]
}
```

`Version` is the format of `configuration.json`, not the CLI product version. Version `1.0.0` is
the only currently supported format. If the property is omitted, the CLI defaults it to `1.0.0`;
an explicitly unsupported value fails when configuration is loaded.

Keep this file out of source control. Prefer `secret:` references, environment variables,
managed identities, or provider login caches for credentials. The
[secret-handling guide](cli-usage.md#handling-passwords-and-secrets) explains each option without
asking anyone to paste a password into an issue, which would be an exciting but regrettable form
of community participation.

## Application configuration

The Wasm application loads `app-config.json` from its deployed base address. Start from
[`app-config.example.json`](../examples/app-config.example.json). The example is deliberately
small; the repository's deployed configuration contains longer controlled vocabularies and
geometries that are useful to its own organization but are not universal defaults.

You can also deploy the default `app-config.json`, open `/editor/app-configuration`, edit the
configuration in the browser, and select **Download**. The editor downloads a replacement named
`app-config.json`; it cannot write directly to a static host. Review the downloaded file, replace
the deployed copy, and redeploy it. The editor autosaves an in-browser draft along the way, which
is convenient but should not be mistaken for publication. Browsers are many things, but they are
not your release manager.

### Properties

| Property | Required | Meaning |
|---|---|---|
| `schemaVersion` | Yes | Application-configuration format identifier; `v0.2` is current and `v0.1` is accepted for legacy deployments |
| `isConfigured` | Yes | Whether initial organization setup has been completed |
| `organizationName` | Yes | Name shown for the organization |
| `toolName` | Yes | Name shown for this Midden installation |
| `catalogPath` | Yes | Relative request path or absolute HTTPS URL for the published catalog |
| `zones` | Practically | Dataset storage or lifecycle zones; at least one is needed to create valid metadata |
| `roles` | No | Contributor roles offered by the editor |
| `projectStatuses` | No | Project status vocabulary |
| `processingLevels` | No | Variable processing-level vocabulary |
| `variableTypes` | No | Variable type vocabulary |
| `geometries` | No | Named reusable GeoJSON geometries |
| `tags` | No | Dataset tag vocabulary |
| `datasetStructures` | No | Dataset structure vocabulary |
| `qualityControlTags` | No | Quality-control vocabulary |

The configuration validator requires non-blank `organizationName`, `toolName`, and `catalogPath`.
It also reports malformed geometries and warns about empty zones, duplicate vocabulary values,
rooted catalog paths, and catalog paths that do not look like JSON.

A relative value such as `catalog.json` or `data/catalog.json` is simplest and works at a site
root or beneath a base path. A full HTTPS URL such as
`https://catalogs.example.org/current/catalog.json` is also supported. If that URL has a different
origin from the Midden site, its server must permit the Midden origin with an appropriate CORS
`Access-Control-Allow-Origin` response header. Test this from the deployed site, since opening the
catalog URL directly does not exercise browser CORS enforcement.

Avoid a path such as `/catalog.json`: it is neither relative to the application's base path nor a
complete URL, so it can quietly leave a subpath deployment staring into the void.

### Geometries

Each geometry has a display `name` and a `geojson` string. The inner GeoJSON must itself be valid,
so its quotation marks are escaped inside the outer JSON document:

```json
{
  "name": "Study boundary",
  "geojson": "{\"type\":\"Polygon\",\"coordinates\":[[[-117.2,46.7],[-117.0,46.7],[-117.0,46.9],[-117.2,46.9],[-117.2,46.7]]]}"
}
```

Use a polygon or multipolygon accepted by Midden's geometry validator. A friendly name is what
researchers see in the editor; `Polygon 7 final FINAL` may be technically legal but is not a gift
to future colleagues.

## Compatibility and changes

`v0.2` is the current application-configuration format. Midden also accepts legacy `v0.1` files
and loads them into the current runtime model; fields introduced in `v0.2`, including
`projectStatuses` and `variableTypes`, default to empty lists when absent. Opening a legacy file
in the configuration editor and downloading it preserves its `v0.1` label until a maintainer
deliberately changes it after reviewing the newer fields.

Missing and unknown schema versions are rejected with a message listing the supported versions.
This prevents a configuration from a future Midden release from loading partially and looking
plausible enough to ruin an otherwise pleasant afternoon.

Do not interpret a product release as an automatic schema-version change. A future format change
must update the Core model and parser, validators, examples, fixtures, this reference, release
notes, and a migration procedure together. Removal of legacy `v0.1` support requires an announced
migration path.

## Validation checklist

Before deployment:

1. Parse both JSON files with a JSON parser; comments and trailing commas are not portable.
2. Run `MiddenCli validate <path> --app-config <path-to-app-config.json>` against representative
   metadata.
3. Confirm a relative `catalogPath` resolves beneath the deployment base path, or test an absolute
  URL from the deployed site and verify its CORS response.
4. Search the files for passwords, tokens, private keys, and private service URLs.
5. Open the editor and catalog after deployment and inspect the browser console for load errors.
