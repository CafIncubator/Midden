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
using Caf.Midden.Core.Services;

namespace Caf.Midden.Wasm.Shared
{
    public partial class DataDictionaryLoaderCafCsv : ComponentBase
    {
        [Parameter]
        public bool isLoading { get; set; } = false;

        [Inject]
        private IMessageService MessageService { get; set; } = default!;

        private async Task OnInputFileDataDictionaryCafCsvChange(
            InputFileChangeEventArgs e)
        {
            isLoading = true;

            if (e.FileCount != 1)
            {
                return;
            }

            // TODO IoC?
            DataDictionaryReaderCafCsv reader =
                new DataDictionaryReaderCafCsv();

            try
            {
                List<Variable> variables;

                using (var stream = new MemoryStream())
                {
                    await e.File.OpenReadStream().CopyToAsync(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    variables = reader.Read(stream);
                }

                this.State.MetadataEdit.Dataset.Variables = variables;

                this.State.UpdateMetadataEdit(this, this.State.MetadataEdit);
            }
            catch (DataDictionaryReadException ex)
            {
                MessageService.Error(new MessageConfig
                {
                    Content = string.Join(" ", ex.RowErrors),
                    Duration = 8
                });
            }
            catch (Exception)
            {
                MessageService.Error(new MessageConfig
                {
                    Content = "The data dictionary CSV could not be read. Please check that it has the expected columns and try again.",
                    Duration = 8
                });
            }
            finally
            {
                isLoading = false;
            }
        }
    }
}
