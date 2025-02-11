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

        [Parameter]
        public bool ShowSearch { get; set; } = true;

        [Parameter]
        public bool ShowHeader { get; set; } = true;

        [Parameter]
        public int ShowRecentNumber { get; set; } = 0;

        EmbeddedProperty Property(int span, int offset) => new() { Span = span, Offset = offset };


        public List<Metadata> BaseMetadatas { get; set; } = new List<Metadata>();
        public List<Metadata> FilteredMetadata { get; set; } = new List<Metadata>();

        public string SearchTerm { get; set; } = String.Empty;
        private string _selectedZone = String.Empty;
        private string SelectedZone
        {
            get => _selectedZone;
            set
            {
                if (_selectedZone != value)
                {
                    _selectedZone = value;
                    Console.WriteLine($"Zone selected: {_selectedZone}");  // Debug output
                    //ApplyZoneFilter();
                }
            }
        }

        private MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseYamlFrontMatter()
            .Build();

        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property) => await StateChanged(source, property);

            if (State?.AppConfig?.Zones != null && State.AppConfig.Zones.Any())
            {
                Console.WriteLine($"Zones Loaded: {string.Join(", ", State.AppConfig.Zones)}");
            }
            else
            {
                Console.WriteLine("Zones are empty or AppConfig is null.");
            }

            if (State?.Catalog != null)
            {
                SetBaseMetadatas();
                FilteredMetadata = this.BaseMetadatas;
            }
        }



        private async Task StateChanged(ComponentBase source, string property)
        {
            if (source != this)
            {
                if (property == "UpdateCatalog" || property == "UpdateAppConfig")
                {
                    SetBaseMetadatas();
                    FilteredMetadata = this.BaseMetadatas;
                }

                await InvokeAsync(StateHasChanged); // Force UI to update after state changes
            }
        }


        private void SetBaseMetadatas()
        {
            List<Metadata> metas = State.Catalog.Metadatas
                .Where(m =>
                    (String.IsNullOrEmpty(this.Zone) || m.Dataset.Zone.ToLower() == this.Zone.ToLower()) &&
                    (String.IsNullOrEmpty(this.Project) || m.Dataset.Project.ToLower() == this.Project.ToLower()) &&
                    (String.IsNullOrEmpty(this.Tag) || m.Dataset.Tags.Any(t => t.ToLower() == this.Tag.ToLower())))
                .OrderByDescending(m => m.Dataset.LastUpdate)
                .ToList();

            Console.WriteLine($"Total datasets loaded: {metas.Count}");  // Debug statement

            if (metas != null && metas.Count > 0 && ShowRecentNumber > 0)
            {
                int toTake = ShowRecentNumber;
                if (metas.Count < toTake) toTake = metas.Count;

                this.BaseMetadatas = metas.GetRange(0, toTake);
            }
            else
            {
                this.BaseMetadatas = metas;
            }

            // Debug to confirm dataset zones
            foreach (var metadata in BaseMetadatas)
            {
                Console.WriteLine($"Dataset loaded: {metadata.Dataset.Name}, Zone: {metadata.Dataset.Zone}");
            }
        }



        private void SearchHandler()
        {
            Console.WriteLine($"Search initiated with term: {SearchTerm} and zone: {SelectedZone}");

            var filtered = BaseMetadatas;

            // Apply search filter
            if (!String.IsNullOrWhiteSpace(SearchTerm))
            {
                filtered = filtered
                    .Where(m =>
                        m.Dataset.Name.ToLower().Contains(SearchTerm.ToLower()) ||
                        m.Dataset.Description.ToLower().Contains(SearchTerm.ToLower()) ||
                        m.Dataset.Tags.Any(t => t.ToLower().Contains(SearchTerm.ToLower())))
                    .ToList();
            }

            // Apply zone filter
            if (!String.IsNullOrEmpty(SelectedZone))
            {
                filtered = filtered
                    .Where(m => m.Dataset.Zone.Equals(SelectedZone, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            FilteredMetadata = filtered.OrderByDescending(m => m.Dataset.LastUpdate).ToList();

            Console.WriteLine($"Total datasets after combined filtering: {FilteredMetadata.Count}");
            InvokeAsync(StateHasChanged); // Ensure UI updates
        }

        private void OnZoneFilterChange()
        {
            SearchHandler();

            // Force UI to refresh
            InvokeAsync(() =>
            {
                StateHasChanged();
                Console.WriteLine("UI refreshed after filtering.");
            });
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
