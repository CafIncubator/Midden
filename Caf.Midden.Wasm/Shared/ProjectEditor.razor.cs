using Caf.Midden.Core.Models.v0_2;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using AntDesign;
using Caf.Midden.Wasm.Shared.Modals;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using Caf.Midden.Core.Services;

namespace Caf.Midden.Wasm.Shared
{
    public partial class ProjectEditor : ComponentBase//, IDisposable
    {
        public Project Project { get; set; } = new Project();

        [Parameter]
        public EventCallback<Project> ProjectChanged { get; set; }

        [Parameter]
        public bool isLoading { get; set; } = false;

        string markdownHtml = "";

        protected override void OnInitialized()
        {
            this.Project = new Project();
            markdownHtml = Markdig.Markdown.ToHtml(Project.Description ?? string.Empty);
            base.OnInitialized();
        }

        Task OnMarkdownValueChanged(string value)
        {
            return Task.CompletedTask;
        }

        Task OnMarkdownValueHTMLChanged(string value)
        {
            markdownHtml = value;
            return Task.CompletedTask;
        }

        private void NewProject()
        {
            DateTime dt = DateTime.UtcNow;

            Project = new Project();
        }

        private async Task<string> SaveProject()
        {
            var now = DateTime.UtcNow;

            State.MetadataEdit.ModifiedDate = now;

            string frontMatter = $"---\nproject: \"{Project.Name}\"\ncreationDate: \"{now.ToString("O")}\"\n---";
            string fileString = frontMatter + "\n" + Project.Description;

            var buffer = Encoding.UTF8.GetBytes(fileString);
            var stream = new MemoryStream(buffer);
            var fileBytes = stream.ToArray();
            
            await JS.InvokeAsync<string>(
                "saveAsFile", 
                $"DESCRIPTION.md",
                Convert.ToBase64String(fileBytes));

            return fileString;
        }

        private async Task OnInputFileProjectChange(
            InputFileChangeEventArgs e)
        {
            isLoading = true;

            if (e.FileCount != 1)
            {
                return;
            }

            try
            {
                ProjectReader projectReader =
                    new ProjectReader(
                        new ProjectParser());

                // TODO: Figure out how to pass e.File.OpenReadStream() to projectReader without it failing
                string fileString;
                using (var sr = new StreamReader(e.File.OpenReadStream(), Encoding.UTF8))
                {
                    fileString = await sr.ReadToEndAsync();
                }

                this.Project = projectReader.Read(fileString);
                await ProjectChanged.InvokeAsync(this.Project);
            }
            catch
            {
                // TODO: Indicate error state
            }
            finally
            {
                isLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
