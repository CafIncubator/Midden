using AntDesign;
using Caf.Midden.Core.Models.v0_1;
using Caf.Midden.Wasm.Shared.Modals;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class CatalogMetadataViewer : IDisposable
    {
        public List<Metadata> FilteredMetadata { get; set; } = new List<Metadata>();

        public string SearchTerm { get; set; }

        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property)
                => await StateChanged(source, property);

            if (State?.Catalog != null)
                FilteredMetadata = State?.Catalog?.Metadatas;
        }

        private async Task StateChanged(
            ComponentBase source,
            string property)
        {
            if(source != this)
            {
                if (property == "UpdateCatalog")
                {
                    FilteredMetadata = State?.Catalog?.Metadatas;
                }

                await InvokeAsync(StateHasChanged);
            }

            
        }

        private void SearchHandler()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                FilteredMetadata = State.Catalog.Metadatas;
            }
            else
            {
                FilteredMetadata = State.Catalog.Metadatas
                    .Where(m =>
                        (m.Dataset.Project.ToLower().Contains(
                            SearchTerm.ToLower())) ||
                        (m.Dataset.Name.ToLower().Contains(
                            SearchTerm.ToLower())) ||
                        (m.Dataset.Description.ToLower().Contains(
                            SearchTerm.ToLower())) ||
                        (m.Dataset.Tags.Any(t => t.ToLower().Contains(
                            SearchTerm.ToLower()))))
                    .ToList();
            }
        }

        private ModalRef metadataDetailsModalRef;
        private async Task OpenMetadataDetailsModalTemplate(Metadata metadata)
        {
            var templateOptions = new ViewModels.MetadataDetailsViewModel
            {
                Metadata = metadata
            };

            var modalConfig = new ModalOptions();
            modalConfig.Title = "Metadata Preview";
            modalConfig.Width = "90%";
            modalConfig.DestroyOnClose = true;
            modalConfig.OnCancel = async (e) =>
            {
                await metadataDetailsModalRef.CloseAsync();
            };
            modalConfig.OnOk = async (e) =>
            {
                await metadataDetailsModalRef.CloseAsync();
            };

            modalConfig.AfterClose = () =>
            {
                InvokeAsync(StateHasChanged);

                return Task.CompletedTask;
            };

            metadataDetailsModalRef = await ModalService
                .CreateModalAsync<MetadataDetailsModal, ViewModels.MetadataDetailsViewModel>(
                    modalConfig, templateOptions);
        }

        public void Dispose()
        {
            State.StateChanged -= async (source, property)
                => await StateChanged(source, property);
        }
    }
}
