using AntDesign;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Wasm.Shared.Modals;
using Caf.Midden.Wasm.Shared.ViewModels;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class CatalogVariableViewer : IDisposable
    {
        [Parameter]
        public string Project { get; set; }

        [Parameter]
        public string TagName { get; set; }

        CatalogVariableViewerViewModel ViewModel { get; set; } = new CatalogVariableViewerViewModel();
        public TableFilter<string>[] FilterProcessing;
        public TableFilter<string>[] FilterZone;

        EmbeddedProperty Property(int span, int offset) => new() { Span = span, Offset = offset };

        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property)
                => await StateChanged(source, property);

            if (State?.Catalog != null)
                SetCatalogVariables(State?.Catalog?.Metadatas);

            if (State?.AppConfig != null)
                SetFilters(State?.AppConfig);
        }

        private async Task StateChanged(
            ComponentBase source,
            string property)
        {
            if (source != this)
            {
                if (property == "UpdateCatalog")
                {
                    SetCatalogVariables(State?.Catalog?.Metadatas);
                    SetFilters(State?.AppConfig);
                }

                await InvokeAsync(StateHasChanged);
            }


        }

        private void SetFilters(Configuration appConfig)
        {
            if (appConfig == null)
                return;

            List<TableFilter<string>> processings = new List<TableFilter<string>>();
            foreach(var processing in appConfig.ProcessingLevels)
            {
                processings.Add(new TableFilter<string> { Text = processing, Value = processing });
            }
            this.FilterProcessing = processings.ToArray();

            List<TableFilter<string>> zones = new List<TableFilter<string>>();
            foreach(var zone in appConfig.Zones)
            {
                zones.Add(new TableFilter<string> { Text = zone, Value = zone });
            }
            this.FilterZone = zones.ToArray();
        }

        private void SetCatalogVariables(List<Metadata> metadatas)
        {
            if (metadatas == null)
                return;

            List<CatalogVariable> catalogVariables = new List<CatalogVariable>();

            foreach(var metadata in metadatas)
            {
                if ((metadata.Dataset != null) && 
                    (metadata.Dataset.Variables != null) && 
                    (string.IsNullOrEmpty(this.Project) || 
                        metadata.Dataset.Project.ToLower().Trim() == this.Project.ToLower().Trim()))
                {
                    foreach (var variable in metadata.Dataset.Variables)
                    {
                        if (string.IsNullOrEmpty(this.TagName))
                        {
                            catalogVariables.Add(new CatalogVariable()
                            {
                                Name = variable.Name,
                                Description = variable.Description,
                                Units = variable.Units,
                                Tags = new List<string>(variable.Tags),
                                Methods = new List<string>(variable.Methods),
                                TemporalResolution = variable.TemporalResolution,
                                TemporalExtent = variable.TemporalExtent,
                                QCApplied = variable.QCApplied,
                                ProcessingLevel = variable.ProcessingLevel,
                                Zone = metadata.Dataset.Zone,
                                ProjectName = metadata.Dataset.Project,
                                DatasetName = metadata.Dataset.Name
                            });
                        }
                        else if (!string.IsNullOrEmpty(this.TagName) && variable.Tags.Contains(this.TagName))
                        {
                            catalogVariables.Add(new CatalogVariable()
                            {
                                Name = variable.Name,
                                Description = variable.Description,
                                Units = variable.Units,
                                Tags = new List<string>(variable.Tags),
                                Methods = new List<string>(variable.Methods),
                                TemporalResolution = variable.TemporalResolution,
                                TemporalExtent = variable.TemporalExtent,
                                QCApplied = variable.QCApplied,
                                ProcessingLevel = variable.ProcessingLevel,
                                Zone = metadata.Dataset.Zone,
                                ProjectName = metadata.Dataset.Project,
                                DatasetName = metadata.Dataset.Name
                            });
                        }
                    }
                }
            }

            ViewModel.CatalogVariables = new List<CatalogVariable>(catalogVariables);
            ViewModel.FilteredCatalogVariables = ViewModel.CatalogVariables;
        }

        private void SearchHandler()
        {
            if (string.IsNullOrWhiteSpace(ViewModel.SearchTerm))
            {
                ViewModel.FilteredCatalogVariables = ViewModel.CatalogVariables;
            }
            else
            {
                ViewModel.FilteredCatalogVariables = ViewModel.CatalogVariables
                    .Where(c =>
                        (c.DatasetName.ToLower().Contains(
                            ViewModel.SearchTerm.ToLower())) ||
                        (c.Name.ToLower().Contains(
                            ViewModel.SearchTerm.ToLower())) ||
                        (c.Description.ToLower().Contains(
                            ViewModel.SearchTerm.ToLower())) ||
                        (c.Units.ToLower().Contains(
                            ViewModel.SearchTerm.ToLower())) ||
                        (c.Tags.Any(t => t.ToLower().Contains(
                            ViewModel.SearchTerm.ToLower()))))
                    .ToList();
            }
        }

        private ModalRef metadataDetailsModalRef;
        private async Task OpenMetadataDetailsModalTemplate(CatalogVariable catalogVariable)
        {
            var metadata = State.Catalog.Metadatas.FirstOrDefault(m =>
                (m.Dataset.Zone == catalogVariable.Zone) &&
                (m.Dataset.Project == catalogVariable.ProjectName) &&
                (m.Dataset.Name == catalogVariable.DatasetName));

            if (metadata == null)
                return;

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
