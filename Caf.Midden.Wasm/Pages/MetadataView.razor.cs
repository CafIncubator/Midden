using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Wasm.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Pages
{
    public partial class MetadataView : IDisposable
    {
        private IDisposable? _stateSubscription;

        [Parameter]
        public string ZoneName { get; set; }

        [Parameter]
        public string ProjectName { get; set; }

        [Parameter]
        public string DatasetName { get; set; }

        Metadata Metadata { get; set; }

        protected override void OnInitialized()
        {
            _stateSubscription = State.Subscribe(
                this,
                OnStateChanged,
                AppStateChange.Catalog);

            if (State?.Catalog != null)
                SetMetadata();
        }

        private async Task OnStateChanged(AppStateChangedEventArgs args)
        {
            SetMetadata();
            await InvokeAsync(StateHasChanged);
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
            _stateSubscription?.Dispose();
        }

        public void EditMetadata()
        {
            State.SetMetadataEdit(this.Metadata, this);
            NavManager.NavigateTo("editor/dataset");
        }
    }
}
