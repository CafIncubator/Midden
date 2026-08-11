using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Caf.Midden.Wasm;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Configuration;
using Caf.Midden.Wasm.Services;
using Radzen;


var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

// HttpClient scoped to the WASM base address
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// App services
builder.Services.AddScoped<StateContainer>();
builder.Services.AddScoped<AppBootstrapService>();
builder.Services.AddScoped<AutosaveService>();
builder.Services.AddSingleton<CatalogInsightsService>();
builder.Services.AddSingleton<CatalogSearchService>();

builder.Services.AddScoped<IReadConfiguration>(sp =>
    new ConfigurationReaderHttp(
        sp.GetRequiredService<HttpClient>(),
        "app-config.json"));

builder.Services.AddScoped<IReadCatalog>(sp =>
    new CatalogReaderHttp(
        sp.GetRequiredService<HttpClient>()));

// UI framework
builder.Services.AddAntDesign();

// Radzen supplies the dashboard charts: it renders SVG directly from Blazor, so axis
// formatting is plain C# and the plot tracks its container without a JS measurement step.
builder.Services.AddRadzenComponents();

await builder.Build().RunAsync();

