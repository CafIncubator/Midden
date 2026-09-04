using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Services;

public sealed class AppBootstrapService : IAsyncDisposable
{
    private readonly IReadConfiguration _configurationReader;
    private readonly IReadCatalog _catalogReader;
    private readonly StateContainer _state;
    private readonly SemaphoreSlim _configurationLock = new(1, 1);
    private readonly SemaphoreSlim _catalogLock = new(1, 1);

    public AppBootstrapService(
        IReadConfiguration configurationReader,
        IReadCatalog catalogReader,
        StateContainer state)
    {
        _configurationReader = configurationReader;
        _catalogReader = catalogReader;
        _state = state;
    }

    public async Task EnsureConfigurationLoadedAsync(object? source = null)
    {
        if (_state.AppConfig is not null)
        {
            return;
        }

        await _configurationLock.WaitAsync();
        try
        {
            if (_state.AppConfig is not null)
            {
                return;
            }

            Configuration config = await _configurationReader.Read();
            _state.SetAppConfig(config, source);
        }
        finally
        {
            _configurationLock.Release();
        }
    }

    public async Task EnsureCatalogLoadedAsync(object? source = null)
    {
        await EnsureConfigurationLoadedAsync(source);

        if (_state.AppConfig is null || _state.Catalog.Metadatas.Count > 0)
        {
            return;
        }

        await _catalogLock.WaitAsync();
        try
        {
            if (_state.AppConfig is null || _state.Catalog.Metadatas.Count > 0)
            {
                return;
            }

            Catalog catalog = await _catalogReader.Read(
                _state.AppConfig.CatalogPath,
                true);

            catalog.Metadatas = catalog.Metadatas
                .OrderByDescending(metadata => metadata.ModifiedDate)
                .ToList();

            _state.SetCatalog(catalog, source);
        }
        finally
        {
            _catalogLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _configurationLock.Dispose();
        _catalogLock.Dispose();
        return ValueTask.CompletedTask;
    }
}