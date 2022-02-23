using AntDesign;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Wasm.Shared.Modals;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class FilteredCatalogProjectViewer : IDisposable
    {
        [Parameter]
        public string Project { get; set; }


        public List<Project> BaseProjects { get; set; } = new List<Project>();
        public List<Project> FilteredProjects { get; set; } = new List<Project>();

        public string SearchTerm { get; set; }

        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property)
                => await StateChanged(source, property);

            if (State?.Catalog != null)
            {
                SetBaseProjects();
                FilteredProjects = this.BaseProjects;
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
                    SetBaseProjects();
                    FilteredProjects = this.BaseProjects;
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        private void SetBaseProjects()
        {
            // Commenting out, but leaving code in case we decide to filter projects by zone?
            /*BaseProjects = State.Catalog.Projects
                .Where(m =>
                    (String.IsNullOrEmpty(this.Zone) || m.Dataset.Zone.ToLower() == this.Zone.ToLower()) &&
                    (String.IsNullOrEmpty(this.Project) || m.Dataset.Project.ToLower() == this.Project.ToLower()) &&
                    (String.IsNullOrEmpty(this.Tag) || m.Dataset.Tags.Any(t => t.ToLower() == this.Tag.ToLower())))
                .ToList();*/

            BaseProjects = State.Catalog.Projects;
        }
        private void SearchHandler()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                FilteredProjects = this.BaseProjects;
            }
            else
            {
                FilteredProjects = this.BaseProjects
                    .Where(p =>
                        (p.Name.ToLower().Contains(
                            SearchTerm.ToLower())) ||
                        (p.Description.ToLower().Contains(
                            SearchTerm.ToLower())))
                    .ToList();
            }
        }

        public void Dispose()
        {
            State.StateChanged -= async (source, property)
                => await StateChanged(source, property);
        }
    }
}
