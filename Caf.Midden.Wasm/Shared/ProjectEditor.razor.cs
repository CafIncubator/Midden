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
using Caf.Midden.Wasm.Services;

namespace Caf.Midden.Wasm.Shared
{
    public partial class ProjectEditor : ComponentBase, IDisposable
    {
        private IDisposable? _stateSubscription;

        public Project Project { get; set; } = new Project();

        [Parameter]
        public bool isLoading { get; set; } = false;

        string markdownHtml = "";

        private async Task OnStateChanged(AppStateChangedEventArgs args)
        {
            await InvokeAsync(StateHasChanged);
            Console.WriteLine("LastUpdate_StateChanged");
        }

        protected override void OnInitialized()
        {
            markdownHtml = Markdig.Markdown.ToHtml(
                State.ProjectEdit.Description ?? string.Empty);

            _stateSubscription = State.Subscribe(
                this,
                OnStateChanged,
                AppStateChange.ProjectEdit,
                AppStateChange.LastUpdated);
        }

        Task OnMarkdownValueHTMLChanged(string value)
        {
            markdownHtml = value;
            return Task.CompletedTask;
        }

        private void NewProjectEdit()
        {
            //DateTime dt = DateTime.UtcNow;

            State.UpdateProjectEdit(this, new Project());
        }

        private async Task<string> SaveProject()
        {
            var now = DateTime.UtcNow;

            //State.MetadataEdit.ModifiedDate = now;

            string frontMatter = $"---\nproject: \"{State.ProjectEdit.Name}\"\nlastModified: \"{now.ToString("O")}\"\nstatus: \"{State.ProjectEdit.ProjectStatus}\"\n---";
            string fileString = frontMatter + "\n" + State.ProjectEdit.Description;

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

                var project = projectReader.Read(fileString);
                if (project is not null)
                {
                    State.UpdateProjectEdit(this, project);
                }
                //await ProjectChanged.InvokeAsync(this.Project);
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

        public void Dispose()
        {
            _stateSubscription?.Dispose();
        }
    }
}
