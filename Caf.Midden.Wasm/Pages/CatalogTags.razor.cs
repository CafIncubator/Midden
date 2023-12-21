using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caf.Midden.Core.Models.v0_2;
using Microsoft.AspNetCore.Components;
using AntDesign;

namespace Caf.Midden.Wasm.Pages
{
    public partial class CatalogTags
    {
        /*
        Dictionary<string, int> TopDatasetTags { get; set; }
        Dictionary<string, int> TopVariableTags { get; set; }

        EmbeddedProperty Property(int span, int offset) => new() { Span = span, Offset = offset };

        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property)
                => await StateChanged(source, property);

            if (State?.Catalog != null)
                SetPageData();
        }

        private async Task StateChanged(
            ComponentBase source,
            string property)
        {
            if (source != this)
            {
                if (property == "UpdateCatalog")
                {
                    SetPageData();
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        private void SetPageData()
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

            this.TopDatasetTags = DatasetTags.GroupBy(s => s)
                .ToDictionary(g => g.Key, g => g.Count())
                .OrderByDescending(d => d.Value)
                .ToDictionary(d => d.Key, d => d.Value);
            this.TopVariableTags = VariableTags.GroupBy(s => s)
                .ToDictionary(g => g.Key, g => g.Count())
                .OrderByDescending(d => d.Value)
                .ToDictionary(d => d.Key, d => d.Value);
        }
        public void Dispose()
        {
            State.StateChanged -= async (source, property)
                => await StateChanged(source, property);
        }
        */
    }
}
