using AntDesign;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Wasm.Shared.Modals;
using Caf.Midden.Wasm.Services;
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
        private IDisposable? _stateSubscription;

        [Parameter]
        public string ProjectName { get; set; }

        public Project Project { get; set; }

        public int NumberDatasets { get; set; }
        public int NumberVariables { get; set; }

        public string MarkdownDescription { get; set; }

        protected override void OnInitialized()
        {
            _stateSubscription = State.Subscribe(
                this,
                OnStateChanged,
                AppStateChange.Catalog);

            if (State?.Catalog != null)
            {
                SetProject();
                if (Project != null)
                {
                    SetMarkdown();
                    SetNumberDatasets();
                    SetNumberVariables();
                }
            }
        }

        private async Task OnStateChanged(AppStateChangedEventArgs args)
        {
            SetProject();
            if(Project != null)
            {
                SetMarkdown();
                SetNumberDatasets();
                SetNumberVariables();
            }

            await InvokeAsync(StateHasChanged);
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

        private void SetNumberVariables()
        {
            int num = 0;
            num = State.Catalog.Metadatas
                .Where(m =>
                    (String.IsNullOrEmpty(this.ProjectName) ||
                        m.Dataset.Project.ToLower().Trim() == this.ProjectName.ToLower().Trim()))
                .SelectMany(m => m.Dataset.Variables).Count();

            this.NumberVariables = num;
        }

        public void Dispose()
        {
            _stateSubscription?.Dispose();
        }
    }
}
