using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntDesign.Charts;
using Caf.Midden.Core.Models.v0_1_0alpha4;

namespace Caf.Midden.Wasm.Pages
{
    public partial class Insights : IDisposable
    {
        // Simple stats
        int TotalDatasets { get; set; }
        int TotalVariables { get; set; }
        int TotalTags { get; set; }
        int TotalContacts { get; set; }
        int TotalProjects { get; set; }
        DateTime CatalogLastUpdate { get; set; }


        IChartComponent MetadataPerZone = new Column<object>();
        public object[] MetadataPerZoneData { get; set; }
        ColumnConfig MetadataPerZoneConfig = new ColumnConfig
        {
            Title = new Title
            {
                Visible = true,
                Text = "Datasets per Zone"
            },
            ForceFit = true,
            Padding = "auto",
            XField = "zone",
            YField = "count"
        };

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
            CreateDatasetsPerZone();
            
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

            //List<string> UniqueProjects = new List<string>();
            List<string> UniqueTags = new List<string>();
            List<string> UniqueContacts = new List<string>();

            foreach(Metadata meta in State.Catalog.Metadatas)
            {
                
                //UniqueTags = UniqueTags.Union(meta.Dataset.Tags).ToList();
                //List<string> varTags = meta.Dataset.Variables.SelectMany(v => v.Tags).ToList();
                //UniqueTags = UniqueTags.Union(varTags).ToList();

                // Get unique tags from all datasets and variables
                UniqueTags = UniqueTags
                    .Union(meta.Dataset.Tags)
                    .ToList()
                    .Union(meta.Dataset.Variables
                        .SelectMany(v => v.Tags))
                    .ToList();

                //List<string> contacts = meta.Dataset.Contacts.Select(p => p.Name).Where(n => n != null).ToList<string>();

                //UniqueContacts = UniqueContacts.Union<string>(contacts).ToList();

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

        private void CreateDatasetsPerZone()
        {
            List<object> objs = new List<object>();

            foreach (string zone in State.AppConfig.Zones)
            {
                int numberMetas = State.Catalog.Metadatas.Count(m =>
                    m.Dataset.Zone == zone);

                object obj = new
                {
                    zone = zone,
                    count = numberMetas
                };

                objs.Add(obj);
            }

            this.MetadataPerZoneData = objs.ToArray();

            MetadataPerZone.ChangeData(MetadataPerZoneData);
        }

        public void Dispose()
        {
            State.StateChanged -= async (source, property)
                => await StateChanged(source, property);
        }
    }
}
