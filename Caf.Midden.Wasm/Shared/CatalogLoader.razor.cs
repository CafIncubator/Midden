using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Wasm.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class CatalogLoader : IDisposable
    {
        [Inject]
        public AppBootstrapService Bootstrapper { get; set; } = default!;

        private IDisposable? _stateSubscription;

        protected override async Task OnInitializedAsync()
        {
            _stateSubscription = State.Subscribe(
                this,
                OnStateChanged,
                AppStateChange.AppConfig);

            await Bootstrapper.EnsureCatalogLoadedAsync(this);
        }

        private async Task OnStateChanged(AppStateChangedEventArgs args)
        {
            await Bootstrapper.EnsureCatalogLoadedAsync(this);
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadCatalog()
        {
            await Bootstrapper.EnsureCatalogLoadedAsync(this);
        }

        public void Dispose()
        {
            _stateSubscription?.Dispose();
        }
    }
}
