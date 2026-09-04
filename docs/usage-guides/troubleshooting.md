# Troubleshooting

Start with the surface that is misbehaving. This page routes common operational problems; the
[CLI guide](cli-usage.md#troubleshooting) remains the detailed source for command-line errors.

## CLI problems

The CLI guide covers missing configuration, secret-store errors, Google and Azure sign-in,
malformed JSON, partial crawl failures, duplicate paths, missing datasets, and validation
messages. If a command failed, keep its exit code and sanitized error text when opening an issue.
Do not attach credentials, token caches, `secrets.midden`, or a configuration containing literal
secrets. A bug report should be interesting, not incident-response interesting.

## The site remains on Loading

Open browser developer tools and inspect the Console and Network tabs.

- If `_framework` or `.wasm` requests return 404, check the `<base href>` and confirm the contents
  of the published `wwwroot` directory, rather than its parent, were deployed.
- If `.wasm` downloads have the wrong content type, configure the host to serve WebAssembly static
  assets correctly.
- If a script or stylesheet is blocked, check content-security policy and the external Leaflet,
  Ant Design, Radzen, and editor assets reported in the console.

## A direct route returns 404

The host is missing its single-page application fallback. Follow the host-specific section in
[Deploy Midden](deployment.md#configure-spa-fallback), then test by opening and refreshing
`/editor/dataset` directly.

## The catalog is empty or cannot load

1. Open `app-config.json` from the deployed site and find `catalogPath`.
2. Resolve that value relative to the application's base URL and open it directly.
3. Confirm it returns JSON rather than `index.html`, a sign-in page, or a 404 response.
4. Remove any leading `/` from a relative `catalogPath` for a subpath deployment.
5. For an absolute URL on another origin, inspect the response for a CORS
  `Access-Control-Allow-Origin` header that permits the Midden site.
6. Run the CLI validation and collation commands again and require a zero exit code.

An over-broad SPA fallback can turn a missing `catalog.json` into a successful `200` response
containing HTML. The status code looks delighted; the JSON parser is less convinced.

## The catalog is stale

Midden adds a changing query value to configuration and catalog requests, and offline caching is
currently disabled in its published service worker. Compare the raw hosted `catalog.json` with
the newly generated file. If they differ, replace the hosted file or purge the hosting platform's
CDN cache. If they match, reload the page and inspect the catalog request in the Network tab.

## The wrong organization or vocabulary appears

Confirm the deployment contains the intended `app-config.json`, not the repository default or a
file copied from another environment. Check the requested URL in the Network tab when multiple
sites share a domain. Then validate the file against the
[configuration reference](configuration.md#application-configuration).

## GitHub Pages works only at the home page

For a project site, `<base href>` must include the repository name with leading and trailing
slashes. The deployed site also needs a `404.html` copy of the configured `index.html` so direct
client routes can start Blazor.

## Ask for help

Use the support channels in [SUPPORT.md](../../SUPPORT.md). Include:

- the Midden version or commit;
- operating system, browser, and hosting platform;
- the command or route that failed;
- sanitized output, HTTP status codes, and relevant console messages; and
- the smallest configuration or metadata example that reproduces the problem.

Report suspected vulnerabilities privately as described in [SECURITY.md](../../SECURITY.md), not
in a public troubleshooting issue.