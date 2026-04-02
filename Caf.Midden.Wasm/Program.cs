using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Caf.Midden.Wasm;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Configuration;
using Caf.Midden.Wasm.Services;


var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

// HttpClient scoped to the WASM base address
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// App services
builder.Services.AddScoped<StateContainer>();
builder.Services.AddScoped<AppBootstrapService>();
builder.Services.AddSingleton<CatalogInsightsService>();

builder.Services.AddScoped<IReadConfiguration>(sp =>
    new ConfigurationReaderHttp(
        sp.GetRequiredService<HttpClient>(),
        "app-config.json"));

builder.Services.AddScoped<IReadCatalog>(sp =>
    new CatalogReaderHttp(
        sp.GetRequiredService<HttpClient>()));

// UI framework
builder.Services.AddAntDesign();

await builder.Build().RunAsync();

