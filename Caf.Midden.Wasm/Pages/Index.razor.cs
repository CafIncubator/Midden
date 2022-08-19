using Caf.Midden.Core.Models.v0_2;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Pages
{
    public partial class Index : IDisposable
    {
        // Simple stats
        int TotalDatasets { get; set; }
        int TotalVariables { get; set; }
        int TotalTags { get; set; }
        int TotalContacts { get; set; }
        int TotalProjects { get; set; }

        Dictionary<string, int> TopDatasetTags { get; set; }
        Dictionary<string, int> TopVariableTags { get; set; }
        Dictionary<string, int> TopContacts { get; set; }

        DateTime CatalogLastUpdate { get; set; }

        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property)
                => await StateChanged(source, property);

            if (State?.Catalog != null)
                SetInsights();
        }

        private async Task StateChanged(
            ComponentBase source,
            string property)
        {
            if (source != this)
            {
                if (property == "UpdateCatalog")
                {
                    SetInsights();
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        private void SetInsights()
        {
            if (State.Catalog == null)
                return;

            if (State.AppConfig == null)
                return;

            SetSimpleStats();
            //CreateDatasetsPerZone();
            //CreateDatasetsOverTime();
            SetTopStats();
        }

        private void SetSimpleStats()
        {
            if (State.Catalog == null)
                return;

            this.TotalDatasets = State.Catalog.Metadatas.Count;
            this.TotalVariables = State.Catalog.Metadatas.SelectMany(m =>
                m.Dataset.Variables).Count();

            this.CatalogLastUpdate = State.Catalog.CreationDate;

            this.TotalProjects = State.Catalog.Metadatas
                .Select(m => m.Dataset.Project)
                .Distinct()
                .Count();

            List<string> UniqueTags = new List<string>();
            List<string> UniqueContacts = new List<string>();

            foreach (Metadata meta in State.Catalog.Metadatas)
            {
                // Get unique tags from all datasets and variables
                UniqueTags = UniqueTags
                    .Union(meta.Dataset.Tags)
                    .ToList()
                    .Union(meta.Dataset.Variables
                        .SelectMany(v => v.Tags))
                    .ToList();

                // Get unique contacts from all datasets
                UniqueContacts = UniqueContacts
                    .Union<string>(meta.Dataset.Contacts
                        .Select(p => p.Name)
                        .Where(n => n != null)
                        .ToList<string>())
                    .ToList();
            }

            this.TotalTags = UniqueTags.Count;
            this.TotalContacts = UniqueContacts.Count;
        }

        private void SetTopStats()
        {
            List<string> DatasetTags = new List<string>();
            List<string> VariableTags = new List<string>();
            List<string> Contacts = new List<string>();

            foreach (Metadata meta in State.Catalog.Metadatas)
            {
                // Get tags from all datasets and variables
                DatasetTags = DatasetTags.Concat(meta.Dataset.Tags).ToList();
                VariableTags = VariableTags.Concat(meta.Dataset.Variables
                        .SelectMany(v => v.Tags)).ToList();

                // Get contacts from all datasets
                Contacts = Contacts.Concat(meta.Dataset.Contacts
                        .Select(p => p.Name)
                        .Where(n => n != null)
                        .ToList<string>()).ToList();
            }

            this.TopDatasetTags = DatasetTags.GroupBy(s => s)
                .ToDictionary(g => g.Key, g => g.Count())
                .OrderByDescending(d => d.Value)
                .Take(5)
                .ToDictionary(d => d.Key, d => d.Value);
            this.TopVariableTags = VariableTags.GroupBy(s => s)
                .ToDictionary(g => g.Key, g => g.Count())
                .OrderByDescending(d => d.Value)
                .Take(5)
                .ToDictionary(d => d.Key, d => d.Value);

            this.TopContacts = Contacts.GroupBy(s => s)
                .ToDictionary(g => g.Key, g => g.Count())
                .OrderByDescending(d => d.Value)
                .Take(5)
                .ToDictionary(d => d.Key, d => d.Value);
        }

        public void Dispose()
        {
            State.StateChanged -= async (source, property)
                => await StateChanged(source, property);
        }
    }
}
