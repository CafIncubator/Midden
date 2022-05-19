using AntDesign;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Wasm.Shared.Modals;
using Markdig;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class FilteredCatalogMetadataViewer : IDisposable
    {
        [Parameter]
        public string Zone { get; set; }

        [Parameter]
        public string Project { get; set; }

        [Parameter]
        public string Tag { get; set; }


        public List<Metadata> BaseMetadatas { get; set; } = new List<Metadata>();
        public List<Metadata> FilteredMetadata { get; set; } = new List<Metadata>();

        public string SearchTerm { get; set; }

        private MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseYamlFrontMatter()
            .Build();

        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property)
                => await StateChanged(source, property);

            if (State?.Catalog != null)
            {
                SetBaseMetadatas();
                FilteredMetadata = this.BaseMetadatas;
            }
        }

        private async Task StateChanged(
            ComponentBase source,
            string property)
        {
            if (source != this)
            {
                if (property == "UpdateCatalog")
                {
                    SetBaseMetadatas();
                    FilteredMetadata = this.BaseMetadatas;
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        private void SetBaseMetadatas()
        {
            BaseMetadatas = State.Catalog.Metadatas
                .Where(m =>
                    (String.IsNullOrEmpty(this.Zone) || m.Dataset.Zone.ToLower() == this.Zone.ToLower()) &&
                    (String.IsNullOrEmpty(this.Project) || m.Dataset.Project.ToLower() == this.Project.ToLower()) &&
                    (String.IsNullOrEmpty(this.Tag) || m.Dataset.Tags.Any(t => t.ToLower() == this.Tag.ToLower())))
                .OrderByDescending(m => m.Dataset.LastUpdate)
                .ToList();

        }
        private void SearchHandler()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                FilteredMetadata = this.BaseMetadatas;
            }
            else
            {
                FilteredMetadata = this.BaseMetadatas
                    .Where(m =>
                        (m.Dataset.Name.ToLower().Contains(
                            SearchTerm.ToLower())) ||
                        (m.Dataset.Description.ToLower().Contains(
                            SearchTerm.ToLower())) ||
                        (m.Dataset.Tags.Any(t => t.ToLower().Contains(
                            SearchTerm.ToLower()))))
                    .OrderByDescending(m => m.Dataset.LastUpdate)
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
