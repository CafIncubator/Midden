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
    public partial class FilteredCatalogTagViewer : IDisposable
    {
        [Parameter]
        public bool ShowSearch { get; set; } = true;

        [Parameter]
        public bool ShowHeader { get; set; } = true;

        EmbeddedProperty Property(int span, int offset) => new() { Span = span, Offset = offset };


        Dictionary<string, int> BaseDatasetTags { get; set; }
        Dictionary<string, int> BaseVariableTags { get; set; }
        Dictionary<string, int> FilteredDatasetTags { get; set; }
        Dictionary<string, int> FilteredVariableTags { get; set; }

        public string SearchTerm { get; set; }

        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property)
                => await StateChanged(source, property);

            if (State?.Catalog != null)
            {
                SetBaseTags();
                InitializeFilteredTags();
            }
        }

        private async Task StateChanged(
            ComponentBase source,
            string property)
        {
            if (source != this)
            {
                if (property == "UpdateCatalog")
                {
                    SetBaseTags();
                    InitializeFilteredTags();
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        private void SetBaseTags()
        {
            List<string> DatasetTags = new List<string>();
            List<string> VariableTags = new List<string>();

            foreach (Metadata meta in State.Catalog.Metadatas)
            {
                // Get tags from all datasets and variables
                DatasetTags = DatasetTags.Concat(meta.Dataset.Tags).ToList();
                VariableTags = VariableTags.Concat(meta.Dataset.Variables
                        .SelectMany(v => v.Tags)).ToList();
            }

            this.BaseDatasetTags = DatasetTags.GroupBy(s => s)
                .ToDictionary(g => g.Key, g => g.Count())
                .OrderByDescending(d => d.Value)
                .ToDictionary(d => d.Key, d => d.Value);
            this.BaseVariableTags = VariableTags.GroupBy(s => s)
                .ToDictionary(g => g.Key, g => g.Count())
                .OrderByDescending(d => d.Value)
                .ToDictionary(d => d.Key, d => d.Value);
        }
        private void InitializeFilteredTags()
        {
            this.FilteredDatasetTags = this.BaseDatasetTags;
            this.FilteredVariableTags = this.BaseVariableTags;
        }
        private void SearchHandler()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                InitializeFilteredTags();
            }
            else
            {
                FilteredDatasetTags = this.BaseDatasetTags
                    .Where(t => t.Key.ToLower().Contains(SearchTerm.ToLower()))
                    .ToDictionary<string, int>()
                    .OrderByDescending(d => d.Value)
                    .ToDictionary<string, int>();

                FilteredVariableTags = this.BaseVariableTags
                    .Where(t => t.Key.ToLower().Contains(SearchTerm.ToLower()))
                    .ToDictionary<string, int>()
                    .OrderByDescending(d => d.Value)
                    .ToDictionary<string, int>();
            }
        }

        public void Dispose()
        {
            State.StateChanged -= async (source, property)
                => await StateChanged(source, property);
        }
    }
}
