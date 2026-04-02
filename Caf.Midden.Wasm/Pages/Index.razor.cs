using AntDesign;
using AntDesign.Charts;
using Caf.Midden.Wasm.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Pages
{
    public partial class Index : IDisposable
    {
        [Inject]
        public CatalogInsightsService InsightsService { get; set; } = default!;

        private IDisposable? _stateSubscription;

        int TotalDatasets { get; set; }
        int TotalVariables { get; set; }
        int TotalTags { get; set; }
        int TotalContacts { get; set; }
        int TotalProjects { get; set; }

        Dictionary<string, int> TopDatasetTags { get; set; } = new();
        Dictionary<string, int> TopVariableTags { get; set; } = new();
        Dictionary<string, int> TopContacts { get; set; } = new();

        DateTime CatalogLastUpdate { get; set; }

        EmbeddedProperty Property(int span, int offset) => new() { Span = span, Offset = offset };

        IChartComponent? MetadataPerZone;
        public object[] MetadataPerZoneData { get; set; } = Array.Empty<object>();

        ColumnConfig MetadataPerZoneConfig = new ColumnConfig
        {
            AutoFit = true,
            Padding = "auto",
            XField = "zone",
            YField = "count",
            Height = 350
        };

        IChartComponent? ProjectsPerStatus;
        public object[] ProjectsPerStatusData { get; set; } = Array.Empty<object>();
        ColumnConfig ProjectsPerStatusConfig = new ColumnConfig
        {
            AutoFit = true,
            Padding = "auto",
            XField = "status",
            YField = "count",
            Height = 350
        };

        IChartComponent? DatasetsOverTime;
        public object[] DatasetsOverTimeData { get; set; } = Array.Empty<object>();
        AreaConfig DatasetsOverTimeConfig = new AreaConfig
        {
            AutoFit = true,
            Padding = new[] { 0, 0, 10, 28 },
            XField = "date",
            YField = "count",
            XAxis = new ValueCatTimeAxis
            {
                Visible = false,
                Type = "dateTime"
            },
            YAxis = new ValueAxis
            {
                Visible = true,
                Min = 0
            },
            Height = 200
        };

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

        private void UpdateInsights()
        {
            CatalogInsightsSnapshot snapshot = InsightsService.BuildSnapshot(State.Catalog, State.AppConfig);

            TotalDatasets = snapshot.TotalDatasets;
            TotalVariables = snapshot.TotalVariables;
            TotalTags = snapshot.TotalTags;
            TotalContacts = snapshot.TotalContacts;
            TotalProjects = snapshot.TotalProjects;
            CatalogLastUpdate = snapshot.CatalogLastUpdate;
            TopDatasetTags = snapshot.TopDatasetTags.ToDictionary(item => item.Key, item => item.Count);
            TopVariableTags = snapshot.TopVariableTags.ToDictionary(item => item.Key, item => item.Count);
            TopContacts = snapshot.TopContacts.ToDictionary(item => item.Key, item => item.Count);

            MetadataPerZoneData = snapshot.DatasetsByZone.Cast<object>().ToArray();
            ProjectsPerStatusData = snapshot.ProjectsByStatus.Cast<object>().ToArray();
            DatasetsOverTimeData = snapshot.DatasetGrowth.Cast<object>().ToArray();
        }

        public void Dispose()
        {
            _stateSubscription?.Dispose();
        }
    }
}
