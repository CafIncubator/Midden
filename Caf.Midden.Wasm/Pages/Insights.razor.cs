using Caf.Midden.Wasm.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        public ZoneCount[] MetadataPerZoneData { get; set; } = Array.Empty<ZoneCount>();

        // Headroom so the tallest column's data label still fits above it.
        public int MetadataPerZoneAxisMax
        {
            get
            {
                int max = MetadataPerZoneData.Select(item => item.Count).DefaultIfEmpty(0).Max();
                return max + Math.Max(1, (int)Math.Ceiling(max * 0.15));
            }
        }

        public int DatasetGrowthValueStep { get; private set; } = 1;

        public int DatasetGrowthValueMax { get; private set; } = 1;

        public Index.GrowthPoint[] DatasetGrowthPoints { get; set; } = Array.Empty<Index.GrowthPoint>();

        private DateTime _growthFirstDate;
        private DateTime _growthLastDate;

        // A step equal to the whole span makes the axis emit exactly two ticks: the first and last
        // point, which is all a cumulative curve needs.
        public object? DatasetGrowthStep { get; private set; }

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

            MetadataPerZoneData = snapshot.DatasetsByZone.ToArray();

            DatasetGrowthPoints = snapshot.DatasetGrowth
                .Select(point => new Index.GrowthPoint(
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

        // Rounds a step to 1, 2, 5 or 10 times a power of ten so ticks read 0/10/20.
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

        public void Dispose()
        {
            _stateSubscription?.Dispose();
        }
    }
}
