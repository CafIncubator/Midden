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
    public partial class ProjectDetails : IDisposable
    {
        [Parameter]
        public string ProjectName { get; set; }

        public Project Project { get; set; }

        public int NumberDatasets { get; set; }

        public string MarkdownDescription { get; set; }

        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property)
                => await StateChanged(source, property);

            if (State?.Catalog != null)
            {
                SetProject();
                if (Project != null)
                {
                    SetMarkdown();
                    SetNumberDatasets();
                }
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
                    SetProject();
                    if(Project != null)
                    {
                        SetMarkdown();
                        SetNumberDatasets();
                    }
                    
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        private void SetProject()
        {
            Project = State.Catalog.Projects
                .Where(p =>
                    (String.IsNullOrEmpty(p.Name) || 
                        p.Name.ToLower() == this.ProjectName.ToLower()))
                .FirstOrDefault();
        }

        private void SetMarkdown()
        {
            if (string.IsNullOrEmpty(Project.Description))
                this.MarkdownDescription = "";

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseYamlFrontMatter()
                .Build();

            this.MarkdownDescription = Markdown.ToHtml(
                this.Project.Description, pipeline);
        }

        private void SetNumberDatasets()
        {
            int num = 0;
            num = State.Catalog.Metadatas
                .Where(m =>
                    (String.IsNullOrEmpty(this.ProjectName) || 
                        m.Dataset.Project.ToLower().Trim() == this.ProjectName.ToLower().Trim()))
                .Count();

            this.NumberDatasets = num;
        }

        public void Dispose()
        {
            State.StateChanged -= async (source, property)
                => await StateChanged(source, property);
        }
    }
}
