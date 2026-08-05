using AntDesign;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Wasm.Shared.Modals;
using Caf.Midden.Wasm.Shared.ViewModels;
using Caf.Midden.Wasm.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class CatalogVariableViewer : IDisposable
    {
        private const int DefaultPageSize = 20;
        private const int DescriptionPreviewCharacterThreshold = 240;
        private const int DescriptionPreviewLineThreshold = 3;
        private const int MethodsPreviewCharacterThreshold = 240;
        private const int MethodsPreviewLineThreshold = 3;

        private IDisposable? _stateSubscription;
        private CancellationTokenSource? _filterDebounceCts;

        private Dictionary<(string Zone, string Project, string Dataset), Metadata> _metadataIndex = new();

        public static readonly IReadOnlyList<(string Key, string Label)> ToggleableColumns = new List<(string, string)>
        {
            ("Description", "Description"),
            ("Units", "Units"),
            ("Methods", "Methods"),
            ("QualityControl", "Quality Control"),
            ("Processing", "Processing"),
            ("Type", "Type"),
            ("Tags", "Tags"),
            ("Zone", "Zone"),
            ("Project", "Project"),
            ("Dataset", "Dataset")
        };

        public Dictionary<string, bool> ColumnVisibility { get; set; } =
            ToggleableColumns.ToDictionary(c => c.Key, _ => true);

        [Parameter]
        public string Project { get; set; }

        [Parameter]
        public string TagName { get; set; }

        CatalogVariableViewerViewModel ViewModel { get; set; } = new CatalogVariableViewerViewModel();

        public List<string> ZoneOptions { get; set; } = new();
        public List<string> ProjectOptions { get; set; } = new();
        public List<string> ProcessingOptions { get; set; } = new();
        public List<string> VariableTypeOptions { get; set; } = new();
        public List<string> TagOptions { get; set; } = new();
        public Dictionary<string, int> TagCounts { get; set; } = new();

        public string TagBrowseSearchTerm { get; set; } = string.Empty;
        public bool TagBrowseSortByPopularity { get; set; }
        public bool TagMatchAll { get; set; }
        public string TagMatchModeLabel => TagMatchAll ? "All" : "Any";

        public string SelectedZone { get; set; } = string.Empty;
        public string SelectedProject { get; set; } = string.Empty;
        public string SelectedProcessing { get; set; } = string.Empty;
        public string SelectedVariableType { get; set; } = string.Empty;
        public List<string> SelectedTags { get; set; } = new();
        public string SelectedSort { get; set; } = VariableSortOptions.NameAz;

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = DefaultPageSize;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)ViewModel.FilteredCatalogVariables.Count / PageSize));

        EmbeddedProperty Property(int span, int offset) => new() { Span = span, Offset = offset };

        private static class VariableSortOptions
        {
            public const string NameAz = "name-az";
            public const string DatasetAz = "dataset-az";
            public const string ProjectAz = "project-az";
            public const string ZoneAz = "zone-az";
        }

        protected override void OnInitialized()
        {
            _stateSubscription = State.Subscribe(
                this,
                OnStateChanged,
                AppStateChange.Catalog,
                AppStateChange.AppConfig);

            SelectedTags = string.IsNullOrWhiteSpace(TagName) ? new List<string>() : new List<string> { TagName };

            if (State?.Catalog != null)
                SetCatalogVariables(State?.Catalog?.Metadatas);
        }

        private async Task OnStateChanged(AppStateChangedEventArgs args)
        {
            if (args.Change == AppStateChange.Catalog)
            {
                SetCatalogVariables(State?.Catalog?.Metadatas);
            }

            await InvokeAsync(StateHasChanged);
        }

        private void SetCatalogVariables(List<Metadata> metadatas)
        {
            if (metadatas == null)
                return;

            List<CatalogVariable> catalogVariables = new List<CatalogVariable>();
            _metadataIndex = new Dictionary<(string, string, string), Metadata>();

            foreach (var metadata in metadatas)
            {
                if ((metadata.Dataset != null) &&
                    (metadata.Dataset.Variables != null) &&
                    (string.IsNullOrEmpty(this.Project) ||
                        string.Equals(metadata.Dataset.Project?.Trim(), this.Project?.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    _metadataIndex[(metadata.Dataset.Zone, metadata.Dataset.Project, metadata.Dataset.Name)] = metadata;

                    foreach (var variable in metadata.Dataset.Variables)
                    {
                        catalogVariables.Add(BuildCatalogVariable(variable, metadata));
                    }
                }
            }

            ViewModel.CatalogVariables = catalogVariables;

            ZoneOptions = BuildDistinctOptions(catalogVariables.Select(c => c.Zone));
            ProjectOptions = BuildDistinctOptions(catalogVariables.Select(c => c.ProjectName));
            ProcessingOptions = BuildDistinctOptions(catalogVariables.Select(c => c.ProcessingLevel));
            VariableTypeOptions = BuildDistinctOptions(catalogVariables.Select(c => c.VariableType));
            TagOptions = BuildDistinctOptions(catalogVariables.SelectMany(c => c.Tags ?? new List<string>()));

            TagCounts = catalogVariables
                .SelectMany(c => c.Tags ?? new List<string>())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            ApplyFilters(resetPage: true);
        }

        private static List<string> BuildDistinctOptions(IEnumerable<string> values)
            => values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static CatalogVariable BuildCatalogVariable(Variable variable, Metadata metadata)
        {
            string description = variable.Description ?? string.Empty;

            var catalogVariable = new CatalogVariable()
            {
                Name = variable.Name,
                Description = description,
                Units = variable.Units,
                Tags = new List<string>(variable.Tags ?? new List<string>()),
                Methods = new List<string>(variable.Methods ?? new List<string>()),
                TemporalResolution = variable.TemporalResolution,
                TemporalExtent = variable.TemporalExtent,
                QCApplied = variable.QCApplied,
                ProcessingLevel = variable.ProcessingLevel,
                VariableType = variable.VariableType,
                Zone = metadata.Dataset.Zone,
                ProjectName = metadata.Dataset.Project,
                DatasetName = metadata.Dataset.Name,
                CanExpandDescription = ShouldAllowDescriptionExpand(description),
                CanExpandMethods = ShouldAllowMethodsExpand(variable.Methods)
            };

            catalogVariable.SearchText = BuildSearchText(catalogVariable);

            return catalogVariable;
        }

        private static string BuildSearchText(CatalogVariable variable)
        {
            return string.Join(
                " ",
                new[]
                {
                    variable.Name,
                    variable.Description,
                    variable.Units,
                    variable.DatasetName,
                    variable.ProjectName,
                    variable.Zone
                }
                .Concat(variable.Tags ?? new List<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        private static bool ShouldAllowDescriptionExpand(string description)
        {
            string content = description ?? string.Empty;
            int lineCount = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;

            return content.Length > DescriptionPreviewCharacterThreshold || lineCount > DescriptionPreviewLineThreshold;
        }

        private static bool ShouldAllowMethodsExpand(List<string> methods)
        {
            if (methods == null || methods.Count == 0)
                return false;

            string content = string.Join("\n", methods);

            return content.Length > MethodsPreviewCharacterThreshold || methods.Count > MethodsPreviewLineThreshold;
        }

        private async Task QueueFilterApplyAsync(bool resetPage = true)
        {
            _filterDebounceCts?.Cancel();
            _filterDebounceCts?.Dispose();
            _filterDebounceCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(250, _filterDebounceCts.Token);
                ApplyFilters(resetPage);
                await InvokeAsync(StateHasChanged);
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void ApplyFilters(bool resetPage)
        {
            IEnumerable<CatalogVariable> query = ViewModel.CatalogVariables;

            if (!string.IsNullOrWhiteSpace(ViewModel.SearchTerm))
            {
                string term = ViewModel.SearchTerm.Trim();
                query = query.Where(c => c.SearchText.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedZone))
            {
                query = query.Where(c => string.Equals(c.Zone, SelectedZone, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedProject))
            {
                query = query.Where(c => string.Equals(c.ProjectName, SelectedProject, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedProcessing))
            {
                query = query.Where(c => string.Equals(c.ProcessingLevel, SelectedProcessing, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedVariableType))
            {
                query = query.Where(c => string.Equals(c.VariableType, SelectedVariableType, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedTags.Count > 0)
            {
                query = TagMatchAll
                    ? query.Where(c => c.Tags != null && SelectedTags.All(selectedTag => c.Tags.Any(tag => string.Equals(tag, selectedTag, StringComparison.OrdinalIgnoreCase))))
                    : query.Where(c => c.Tags != null && c.Tags.Any(tag => SelectedTags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
            }

            query = SelectedSort switch
            {
                VariableSortOptions.DatasetAz => query.OrderBy(c => c.DatasetName, StringComparer.OrdinalIgnoreCase),
                VariableSortOptions.ProjectAz => query.OrderBy(c => c.ProjectName, StringComparer.OrdinalIgnoreCase),
                VariableSortOptions.ZoneAz => query.OrderBy(c => c.Zone, StringComparer.OrdinalIgnoreCase),
                _ => query.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            };

            ViewModel.FilteredCatalogVariables = query.ToList();

            if (resetPage)
            {
                CurrentPage = 1;
            }

            UpdatePagedVariables();
        }

        private void UpdatePagedVariables()
        {
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }

            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            ViewModel.PagedCatalogVariables = ViewModel.FilteredCatalogVariables
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        private Task SearchHandler() => QueueFilterApplyAsync();

        private Task OnZoneFilterChanged() => QueueFilterApplyAsync();

        private Task OnProjectFilterChanged() => QueueFilterApplyAsync();

        private Task OnProcessingFilterChanged() => QueueFilterApplyAsync();

        private Task OnTagFilterChanged(IEnumerable<string> values)
        {
            SelectedTags = values?.ToList() ?? new List<string>();
            return QueueFilterApplyAsync();
        }

        private Task OnTagBrowseSearchChanged() => Task.CompletedTask;

        private Task SetTagMatchMode(bool matchAll)
        {
            TagMatchAll = matchAll;
            return QueueFilterApplyAsync();
        }

        private void SetTagBrowseSort(bool sortByPopularity)
        {
            TagBrowseSortByPopularity = sortByPopularity;
        }

        private List<(string Tag, int Count)> GetTagBrowseList()
        {
            IEnumerable<string> tags = TagOptions;

            if (!string.IsNullOrWhiteSpace(TagBrowseSearchTerm))
            {
                string term = TagBrowseSearchTerm.Trim();
                tags = tags.Where(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            IEnumerable<(string Tag, int Count)> list = tags
                .Select(tag => (Tag: tag, Count: TagCounts.TryGetValue(tag, out int count) ? count : 0));

            return TagBrowseSortByPopularity
                ? list.OrderByDescending(item => item.Count).ThenBy(item => item.Tag, StringComparer.OrdinalIgnoreCase).ToList()
                : list.OrderBy(item => item.Tag, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private Task ToggleTagSelection(string tag, bool isChecked)
        {
            if (isChecked)
            {
                if (!SelectedTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    SelectedTags.Add(tag);
                }
            }
            else
            {
                SelectedTags.RemoveAll(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
            }

            return QueueFilterApplyAsync();
        }

        private Task SetSort(string sort)
        {
            SelectedSort = sort;
            return QueueFilterApplyAsync(resetPage: false);
        }

        private Task OnVariableTypeFilterChanged() => QueueFilterApplyAsync();


        private void ToggleDescription(CatalogVariable variable)
        {
            variable.IsDescriptionExpanded = !variable.IsDescriptionExpanded;
        }

        private void ToggleMethods(CatalogVariable variable)
        {
            variable.IsMethodsExpanded = !variable.IsMethodsExpanded;
        }

        private void SetColumnVisible(string key, bool visible)
        {
            ColumnVisibility[key] = visible;
        }

        private Task MoveToPage(int nextPage)
        {
            if (nextPage < 1 || nextPage > TotalPages)
            {
                return Task.CompletedTask;
            }

            CurrentPage = nextPage;
            UpdatePagedVariables();
            return InvokeAsync(StateHasChanged);
        }

        private int GetCurrentRangeStart()
        {
            if (ViewModel.FilteredCatalogVariables.Count == 0)
            {
                return 0;
            }

            return ((CurrentPage - 1) * PageSize) + 1;
        }

        private int GetCurrentRangeEnd()
            => Math.Min(CurrentPage * PageSize, ViewModel.FilteredCatalogVariables.Count);

        private ModalRef metadataDetailsModalRef;
        private async Task OpenMetadataDetailsModalTemplate(CatalogVariable catalogVariable)
        {
            if (!_metadataIndex.TryGetValue(
                (catalogVariable.Zone, catalogVariable.ProjectName, catalogVariable.DatasetName),
                out var metadata))
            {
                return;
            }

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
            _filterDebounceCts?.Cancel();
            _filterDebounceCts?.Dispose();
            _stateSubscription?.Dispose();
        }

    }
}
