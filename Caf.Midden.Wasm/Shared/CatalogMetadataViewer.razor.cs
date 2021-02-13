using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class CatalogMetadataViewer : IDisposable
    {
        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property)
                => await StateChanged(source, property);
        }

        private async Task StateChanged(
            ComponentBase source,
            string property)
        {
            if(source != this)
            {
                await InvokeAsync(StateHasChanged);
            }
        }

        public void Dispose()
        {
            State.StateChanged -= async (source, property)
                => await StateChanged(source, property);
        }
    }
}
