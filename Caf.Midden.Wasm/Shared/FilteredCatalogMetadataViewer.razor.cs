using AntDesign;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Wasm.Services;
using Caf.Midden.Wasm.Shared.Modals;
using Markdig;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class FilteredCatalogMetadataViewer : IDisposable
    {
        private const int MaxVisibleTags = 4;
        private const int DefaultPageSize = 12;
        private const int DescriptionPreviewCharacterThreshold = 240;
        private const int DescriptionPreviewLineThreshold = 3;

        private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseYamlFrontMatter()
            .Build();

        private IDisposable? _stateSubscription;
        private CancellationTokenSource? _filterDebounceCts;

        [Parameter]
        public string Zone { get; set; } = string.Empty;

        [Parameter]
        public string Project { get; set; } = string.Empty;

        [Parameter]
        public string Tag { get; set; } = string.Empty;

        [Parameter]
        public bool ShowSearch { get; set; } = true;

        [Parameter]
        public bool ShowHeader { get; set; } = true;

        [Parameter]
        public int ShowRecentNumber { get; set; }

        EmbeddedProperty Property(int span, int offset) => new() { Span = span, Offset = offset };

        public List<MetadataCardItem> BaseMetadataCards { get; set; } = new();
        public List<MetadataCardItem> FilteredMetadata { get; set; } = new();
        public List<MetadataCardItem> PagedMetadata { get; set; } = new();

        public List<string> ZoneOptions { get; set; } = new();
        public List<string> ProjectOptions { get; set; } = new();
        public List<string> TagOptions { get; set; } = new();
        public Dictionary<string, int> TagCounts { get; set; } = new();

        public string TagBrowseSearchTerm { get; set; } = string.Empty;
        public bool TagBrowseSortByPopularity { get; set; }
        public bool TagMatchAll { get; set; }
        public string TagMatchModeLabel => TagMatchAll ? "All" : "Any";

        public string SearchTerm { get; set; } = string.Empty;
        public string SelectedZone { get; set; } = string.Empty;
        public string SelectedProject { get; set; } = string.Empty;
        public List<string> SelectedTags { get; set; } = new();
        public string SelectedSort { get; set; } = DatasetSortOptions.Recent;

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = DefaultPageSize;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)FilteredMetadata.Count / PageSize));

        private static class DatasetSortOptions
        {
            public const string Recent = "recent";
            public const string Oldest = "oldest";
            public const string NameAz = "name-az";
            public const string VariableCount = "vars-desc";
        }

        protected override void OnInitialized()
        {
            _stateSubscription = State.Subscribe(
                this,
                OnStateChanged,
                AppStateChange.Catalog,
                AppStateChange.AppConfig);

            SelectedTags = string.IsNullOrWhiteSpace(Tag) ? new List<string>() : new List<string> { Tag };

            if (State?.Catalog != null)
            {
                RebuildCatalogCards();
            }
        }

        private async Task OnStateChanged(AppStateChangedEventArgs args)
        {
            RebuildCatalogCards();
            await InvokeAsync(StateHasChanged);
        }

        private void RebuildCatalogCards()
        {
            if (State.Catalog?.Metadatas is null)
            {
                BaseMetadataCards = new();
                FilteredMetadata = new();
                PagedMetadata = new();
                return;
            }

            List<Metadata> routeFilteredMetadatas = State.Catalog.Metadatas
                .Where(metadata =>
                    (string.IsNullOrWhiteSpace(Zone) || string.Equals(metadata.Dataset.Zone, Zone, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrWhiteSpace(Project) || string.Equals(metadata.Dataset.Project, Project, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (routeFilteredMetadatas.Count > 0 && ShowRecentNumber > 0)
            {
                routeFilteredMetadatas = routeFilteredMetadatas
                    .OrderByDescending(metadata => GetSortDate(metadata))
                    .Take(ShowRecentNumber)
                    .ToList();
            }

            BaseMetadataCards = routeFilteredMetadatas
                .Select(metadata => new MetadataCardItem(
                    metadata,
                    GetMarkdown(metadata.Dataset.Description),
                    metadata.Dataset.Description,
                    MaxVisibleTags,
                    DescriptionPreviewCharacterThreshold,
                    DescriptionPreviewLineThreshold))
                .ToList();

            ZoneOptions = BaseMetadataCards
                .Select(item => item.Metadata.Dataset.Zone)
                .Where(zone => !string.IsNullOrWhiteSpace(zone))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(zone => zone, StringComparer.OrdinalIgnoreCase)
                .ToList();

            ProjectOptions = BaseMetadataCards
                .Select(item => item.Metadata.Dataset.Project)
                .Where(project => !string.IsNullOrWhiteSpace(project))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(project => project, StringComparer.OrdinalIgnoreCase)
                .ToList();

            TagOptions = BaseMetadataCards
                .SelectMany(item => item.Metadata.Dataset.Tags ?? new List<string>())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .ToList();

            TagCounts = BaseMetadataCards
                .SelectMany(item => item.Metadata.Dataset.Tags ?? new List<string>())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            ApplyFilters(resetPage: true);
        }

        private static string GetMarkdown(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return Markdown.ToHtml(value, MarkdownPipeline);
        }

        private static DateTime GetSortDate(Metadata metadata)
            => metadata.Dataset.LastUpdate ?? metadata.ModifiedDate;

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
            IEnumerable<MetadataCardItem> query = BaseMetadataCards;

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                string term = SearchTerm.Trim();
                query = query.Where(item =>
                    item.Metadata.Dataset.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (item.Metadata.Dataset.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    item.Metadata.Dataset.Tags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    item.Metadata.Dataset.Project.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    item.Metadata.Dataset.Zone.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedZone))
            {
                query = query.Where(item => string.Equals(item.Metadata.Dataset.Zone, SelectedZone, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedProject))
            {
                query = query.Where(item => string.Equals(item.Metadata.Dataset.Project, SelectedProject, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedTags.Count > 0)
            {
                query = TagMatchAll
                    ? query.Where(item => SelectedTags.All(selectedTag => item.Metadata.Dataset.Tags.Any(tag => string.Equals(tag, selectedTag, StringComparison.OrdinalIgnoreCase))))
                    : query.Where(item => item.Metadata.Dataset.Tags.Any(tag => SelectedTags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
            }

            query = SelectedSort switch
            {
                DatasetSortOptions.Oldest => query.OrderBy(item => GetSortDate(item.Metadata)),
                DatasetSortOptions.NameAz => query.OrderBy(item => item.Metadata.Dataset.Name, StringComparer.OrdinalIgnoreCase),
                DatasetSortOptions.VariableCount => query.OrderByDescending(item => item.Metadata.Dataset.Variables.Count),
                _ => query.OrderByDescending(item => GetSortDate(item.Metadata))
            };

            FilteredMetadata = query.ToList();

            if (resetPage)
            {
                CurrentPage = 1;
            }

            UpdatePagedMetadata();
        }

        private void UpdatePagedMetadata()
        {
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }

            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            PagedMetadata = FilteredMetadata
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        private Task SearchHandler() => QueueFilterApplyAsync();

        private Task OnZoneFilterChange() => QueueFilterApplyAsync();

        private Task OnProjectFilterChange() => QueueFilterApplyAsync();

        private Task OnTagFilterChange(IEnumerable<string> values)
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

        private Task MoveToPage(int nextPage)
        {
            if (nextPage < 1 || nextPage > TotalPages)
            {
                return Task.CompletedTask;
            }

            CurrentPage = nextPage;
            UpdatePagedMetadata();
            return InvokeAsync(StateHasChanged);
        }

        private void ToggleDescription(MetadataCardItem card)
        {
            card.IsDescriptionExpanded = !card.IsDescriptionExpanded;
        }

        private int GetCurrentRangeStart()
        {
            if (FilteredMetadata.Count == 0)
            {
                return 0;
            }

            return ((CurrentPage - 1) * PageSize) + 1;
        }

        private int GetCurrentRangeEnd()
            => Math.Min(CurrentPage * PageSize, FilteredMetadata.Count);

        private ModalRef metadataDetailsModalRef = default!;

        private async Task OpenMetadataDetailsModalTemplate(Metadata metadata)
        {
            var templateOptions = new ViewModels.MetadataDetailsViewModel
            {
                Metadata = metadata
            };

            ModalOptions modalConfig = new()
            {
                Title = "Metadata Preview",
                Width = "90%",
                DestroyOnClose = true,
                OnCancel = async _ => await metadataDetailsModalRef.CloseAsync(),
                OnOk = async _ => await metadataDetailsModalRef.CloseAsync(),
                AfterClose = () =>
                {
                    InvokeAsync(StateHasChanged);
                    return Task.CompletedTask;
                }
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

        public sealed class MetadataCardItem
        {
            public MetadataCardItem(
                Metadata metadata,
                string descriptionHtml,
                string? rawDescription,
                int maxVisibleTags,
                int characterThreshold,
                int lineThreshold)
            {
                Metadata = metadata;
                DescriptionHtml = descriptionHtml;

                List<string> tags = metadata.Dataset.Tags ?? new List<string>();
                VisibleTags = tags.Take(maxVisibleTags).ToList();
                HiddenTagCount = Math.Max(0, tags.Count - VisibleTags.Count);

                string description = rawDescription ?? string.Empty;
                int lineCount = description.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
                CanExpandDescription = description.Length > characterThreshold || lineCount > lineThreshold;
            }

            public Metadata Metadata { get; }
            public string DescriptionHtml { get; }
            public List<string> VisibleTags { get; }
            public int HiddenTagCount { get; }
            public bool CanExpandDescription { get; }
            public bool IsDescriptionExpanded { get; set; }
        }
    }
}
