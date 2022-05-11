using AntDesign;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Wasm.Shared.Modals;
using Caf.Midden.Wasm.Shared.ViewModels;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Markdig;

namespace Caf.Midden.Wasm.Shared
{
    public partial class FilteredCatalogProjectViewer : IDisposable
    {
        [Parameter]
        public string Project { get; set; }

        public CatalogProjectsViewerViewModel ViewModel { get; set; } = new CatalogProjectsViewerViewModel();
        //public List<Project> BaseProjects { get; set; } = new List<Project>();
        //public List<Project> FilteredProjects { get; set; } = new List<Project>();

        //public string SearchTerm { get; set; }

        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property)
                => await StateChanged(source, property);

            if (State?.Catalog != null)
            {
                SetBaseCatalogProjects(State.Catalog);
                ViewModel.FilteredCatalogProjects = ViewModel.BaseCatalogProjects;
                //FilteredProjects = this.BaseProjects;
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
                    SetBaseCatalogProjects(State.Catalog);
                    ViewModel.FilteredCatalogProjects = ViewModel.BaseCatalogProjects;
                    //FilteredProjects = this.BaseProjects;
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        private void SetBaseCatalogProjects(Catalog catalog)
        {
            // Commenting out, but leaving code in case we decide to filter projects by zone?
            /*BaseProjects = State.Catalog.Projects
                .Where(m =>
                    (String.IsNullOrEmpty(this.Zone) || m.Dataset.Zone.ToLower() == this.Zone.ToLower()) &&
                    (String.IsNullOrEmpty(this.Project) || m.Dataset.Project.ToLower() == this.Project.ToLower()) &&
                    (String.IsNullOrEmpty(this.Tag) || m.Dataset.Tags.Any(t => t.ToLower() == this.Tag.ToLower())))
                .ToList();*/

            if (catalog is null || catalog.Projects is null)
                return;

            List<CatalogProject> catalogProjects = new List<CatalogProject>();

            foreach(var project in catalog.Projects)
            {
                CatalogProject catalogProject = new CatalogProject()
                {
                    Name = project.Name,
                    Description = project.Description,
                    MarkdownDescription = GetMarkdown(project.Description),
                    DatasetCount = GetNumberDatasets(project.Name, catalog.Metadatas),
                    VariableCount = GetNumberVariables(project.Name, catalog.Metadatas)
                };

                catalogProjects.Add(catalogProject);
            }

            ViewModel.BaseCatalogProjects = new List<CatalogProject>(catalogProjects);
        }

        private string GetMarkdown(string description)
        {
            string markdown = "";

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseYamlFrontMatter()
                .Build();

            markdown = Markdown.ToHtml(
                description, pipeline);

            return markdown;
        }

        private int GetNumberDatasets(
            string projectName,
            List<Metadata> metadatas)
        {
            int num = 0;

            num = metadatas
                .Where(m =>
                    (String.IsNullOrEmpty(projectName) ||
                        m.Dataset.Project.ToLower().Trim() == projectName.ToLower().Trim()))
                .Count();

            return num;
        }

        private int GetNumberVariables(
            string projectName,
            List<Metadata> metadatas)
        {
            int num = 0;
            num = metadatas
                .Where(m =>
                    (String.IsNullOrEmpty(projectName) ||
                        m.Dataset.Project.ToLower().Trim() == projectName.ToLower().Trim()))
                .SelectMany(m => m.Dataset.Variables).Count();

            return num;
        }

        private void SearchHandler()
        {
            if (string.IsNullOrWhiteSpace(ViewModel.SearchTerm))
            {
                ViewModel.FilteredCatalogProjects = ViewModel.BaseCatalogProjects;
            }
            else
            {
                ViewModel.FilteredCatalogProjects = ViewModel.BaseCatalogProjects
                    .Where(p =>
                        (p.Name.ToLower().Contains(
                            ViewModel.SearchTerm.ToLower())) ||
                        (p.Description.ToLower().Contains(
                            ViewModel.SearchTerm.ToLower())))
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
