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
        [Parameter]
        public Metadata Metadata { get; set; }

        [Parameter]
        public EventCallback<Metadata> MetadataChanged { get; set; }

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

            this.Metadata =
                await metadataReader.ReadAsync(e.File.OpenReadStream());

            await MetadataChanged.InvokeAsync(this.Metadata);
        }
    }
}
