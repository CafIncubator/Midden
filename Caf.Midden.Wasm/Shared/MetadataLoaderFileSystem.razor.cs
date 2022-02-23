using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caf.Midden.Core.Services.Metadata;
using Caf.Midden.Core.Models.v0_2;
using Microsoft.AspNetCore.Components;
using AntDesign;

namespace Caf.Midden.Wasm.Shared
{
    public partial class MetadataLoaderFileSystem : ComponentBase
    {
        [Parameter]
        public Metadata Metadata { get; set; }

        [Parameter]
        public EventCallback<Metadata> MetadataChanged { get; set; }

        [Parameter]
        public bool isLoading { get; set; } = false;

        private async Task OnInputFileMetadataChange(
            InputFileChangeEventArgs e)
        {
            isLoading = true;

            if (e.FileCount != 1)
            {
                return;
            }

            try
            {
                // TODO IoC?
                MetadataReader metadataReader =
                        new MetadataReader(
                            new MetadataParser(
                                new MetadataConverter()));

                this.Metadata =
                    await metadataReader.ReadAsync(e.File.OpenReadStream());

                await MetadataChanged.InvokeAsync(this.Metadata);
            }
            catch
            {
                // TODO: Indicate error state
            }
            finally
            {
                isLoading = false;
            }
        }
    }
}
