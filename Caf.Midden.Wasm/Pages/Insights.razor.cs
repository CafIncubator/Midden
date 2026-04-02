using AntDesign.Charts;
using Caf.Midden.Wasm.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Pages
{
    public partial class Insights : IDisposable
    {
        [Inject]
        public CatalogInsightsService InsightsService { get; set; } = default!;

        private IDisposable? _stateSubscription;

        int TotalDatasets { get; set; }
        int TotalVariables { get; set; }
        int TotalTags { get; set; }
        int TotalContacts { get; set; }
        int TotalProjects { get; set; }

        Dictionary<string, int> TopTags { get; set; } = new();
        Dictionary<string, int> TopContacts { get; set; } = new();

        DateTime CatalogLastUpdate { get; set; }

        IChartComponent? MetadataPerZone;

        public object[] MetadataPerZoneData { get; set; } = Array.Empty<object>();
        ColumnConfig MetadataPerZoneConfig = new ColumnConfig
        {
            AutoFit = true,
            Padding = "auto",
            XField = "zone",
            YField = "count"
        };

        public object[] DatasetsOverTimeData { get; set; } = Array.Empty<object>();
        IChartComponent? DatasetsOverTime;
        AreaConfig DatasetsOverTimeConfig = new AreaConfig
        {
            AutoFit = true,
            Padding = "auto",
            XField = "date",
            YField = "count",
            XAxis = new ValueCatTimeAxis
            {
                Visible = true,
                Type = "dateTime"
            },
            YAxis = new ValueAxis
            {
                Visible = true,
                Min = 0
            }
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
            TopTags = snapshot.TopTags.ToDictionary(item => item.Key, item => item.Count);
            TopContacts = snapshot.TopContacts.ToDictionary(item => item.Key, item => item.Count);

            MetadataPerZoneData = snapshot.DatasetsByZone.Cast<object>().ToArray();
            DatasetsOverTimeData = snapshot.DatasetGrowth.Cast<object>().ToArray();

        }

        public void Dispose()
        {
            _stateSubscription?.Dispose();
        }
    }
}
