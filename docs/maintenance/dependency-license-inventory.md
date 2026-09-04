# Dependency License Inventory

| | |
|---|---|
| **Reviewed** | 2026-09-04 |
| **Scope** | Self-contained CLI archives and published Wasm application |
| **Source** | Restored NuGet assets, package metadata, and published web assets |

This is a release-focused inventory, not a legal opinion. It records the resolved production
dependencies and notices reviewed for Phase 5. Re-review it before a release when dependency
versions, runtime targets, or bundled browser assets change.

Test-only packages, analyzers, SDK packs, ILLink build tasks, the Wasm development server, and
GitHub Actions do not ship in Midden release output and are excluded from artifact notices.

## CLI archive

### Direct packages

| Package | Version | License |
|---|---:|---|
| Azure.Identity | 1.21.0 | MIT |
| Azure.Storage.Files.DataLake | 12.27.1 | MIT |
| Azure.Storage.Files.Shares | 12.27.1 | MIT |
| Google.Apis.Drive.v3 | 1.75.0.4218 | Apache-2.0 |
| System.CommandLine | 2.0.10 | MIT |
| System.Security.Cryptography.ProtectedData | 10.0.10 | MIT |

`Caf.Midden.Core` also contributes CsvHelper 33.1.0, used under its Apache-2.0 option.

### Transitive packages

| Package | Version | License |
|---|---:|---|
| Azure.Core | 1.55.0 | MIT |
| Azure.Storage.Blobs | 12.29.1 | MIT |
| Azure.Storage.Common | 12.28.0 | MIT |
| Google.Apis | 1.75.0 | Apache-2.0 |
| Google.Apis.Auth | 1.75.0 | Apache-2.0 |
| Google.Apis.Core | 1.75.0 | Apache-2.0 |
| Microsoft.Bcl.AsyncInterfaces | 10.0.3 | MIT |
| Microsoft.Extensions.Configuration.Abstractions | 10.0.3 | MIT |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.3 | MIT |
| Microsoft.Extensions.Diagnostics.Abstractions | 10.0.3 | MIT |
| Microsoft.Extensions.FileProviders.Abstractions | 10.0.3 | MIT |
| Microsoft.Extensions.Hosting.Abstractions | 10.0.3 | MIT |
| Microsoft.Extensions.Logging.Abstractions | 10.0.3 | MIT |
| Microsoft.Extensions.Options | 10.0.3 | MIT |
| Microsoft.Extensions.Primitives | 10.0.3 | MIT |
| Microsoft.Identity.Client | 4.83.1 | MIT |
| Microsoft.Identity.Client.Extensions.Msal | 4.83.1 | MIT |
| Microsoft.IdentityModel.Abstractions | 8.14.0 | MIT |
| Newtonsoft.Json | 13.0.4 | MIT |
| System.ClientModel | 1.11.0 | MIT |
| System.CodeDom | 7.0.0 | MIT |
| System.IO.Hashing | 10.0.3 | MIT |
| System.Management | 7.0.2 | MIT |
| System.Memory.Data | 10.0.3 | MIT |

Each CLI archive also embeds the RID-specific Microsoft .NET runtime. The release workflow copies
`LICENSE.TXT` and `THIRD-PARTY-NOTICES.TXT` from that exact restored runtime pack into the archive.
The Windows rehearsal resolved runtime pack 10.0.11; runtime-pack servicing versions can differ
from the 10.0.10 .NET NuGet reference versions elsewhere in this inventory.

## Published Wasm application

### Direct packages

| Package | Version | License |
|---|---:|---|
| AntDesign | 1.6.2 | MIT |
| Markdig | 1.3.2 | BSD-2-Clause |
| Microsoft.AspNetCore.Components.WebAssembly | 10.0.10 | MIT |
| PSC.Blazor.Components.MarkdownEditor | 10.0.9 | MIT |
| Radzen.Blazor | 11.2.3 | MIT |
| Z.Blazor.Diagrams | 3.0.4.1 | MIT |

The Wasm application references `Caf.Midden.Core`, which contributes CsvHelper 33.1.0 under its
Apache-2.0 option.

### Runtime transitive packages

