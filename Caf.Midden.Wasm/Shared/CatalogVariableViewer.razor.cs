using AntDesign;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Wasm.Shared.Modals;
using Caf.Midden.Wasm.Shared.ViewModels;
using Caf.Midden.Wasm.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class CatalogVariableViewer : IDisposable
    {
        private IDisposable? _stateSubscription;

        [Parameter]
        public string Project { get; set; }

        [Parameter]
        public string TagName { get; set; }

        CatalogVariableViewerViewModel ViewModel { get; set; } = new CatalogVariableViewerViewModel();
        public TableFilter<string>[] FilterProcessing;
        public TableFilter<string>[] FilterVariableType;
        public TableFilter<string>[] FilterZone;

        EmbeddedProperty Property(int span, int offset) => new() { Span = span, Offset = offset };

        protected override void OnInitialized()
        {
            _stateSubscription = State.Subscribe(
                this,
                OnStateChanged,
                AppStateChange.Catalog,
                AppStateChange.AppConfig);

            if (State?.Catalog != null)
                SetCatalogVariables(State?.Catalog?.Metadatas);

            if (State?.AppConfig != null)
                SetFilters(State?.AppConfig);
        }

        private async Task OnStateChanged(AppStateChangedEventArgs args)
        {
            if (args.Change == AppStateChange.Catalog)
            {
                SetCatalogVariables(State?.Catalog?.Metadatas);
            }

            SetFilters(State?.AppConfig);
            await InvokeAsync(StateHasChanged);
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

            List<TableFilter<string>> variableTypes = new List<TableFilter<string>>();
            foreach (var variableType in appConfig.VariableTypes)
            {
                variableTypes.Add(new TableFilter<string> { Text = variableType, Value = variableType });
            }
            this.FilterVariableType = variableTypes.ToArray();

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
                                VariableType = variable.VariableType,
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
                                VariableType = variable.VariableType,
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
                        (c.DatasetName != null && c.DatasetName.ToLower().Contains(ViewModel.SearchTerm.ToLower())) ||
                        (c.Name != null && c.Name.ToLower().Contains(ViewModel.SearchTerm.ToLower())) ||
                        (c.Description != null && c.Description.ToLower().Contains(ViewModel.SearchTerm.ToLower())) ||
                        (c.Units != null && c.Units.ToLower().Contains(ViewModel.SearchTerm.ToLower())) ||
                        (c.Tags != null && c.Tags.Any(t => t != null && t.ToLower().Contains(ViewModel.SearchTerm.ToLower()))))
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

            metadataDetailsModalRef = ModalService
                .CreateModal<MetadataDetailsModal, ViewModels.MetadataDetailsViewModel>(
                    modalConfig,
                    templateOptions);
        }

        public void Dispose()
        {
            _stateSubscription?.Dispose();
        }

    }
}
