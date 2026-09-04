using AntDesign;
using Caf.Midden.Wasm.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Pages
{
    public partial class Index : IDisposable
    {
        [Inject]
        public CatalogInsightsService InsightsService { get; set; } = default!;

        [Inject]
        public CatalogSearchService SearchService { get; set; } = default!;

        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        public Microsoft.JSInterop.IJSRuntime JSRuntime { get; set; } = default!;

        private IDisposable? _stateSubscription;
        private CancellationTokenSource? _searchDebounceCts;

        int TotalDatasets { get; set; }
        int TotalVariables { get; set; }
        int TotalTags { get; set; }
        int TotalContacts { get; set; }
        int TotalProjects { get; set; }

        Dictionary<string, int> TopDatasetTags { get; set; } = new();
        Dictionary<string, int> TopVariableTags { get; set; } = new();
        Dictionary<string, int> TopContacts { get; set; } = new();

        DateTime CatalogLastUpdate { get; set; }

        public string SearchTerm { get; set; } = string.Empty;
        public CatalogSearchResponse SearchResponse { get; set; } = CatalogSearchResponse.Empty;

        CompletenessSummary Completeness { get; set; } = CompletenessSummary.Empty;
        ValidationHealthSummary ValidationHealth { get; set; } = ValidationHealthSummary.Empty;
        IReadOnlyList<OrphanedProject> OrphanedProjects { get; set; } = Array.Empty<OrphanedProject>();
        UndocumentedVariableSummary UndocumentedVariables { get; set; } = UndocumentedVariableSummary.Empty;

        ProjectCoverageSummary ProjectCoverage { get; set; } = ProjectCoverageSummary.Empty;
        IReadOnlyList<TemporalCoverageYear> TemporalCoverage { get; set; } = Array.Empty<TemporalCoverageYear>();
        SpatialCoverageSummary SpatialCoverage { get; set; } = SpatialCoverageSummary.Empty;
        int DatasetsPerProjectTotal { get; set; }

        int MaxTemporalCoverageCount => TemporalCoverage.Count == 0 ? 0 : TemporalCoverage.Max(year => year.Count);

        bool HasAttentionItems =>
            Completeness.LowestScoring.Count > 0
            || OrphanedProjects.Count > 0
            || ValidationHealth.WithErrors > 0
            || ValidationHealth.WithWarnings > 0
            || UndocumentedVariables.AffectedVariables > 0;

        EmbeddedProperty Property(int span, int offset) => new() { Span = span, Offset = offset };

        // Shared chart styling. The dashboard charts are small, low-cardinality, and read at a
        // glance, so they drop the heavy default chrome: axis lines, tick marks, and axis titles
        // that merely restate the card heading. Value labels sit on the marks themselves, which
        // removes the need for the viewer to trace a bar back to a gridline.
        private const string ChartAccent = "#1890ff";
        private const string ChartAccentFill = "rgba(24, 144, 255, 0.15)";
        private const string ChartGridLine = "#f0f0f0";

        // Radzen binds to typed collections by property name, so the chart data no longer has to
        // be boxed to object[] for a JSON serializer.
        public ZoneCount[] MetadataPerZoneData { get; set; } = Array.Empty<ZoneCount>();

        public StatusCount[] ProjectsPerStatusData { get; set; } = Array.Empty<StatusCount>();

        // The value axis otherwise scales exactly to the tallest column, leaving the data label no
        // room above it; Radzen then flips that one label inside the column while its shorter
        // neighbours keep theirs on top, which reads as a rendering fault.
        public int ProjectsPerStatusAxisMax => AxisMaxWithHeadroom(ProjectsPerStatusData.Select(item => item.Count));

        public int MetadataPerZoneAxisMax => AxisMaxWithHeadroom(MetadataPerZoneData.Select(item => item.Count));

        private static int AxisMaxWithHeadroom(IEnumerable<int> values)
        {
            int max = values.DefaultIfEmpty(0).Max();
            return max + Math.Max(1, (int)Math.Ceiling(max * 0.15));
        }

        // Rounds a step to 1, 2, 5 or 10 times a power of ten so ticks read 0/10/20 rather than
        // the 0/11/22 that comes from dividing the maximum into equal thirds.
        private static int NiceStep(int max, int targetTicks = 3)
        {
            if (max <= 0)
            {
                return 1;
            }

            double raw = (double)max / targetTicks;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double normalized = raw / magnitude;
            double nice = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;

            return Math.Max(1, (int)(nice * magnitude));
        }

        // Horizontal bars, because project names such as
        // "CafModelingRegionalSoilConditioningIndex" are unreadable on a vertical column axis.
        public KeyCount[] DatasetsPerProjectData { get; set; } = Array.Empty<KeyCount>();

        // Radzen does not grow a chart to fit its categories, so the height is derived from the
        // project count to keep the bars from being squeezed together.
        public int DatasetsPerProjectChartHeight
            => Math.Max(200, (DatasetsPerProjectData.Length * 28) + 40);

        // The growth timeline arrives as "yyyy-MM" strings and is parsed back into DateTime so the
        // category axis is genuinely temporal rather than a list of 100-plus string categories.
        public sealed record GrowthPoint(DateTime Date, int Count);

        public GrowthPoint[] DatasetGrowthPoints { get; set; } = Array.Empty<GrowthPoint>();

        private DateTime _growthFirstDate;
        private DateTime _growthLastDate;

        // A step equal to the whole span makes the axis emit exactly two ticks: the first and last
        // point. Letting Radzen choose the interval put the final tick short of the last point, so a
        // formatter that matched on the endpoint date never fired for the max.
        public object? DatasetGrowthStep { get; private set; }

        // Round ticks on the running total, with the axis maximum snapped to a whole step so the
        // top gridline lands on a labelled value.
        public int DatasetGrowthValueStep { get; private set; } = 1;

        public int DatasetGrowthValueMax { get; private set; } = 1;

        // With only two ticks on the axis, every tick is an endpoint and gets a date.
        private string FormatGrowthDate(object value)
            => value is DateTime date
                ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty;

        protected override void OnInitialized()
        {
            _stateSubscription = State.Subscribe(
                this,
                OnStateChanged,
                AppStateChange.Catalog,
                AppStateChange.AppConfig);

            UpdateInsights();
        }

        private async Task OnStateChanged(AppStateChangedEventArgs args)
        {
            UpdateInsights();
            await InvokeAsync(StateHasChanged);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync("homeSearchInterop.registerShortcut", "home-search-input");
            }
        }

        private void UpdateInsights()
        {
            CatalogInsightsSnapshot snapshot = InsightsService.BuildSnapshot(State.Catalog, State.AppConfig);

            TotalDatasets = snapshot.TotalDatasets;
            TotalVariables = snapshot.TotalVariables;
            TotalTags = snapshot.TotalTags;
            TotalContacts = snapshot.TotalContacts;
            TotalProjects = snapshot.TotalProjects;
            CatalogLastUpdate = snapshot.CatalogLastUpdate;
            Completeness = snapshot.Completeness;
            ValidationHealth = snapshot.ValidationHealth;
            OrphanedProjects = snapshot.OrphanedProjects;
            UndocumentedVariables = snapshot.UndocumentedVariables;
            ProjectCoverage = snapshot.ProjectCoverage;
            TemporalCoverage = snapshot.TemporalCoverage;
            SpatialCoverage = snapshot.SpatialCoverage;
            DatasetsPerProjectData = snapshot.DatasetsPerProject.ToArray();
            DatasetsPerProjectTotal = snapshot.DatasetsPerProjectTotal;
            TopDatasetTags = snapshot.TopDatasetTags.ToDictionary(item => item.Key, item => item.Count);
            TopVariableTags = snapshot.TopVariableTags.ToDictionary(item => item.Key, item => item.Count);
            TopContacts = snapshot.TopContacts.ToDictionary(item => item.Key, item => item.Count);

            MetadataPerZoneData = snapshot.DatasetsByZone.ToArray();
            ProjectsPerStatusData = snapshot.ProjectsByStatus.ToArray();

            DatasetGrowthPoints = snapshot.DatasetGrowth
                .Select(point => new GrowthPoint(
                    DateTime.ParseExact(point.Date, "yyyy-MM", CultureInfo.InvariantCulture),
                    point.Count))
                .ToArray();

            _growthFirstDate = DatasetGrowthPoints.Length > 0 ? DatasetGrowthPoints[0].Date : default;
            _growthLastDate = DatasetGrowthPoints.Length > 0 ? DatasetGrowthPoints[^1].Date : default;

            TimeSpan growthSpan = _growthLastDate - _growthFirstDate;
            DatasetGrowthStep = growthSpan > TimeSpan.Zero ? growthSpan : null;

            int growthMax = DatasetGrowthPoints.Select(point => point.Count).DefaultIfEmpty(0).Max();
            DatasetGrowthValueStep = NiceStep(growthMax);
            DatasetGrowthValueMax = Math.Max(
                DatasetGrowthValueStep,
                (int)Math.Ceiling((double)growthMax / DatasetGrowthValueStep) * DatasetGrowthValueStep);
        }

        public void Dispose()
        {
            _stateSubscription?.Dispose();
            _searchDebounceCts?.Cancel();
            _searchDebounceCts?.Dispose();
            _ = JSRuntime.InvokeVoidAsync("homeSearchInterop.unregisterShortcut");
        }

        private async Task OnSearchTermChanged(string value)
        {
            SearchTerm = value;

            _searchDebounceCts?.Cancel();
            _searchDebounceCts?.Dispose();
            _searchDebounceCts = new CancellationTokenSource();
            var token = _searchDebounceCts.Token;

            try
            {
                await Task.Delay(200, token);
                SearchResponse = SearchService.Search(State.Catalog, SearchTerm);
                await InvokeAsync(StateHasChanged);
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void OnSearchResultSelected(string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                NavigationManager.NavigateTo(url);
            }
        }

        private void ClearSearch()
        {
            _searchDebounceCts?.Cancel();
            SearchTerm = string.Empty;
            SearchResponse = CatalogSearchResponse.Empty;
        }

        private static string GetResultIcon(CatalogSearchResultType type) => type switch
        {
            CatalogSearchResultType.Dataset => "file-text",
            CatalogSearchResultType.Project => "project",
            CatalogSearchResultType.Variable => "calculator",
            CatalogSearchResultType.Tag => "tags",
            _ => "file"
        };

        private static string DatasetUrl(string zone, string project, string name)
            => $"catalog/datasets/{zone}/{project}/{name}";

        private static string ProjectEditorUrl(string name)
            => $"editor/project?name={Uri.EscapeDataString(name)}";

        private static string CompletenessColor(int percent) => percent switch
        {
            >= 80 => "#52c41a",
            >= 50 => "#faad14",
            _ => "#ff4d4f"
        };
    }
}
