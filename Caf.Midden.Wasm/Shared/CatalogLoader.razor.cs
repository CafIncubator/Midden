using Caf.Midden.Core.Models.v0_1;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class CatalogLoader : IDisposable
    {
        protected override async Task OnInitializedAsync()
        {
            State.StateChanged += async (source, property)
                => await StateChanged(source, property);

            if (State?.AppConfig != null)
                await LoadCatalog();
        }

        private async Task StateChanged(
            ComponentBase source,
            string property)
        {
            if (source != this)
            {
                if (property == "UpdateAppConfig")
                {
                    await LoadCatalog();
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task LoadCatalog()
        {
            if ((State?.Catalog != null) && (State.Catalog.Metadatas.Count > 0))
                return;

            Catalog catalog = await CatalogReader.Read(
                State.AppConfig.CatalogPath,
                true);

            catalog.Metadatas = catalog.Metadatas
                .OrderByDescending(m => m.ModifiedDate).ToList();

            State.UpdateCatalog(this, catalog);
        }

        public void Dispose()
        {
            State.StateChanged -= async (source, property)
                => await StateChanged(source, property);
        }
    }
}
