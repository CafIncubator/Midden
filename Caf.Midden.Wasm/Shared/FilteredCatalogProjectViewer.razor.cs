using AntDesign;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Wasm.Services;
using Caf.Midden.Wasm.Shared.ViewModels;
using Markdig;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class FilteredCatalogProjectViewer : IDisposable
    {
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
        public string Project { get; set; } = string.Empty;

        public CatalogProjectsViewerViewModel ViewModel { get; set; } = new();

        [Parameter]
        public bool ShowSearch { get; set; } = true;

        [Parameter]
        public bool ShowHeader { get; set; } = true;

        [Parameter]
        public int ShowRecentNumber { get; set; }

        [Parameter]
        public bool ShowResultCount { get; set; } = true;

        [Parameter]
        public bool ShowPager { get; set; } = true;

        public List<string> StatusOptions { get; set; } = new();

        public string SelectedStatus { get; set; } = string.Empty;
        public string SelectedSort { get; set; } = ProjectSortOptions.Recent;

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = DefaultPageSize;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)ViewModel.FilteredCatalogProjects.Count / PageSize));

        EmbeddedProperty Property(int span, int offset) => new() { Span = span, Offset = offset };

        private static class ProjectSortOptions
        {
            public const string Recent = "recent";
            public const string NameAz = "name-az";
            public const string MostDatasets = "datasets-desc";
            public const string MostVariables = "variables-desc";
        }

        protected override void OnInitialized()
        {
            _stateSubscription = State.Subscribe(
                this,
                OnStateChanged,
                AppStateChange.Catalog,
                AppStateChange.AppConfig);

            if (State?.Catalog != null)
            {
                SetBaseCatalogProjects(State.Catalog);
            }
        }

        private async Task OnStateChanged(AppStateChangedEventArgs args)
        {
            SetBaseCatalogProjects(State.Catalog);
            await InvokeAsync(StateHasChanged);
        }

        private void SetBaseCatalogProjects(Catalog catalog)
        {
            if (catalog?.Projects is null)
            {
                ViewModel.BaseCatalogProjects = new();
                ViewModel.FilteredCatalogProjects = new();
                ViewModel.PagedCatalogProjects = new();
                return;
            }

            List<Metadata> metadatas = catalog.Metadatas ?? new List<Metadata>();

            Dictionary<string, List<Metadata>> metadataByProject = metadatas
                .Where(metadata => !string.IsNullOrWhiteSpace(metadata.Dataset.Project))
                .GroupBy(metadata => Normalize(metadata.Dataset.Project))
                .ToDictionary(group => group.Key, group => group.ToList());

            List<CatalogProject> catalogProjects = catalog.Projects
                .Where(project => string.IsNullOrWhiteSpace(Project) || string.Equals(project.Name, Project, StringComparison.OrdinalIgnoreCase))
                .Select(project => BuildCatalogProject(project, metadataByProject))
                .GroupBy(project => Normalize(project.Name))
                .Select(group => group
                    .OrderByDescending(project => project.LastModified ?? DateTime.MinValue)
                    .First())
                .ToList();

            IEnumerable<CatalogProject> orderedProjects = catalogProjects
                .OrderByDescending(project => project.LastModified ?? DateTime.MinValue)
                .ThenBy(project => project.Name, StringComparer.OrdinalIgnoreCase);

            if (ShowRecentNumber > 0)
            {
                orderedProjects = orderedProjects.Take(ShowRecentNumber);
            }

            ViewModel.BaseCatalogProjects = orderedProjects.ToList();

            StatusOptions = ViewModel.BaseCatalogProjects
                .Select(project => project.ProjectStatus)
                .Where(status => !string.IsNullOrWhiteSpace(status))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(status => status, StringComparer.OrdinalIgnoreCase)
                .ToList();

            ApplyFilters(resetPage: true);
        }

        private static CatalogProject BuildCatalogProject(Project project, Dictionary<string, List<Metadata>> metadataByProject)
        {
            string normalizedProject = Normalize(project.Name);
            metadataByProject.TryGetValue(normalizedProject, out List<Metadata>? relatedMetadata);

            relatedMetadata ??= new List<Metadata>();

            int datasetCount = relatedMetadata.Count;
            int variableCount = relatedMetadata.Sum(metadata => metadata.Dataset.Variables?.Count ?? 0);

            return new CatalogProject
            {
                Name = project.Name,
                Description = project.Description,
                MarkdownDescription = GetMarkdown(project.Description),
                LastModified = project.LastModified,
                ProjectStatus = project.ProjectStatus ?? string.Empty,
                DatasetCount = datasetCount,
                VariableCount = variableCount,
                CanExpandDescription = ShouldAllowDescriptionExpand(project.Description)
            };
        }

        private static string GetMarkdown(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return string.Empty;
            }

            return Markdown.ToHtml(description, MarkdownPipeline);
        }

        private static bool ShouldAllowDescriptionExpand(string? description)
        {
            string content = description ?? string.Empty;
            int lineCount = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;

            return content.Length > DescriptionPreviewCharacterThreshold || lineCount > DescriptionPreviewLineThreshold;
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
            IEnumerable<CatalogProject> query = ViewModel.BaseCatalogProjects;

            if (!string.IsNullOrWhiteSpace(ViewModel.SearchTerm))
            {
                string term = ViewModel.SearchTerm.Trim();
                query = query.Where(project =>
                    project.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    project.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedStatus))
            {
                query = query.Where(project => string.Equals(project.ProjectStatus, SelectedStatus, StringComparison.OrdinalIgnoreCase));
            }

            query = SelectedSort switch
            {
                ProjectSortOptions.NameAz => query.OrderBy(project => project.Name, StringComparer.OrdinalIgnoreCase),
                ProjectSortOptions.MostDatasets => query.OrderByDescending(project => project.DatasetCount),
                ProjectSortOptions.MostVariables => query.OrderByDescending(project => project.VariableCount),
                _ => query.OrderByDescending(project => project.LastModified ?? DateTime.MinValue)
            };

            ViewModel.FilteredCatalogProjects = query.ToList();

            if (resetPage)
            {
                CurrentPage = 1;
            }

            UpdatePagedProjects();
        }

        private void UpdatePagedProjects()
        {
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }

            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            ViewModel.PagedCatalogProjects = ViewModel.FilteredCatalogProjects
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        private Task SearchHandler() => QueueFilterApplyAsync();

        private Task OnStatusFilterChanged() => QueueFilterApplyAsync();

        private Task OnSortChanged() => QueueFilterApplyAsync(resetPage: false);

        private void ToggleDescription(CatalogProject project)
        {
            project.IsDescriptionExpanded = !project.IsDescriptionExpanded;
        }

        private Task MoveToPage(int nextPage)
        {
            if (nextPage < 1 || nextPage > TotalPages)
            {
                return Task.CompletedTask;
            }

            CurrentPage = nextPage;
            UpdatePagedProjects();
            return InvokeAsync(StateHasChanged);
        }

        private int GetCurrentRangeStart()
        {
            if (ViewModel.FilteredCatalogProjects.Count == 0)
            {
                return 0;
            }

            return ((CurrentPage - 1) * PageSize) + 1;
        }

        private int GetCurrentRangeEnd()
            => Math.Min(CurrentPage * PageSize, ViewModel.FilteredCatalogProjects.Count);

        private static string Normalize(string? value)
            => (value ?? string.Empty).Trim().ToLowerInvariant();

        public void Dispose()
        {
            _filterDebounceCts?.Cancel();
            _filterDebounceCts?.Dispose();
            _stateSubscription?.Dispose();
        }
    }
}
