using Caf.Midden.Core.Models.v0_1_0alpha4;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class CatalogLoader
    {
        protected override async Task OnInitializedAsync()
        {
            if (State?.Catalog != null)
                return;

            Catalog catalog = await CatalogReader.Read(
                State.AppConfig.CatalogPath);

            State.UpdateCatalog(this, catalog);
        }
    }
}
