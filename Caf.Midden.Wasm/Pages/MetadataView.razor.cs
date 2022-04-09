using Caf.Midden.Core.Models.v0_2;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Pages
{
    public partial class MetadataView : IDisposable
    {
        [Parameter]
        public string ZoneName { get; set; }

        [Parameter]
        public string ProjectName { get; set; }

        [Parameter]
        public string DatasetName { get; set; }

        Metadata Metadata { get; set; }

        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property)
                => await StateChanged(source, property);

            if (State?.Catalog != null)
                SetMetadata();
        }

        private async Task StateChanged(
            ComponentBase source,
            string property)
        {
            if (source != this)
            {
                if (property == "UpdateCatalog")
                {
                    SetMetadata();
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        private void SetMetadata()
        {

            if(State != null && State.Catalog != null && State.Catalog.Metadatas != null)
            {
                var metadata = State.Catalog.Metadatas.FirstOrDefault(m =>
                    (m.Dataset.Zone == this.ZoneName) && 
                    (m.Dataset.Project == this.ProjectName) && 
                    (m.Dataset.Name == this.DatasetName));

                if(metadata != null)
                    this.Metadata = metadata;
            }
        }

        public void Dispose()
        {
            State.StateChanged -= async (source, property)
                => await StateChanged(source, property);
        }

        public void EditMetadata()
        {
            State.MetadataEdit = this.Metadata;
            NavManager.NavigateTo("editor/dataset");
        }
    }
}
