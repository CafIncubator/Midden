using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caf.Midden.Core.Services.Metadata;
using Caf.Midden.Core.Models.v0_1_0alpha4;
using Microsoft.AspNetCore.Components;

namespace Caf.Midden.Components
{
    public partial class MetadataLoaderFileSystem : ComponentBase
    {
        private async Task OnInputFileMetadataChange(
            InputFileChangeEventArgs e)
        {
            if (e.FileCount != 1)
            {
                return;
            }

            // TODO IoC?
            MetadataReader metadataReader =
                    new MetadataReader(
                        new MetadataParser(
                            new MetadataConverter()));

            Metadata metadata =
                await metadataReader.ReadAsync(e.File.OpenReadStream());

            State.SetMetadata(this, metadata);
        }

        private async Task State_StateChanged(
            ComponentBase source,
            string property)
        {
            if(source != this)
            {
                // Do work?
                await InvokeAsync(StateHasChanged);
            }
        }

        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property) =>
                await State_StateChanged(source, property);
        }

        void IDisposable.Dispose()
        {
            State.StateChanged -= async (source, property) =>
                await State_StateChanged(source, property);
        }
    }
}
