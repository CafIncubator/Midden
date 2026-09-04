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
using Caf.Midden.Core.Services.Validation;

// Aliased because Caf.Midden.Wasm.Shared.MetadataSections is a namespace of components and wins
// simple-name resolution against the Core class of the same name.
using Sections = Caf.Midden.Core.Services.Validation.MetadataSections;

namespace Caf.Midden.Wasm.Shared
{
    public partial class DataDictionaryLoaderCafCsv : ComponentBase
    {
        private static readonly MetadataValidator Validator = new();

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

                ReportImportedVariableIssues();
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

        /// <summary>
        /// Summarizes validation problems in the rows that were just imported.
        /// </summary>
        /// <remarks>
        /// A CSV import replaces every variable at once, so problems arrive in bulk and far from
        /// the user's attention. Reporting the count here - rather than only at download time -
        /// keeps the cost of a bad dictionary next to the action that caused it. The rows
        /// themselves are marked in the editor's variables table.
        /// </remarks>
        private void ReportImportedVariableIssues()
        {
            var result = Validator.Validate(State.MetadataEdit, State.AppConfig);

            var affectedRows = result.Issues
                .Where(i => i.Section == Sections.Variables)
                .Select(i => i.Path)
                .Distinct()
                .Count();

            if (affectedRows == 0)
            {
                return;
            }

            MessageService.Warning(new MessageConfig
            {
                Content = $"Imported, but {affectedRows} variable {(affectedRows == 1 ? "entry needs" : "entries need")} attention. Affected rows are marked in the variables table.",
                Duration = 8
            });
        }
    }
}