| Package | Version | License |
|---|---:|---|
| Microsoft.AspNetCore.Authorization | 10.0.10 | MIT |
| Microsoft.AspNetCore.Components | 10.0.10 | MIT |
| Microsoft.AspNetCore.Components.Forms | 10.0.10 | MIT |
| Microsoft.AspNetCore.Components.Web | 10.0.10 | MIT |
| Microsoft.AspNetCore.Metadata | 10.0.10 | MIT |
| Microsoft.Extensions.Configuration | 10.0.10 | MIT |
| Microsoft.Extensions.Configuration.Abstractions | 10.0.10 | MIT |
| Microsoft.Extensions.Configuration.Binder | 10.0.10 | MIT |
| Microsoft.Extensions.Configuration.FileExtensions | 10.0.10 | MIT |
| Microsoft.Extensions.Configuration.Json | 10.0.10 | MIT |
| Microsoft.Extensions.DependencyInjection | 10.0.10 | MIT |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.10 | MIT |
| Microsoft.Extensions.Diagnostics | 10.0.10 | MIT |
| Microsoft.Extensions.Diagnostics.Abstractions | 10.0.10 | MIT |
| Microsoft.Extensions.FileProviders.Abstractions | 10.0.10 | MIT |
| Microsoft.Extensions.FileProviders.Physical | 10.0.10 | MIT |
| Microsoft.Extensions.FileSystemGlobbing | 10.0.10 | MIT |
| Microsoft.Extensions.Logging | 10.0.10 | MIT |
| Microsoft.Extensions.Logging.Abstractions | 10.0.10 | MIT |
| Microsoft.Extensions.Options | 10.0.10 | MIT |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.10 | MIT |
| Microsoft.Extensions.Primitives | 10.0.10 | MIT |
| Microsoft.Extensions.Validation | 10.0.10 | MIT |
| Microsoft.JSInterop | 10.0.10 | MIT |
| Microsoft.JSInterop.WebAssembly | 10.0.10 | MIT |
| OneOf | 3.0.271 | MIT |
| SvgPathProperties | 1.1.2 | MIT |
| Z.Blazor.Diagrams.Core | 3.0.4.1 | MIT |

### Browser assets

| Component | Version | Delivery | License |
|---|---:|---|---|
| Bootstrap | 4.3.1 | Self-hosted static asset | MIT |
| EasyMDE fork | 2.0.x, bundled by MarkdownEditor 10.0.9 | Published static asset | MIT |
| Mermaid | 11.12.0, bundled by MarkdownEditor 10.0.9 | Published optional static asset | MIT |
| highlight.js | 11.11.1, bundled by MarkdownEditor 10.0.9 | Published optional static asset | BSD-3-Clause |
| Leaflet | 1.7.1 | Self-hosted static asset | BSD-2-Clause |
| Leaflet-Geoman Free | 2.20.0 | Self-hosted static asset | MIT |
| Leaflet.heat | 0.2.0 | Self-hosted static asset | BSD-2-Clause |
| Open Iconic icons | 1.1.1 | Vendored static asset | MIT |
| Open Iconic font | 1.1.1 | Vendored static asset | SIL-OFL-1.1 |

The Leaflet, Leaflet-Geoman Free, Leaflet.heat, and Open Iconic license texts are distributed
beside their static assets. Project-level `LICENSE.md`, `NOTICE.md`, and
`THIRD-PARTY-NOTICES.md` are published under the Wasm application's `wwwroot/legal` directory.
Mermaid and highlight.js are copied into published output by the Markdown editor package but are
loaded only when their corresponding editor options are enabled; Midden does not currently enable
those options.

## Upstream sources

| Component family | Source or project page |
|---|---|
| Microsoft .NET, ASP.NET Core, and Extensions | <https://github.com/dotnet> |
| Azure SDK for .NET | <https://github.com/Azure/azure-sdk-for-net> |
| Google APIs Client Library for .NET | <https://github.com/googleapis/google-api-dotnet-client> |
| CsvHelper | <https://github.com/JoshClose/CsvHelper> |
| Newtonsoft.Json | <https://github.com/JamesNK/Newtonsoft.Json> |
| System.CommandLine | <https://github.com/dotnet/command-line-api> |
| Ant Design Blazor | <https://github.com/ant-design-blazor/ant-design-blazor> |
| Markdig | <https://github.com/xoofx/markdig> |
| OneOf | <https://github.com/mcintyre321/OneOf> |
| PSC Markdown Editor | <https://github.com/erossini/BlazorMarkdownEditor> |
| Radzen Blazor | <https://github.com/radzenhq/radzen-blazor> |
| Z.Blazor.Diagrams and SvgPathProperties | <https://github.com/Blazor-Diagrams/Blazor.Diagrams> |
| Bootstrap | <https://github.com/twbs/bootstrap> |
| Leaflet | <https://github.com/Leaflet/Leaflet> |
| Leaflet-Geoman Free | <https://github.com/geoman-io/leaflet-geoman> |
| Leaflet.heat | <https://github.com/Leaflet/Leaflet.heat> |
| Leaflet.heat | <https://github.com/Leaflet/Leaflet.heat> |
| EasyMDE fork | <https://github.com/erossini/EasyMarkdownEditor> |
| Mermaid | <https://github.com/mermaid-js/mermaid> |
| highlight.js | <https://github.com/highlightjs/highlight.js> |
| Open Iconic | <https://github.com/iconic/open-iconic> |

## Notice decision

- Include `LICENSE.md`, `NOTICE.md`, and `THIRD-PARTY-NOTICES.md` in every CLI archive.
- Include the exact .NET runtime pack license and third-party notice as
  `DOTNET-LICENSE.txt` and `DOTNET-THIRD-PARTY-NOTICES.txt` in every CLI archive.
- Retain the Open Iconic license files in published Wasm output.
- No copyleft dependency requiring source redistribution was found in production output.
- The MPL-2.0 axe packages are development-only and do not ship in product artifacts.