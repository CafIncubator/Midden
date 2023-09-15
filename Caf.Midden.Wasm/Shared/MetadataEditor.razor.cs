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

namespace Caf.Midden.Wasm.Shared
{
    public partial class MetadataEditor : ComponentBase
    {
        string markdownDescriptionHtml = "";

        private string ZoneTooltip = @"This is the ""data zone"" that the dataset belongs to. Items in the dropdown menu are populated by information specified in the app configuration.";
        private string DatasetNameTooltip = @"This is the name of the dataset and should correspond to the name of the file or folder that contains the actual data. This also determines the name of the .midden file that is created by the Editor.";
        private string ProjectTooltip = @"This is the name of the project that the dataset belongs to. Grouping datasets under the same project provides more context to the data through the associated Project pages.";
        private string DatasetDescriptionTooltip = @"This is a description of the dataset and should include enough information for a data user to understand the basic origin and purpose of the data.";
        private string ContactsTooltip = @"This is the contact information for the contributors to the dataset. Because Midden protects the data itself by not providing download links, providing contact information is important for potential data users to start a conversation about access.";
        
        private string DatasetTagsTooltip = @"These tags are used to make the dataset more discoverable. The ""Catalog"" supports browsing and searching datasets by tags. A dataset should contain at least a few tags and the use of those tags should be as consistent as possible. Items in the dropdown menu are populated by information specified in the app configuration.";
        private string VariablesTooltip = @"These represent the measurements, or other information, represented in the dataset; i.e. the ""data dictionary"". Specifying variables is not required but it is highly recommended if the data are to be used without close oversight.";
        private string SpatialRepeatsTooltip = @"This is the number of repeated measurements that are represented in the dataset. For example, a dataset that contains soil temperature measurements (a single variable described in the ""Variable"" section) at five locations buried at five different depths would have a value of 25.";
        private string SpatialExtentTooltip = @"This is the region at which the data were collected and/or represent. Values should be valid GeoJSON (point, line, or polygon). Items in the dropdown menu are populated by information specified in the app configuration.";
        private string TemporalResolutionTooltip = @"This is the frequency at which the variables in the dataset were measured. Air temperature measured every 15 minutes may have the value of ""15 min"". A dataset that contains plant community survey data taken annually may have a value of ""1 year"" or ""annually"".";
        private string TemporalExtentTooltip = @"This is the starting and ending dates (and optionally time) that contain the date and time the data were collected or that the data represent. Consider using the ISO 8601 format for time-intervals (e.g. ""1997-07-16/1997-07-17"" corresponds to a time-period starting on July 16, 1997 and ending on July 17, 1997).";
        private string FileFormatTooltip = @"This is the format that the data are stored in. This could be a file extension (e.g. "".json"", "".txt""), general category (e.g. ""tabular"", ""image""), or some standard (e.g. MIME types: ""text/csv"", ""application/java-archive"").";
        private string FilePathTemplateTooltip = @"This is a description of the directory and file structure within the dataset folder, if applicable. For example, this can be used to describe a dataset comprised of time-series files generated every hour and separated into monthly folders via: ""{YYYY-MM}/{DD}T{hh}:{mm}_{VariableName}.csv""";
        private string FilePathDescriptionTooltip = @"This is a description of the ""File Path Template"" where each variable is described. For example, ""{YYYY-MM} is the date, in ISO 8601 format, that the data were collected.""";
        private string DatasetStructureTooltip = @"This is a category tag that broadly indicates how the data are structured. For example, a dataset folder that has multiple files but a dataset structure of ""Single"" may indicate that the various files are different versions of a single dataset. A value of ""Multiple"" on the other hand may indicate these files are timeseries data that can be aggregated. Items in the dropdown menu are populated by information specified in the app configuration.";
        private string DatasetMethodsTooltip = @"These are methods used to generate the dataset and may include things like sample collection methods, data pipelines, and so on. Methods for specific measurements within the dataset can be described here but might be better to do so in the methods field of the associated variable. The intent for these fields is to provide multiple links (e.g. GitHub repository, protocols.io, standard operating procedures), but this currently also supports free text.";
        private string ParentDatasetsTooltip = @"This is used to specify datasets that this dataset was derived from. Values are expected to be linked resources (URL) but a citation/reference is fine. This field is important for documenting data lineage.";
        private string DerivedWorksTooltip = @"This is used to indicate related products that use the dataset (e.g. published papers, presentations, decision support tools). Values are expected to be linked resources (URL) but a citation/reference is fine. This field is not intended for derived datasets, see the field “Parent Datasets” for that.";

        AntDesign.Form<Metadata> form;

        private async Task LastUpdated_StateChanged(
            ComponentBase source,
            string lastUpdated)
        {
            if(source != this)
            {
                await InvokeAsync(StateHasChanged);
                Console.WriteLine("LastUpdate_StateChanged");
            }
        }

        protected override void OnInitialized()
        {
            //this.EditContext = new EditContext(State.MetadataEdit);
            //this.EditContext.OnFieldChanged +=
            //    EditContext_OnFieldChange;

            markdownDescriptionHtml = Markdig.Markdown.ToHtml(
                State.MetadataEdit.Dataset.Description ?? string.Empty);

            State.StateChanged += async (source, property) =>
                await LastUpdated_StateChanged(source, property);
        }

        Task OnMarkdownDescriptionValueHTMLChanged(string value)
        {
            markdownDescriptionHtml = value;
            return Task.CompletedTask;
        }

        private void EditContext_OnFieldChange(
            object sender, 
            FieldChangedEventArgs e)
        {
            State.UpdateLastUpdated(this, DateTime.UtcNow);
        }

        private void NewMetadata()
        {
            DateTime dt = DateTime.UtcNow;

            State.UpdateMetadataEdit(this,new Metadata()
            {
                Dataset = new Dataset(),
                CreationDate = dt,
                ModifiedDate = dt
            });
            
            State.UpdateLastUpdated(this, DateTime.UtcNow);

        }

        #region Contact Functions
        private ModalRef personModalRef;
        private async Task OpenPersonModalTemplate(Person contact)
        {
            var templateOptions = new ViewModels.PersonModalViewModel
            {
                Person = new Person()
                {
                    Name = contact.Name,
                    Email = contact.Email,
                    Role = contact.Role
                },
                Roles = State.AppConfig.Roles
            };

            var modalConfig = new ModalOptions();
            modalConfig.Title = "Contact";
            modalConfig.OnCancel = async (e) =>
            {
                await personModalRef.CloseAsync();
            };
            modalConfig.OnOk = async (e) =>
            {
                contact.Name = templateOptions.Person.Name;
                contact.Email = templateOptions.Person.Email;
                contact.Role = templateOptions.Person.Role;

                await personModalRef.CloseAsync();
            };

            modalConfig.AfterClose = () =>
            {
                RemoveBlankContacts();

                InvokeAsync(StateHasChanged);

                return Task.CompletedTask;
            };

            personModalRef = await ModalService
                .CreateModalAsync<PersonModal, ViewModels.PersonModalViewModel>(
                    modalConfig, templateOptions);
        }

        private void RemoveBlankContacts()
        {
            List<Person> contactsToRemove = new List<Person>();
            foreach(Person contact in State.MetadataEdit.Dataset.Contacts)
            {
                if(string.IsNullOrWhiteSpace(contact.Name) &&
                    string.IsNullOrWhiteSpace(contact.Email) &&
                    string.IsNullOrWhiteSpace(contact.Role))
                {
                    contactsToRemove.Add(contact);
                }
            }
            foreach(Person remove in contactsToRemove)
            {
                State.MetadataEdit.Dataset.Contacts.Remove(remove);
            }
        }

        private async Task AddContactHandler()
        {
            if (State.MetadataEdit.Dataset.Contacts == null)
                State.MetadataEdit.Dataset.Contacts = new List<Person>();

            var contact = new Person();

            await OpenPersonModalTemplate(contact);

            State.MetadataEdit.Dataset.Contacts.Add(contact);
        }

        private void DeleteContactHandler(Person person)
        {
            State.MetadataEdit.Dataset.Contacts.Remove(person);
        }
        #endregion

        #region DatasetTag
        private string NewDatasetTag { get; set; }
        private string SavedDatasetTag { get; set; }

        private void AddDatasetTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag) &&
                !IsDuplicateDatasetTag(tag))
            {
                State.MetadataEdit.Dataset.Tags.Add(tag);
            }
        }
        private void AddDatasetTagHandler()
        {
            AddDatasetTag(NewDatasetTag);
            NewDatasetTag = "";
        }

        private void DatasetTagSelectedItemChangedHandler(string value)
        {
            AddDatasetTag(value);
            SavedDatasetTag = "";
        }

        private void DeleteDatasetTagHandler(string tag)
        {
            State.MetadataEdit.Dataset.Tags.Remove(tag);
        }

        private bool IsDuplicateDatasetTag(string tag)
        {
            var dup = State.MetadataEdit.Dataset.Tags.Find(s => s == tag);
            if (string.IsNullOrEmpty(dup))
                return false;
            else { return true; }
        }
        #endregion

        #region DatasetMethods
        private string NewDatasetMethod { get; set; }

        
        private void AddDatasetMethod(string method)
        {
            if(!string.IsNullOrWhiteSpace(method) &&
                !IsDuplicateDatasetMethod(method))
            {
                State.MetadataEdit.Dataset.Methods.Add(method);
                NewDatasetMethod = "";
            }
        }
        private bool IsDuplicateDatasetMethod(string method)
        {
            var dup = State.MetadataEdit.Dataset.Methods.Find(s => s == method);
            if (string.IsNullOrEmpty(dup))
                return false;
            else { return true; }
        }

        private void AddDatasetMethodHandler()
        {
            AddDatasetMethod(NewDatasetMethod);
        }
        private void DeleteDatasetMethodHandler(string method)
        {
            State.MetadataEdit.Dataset.Methods.Remove(method);
        }
        #endregion

        #region Parent Datasets
        private string NewParentDataset { get; set; }

        private void AddParentDataset(string parentDataset)
        {
            if (!string.IsNullOrWhiteSpace(parentDataset) &&
                !IsDuplicateParentDataset(parentDataset))
            {
                State.MetadataEdit.Dataset.ParentDatasets.Add(parentDataset);
                NewParentDataset = "";
            }
        }
        private bool IsDuplicateParentDataset(string parentDataset)
        {
            var dup = State.MetadataEdit.Dataset.ParentDatasets.Find(p => p == parentDataset);
            if (string.IsNullOrEmpty(dup))
                return false;
            else { return true; }
        }

        private void AddParentDatasetHandler()
        {
            AddParentDataset(NewParentDataset);
        }
        private void DeleteParentDatasetHandler(string parentDataset)
        {
            State.MetadataEdit.Dataset.ParentDatasets.Remove(parentDataset);
        }
        #endregion

        #region Derived Works
        private string NewDerivedWork { get; set; }

        private void AddDerivedWork(string derived)
        {
            if (!string.IsNullOrWhiteSpace(derived) &&
                !IsDuplicateDerivedWork(derived))
            {
                State.MetadataEdit.Dataset.DerivedWorks.Add(derived);
                NewDerivedWork = "";
            }
        }
        private bool IsDuplicateDerivedWork(string derived)
        {
            var dup = State.MetadataEdit.Dataset.DerivedWorks.Find(s => s == derived);
            if (string.IsNullOrEmpty(dup))
                return false;
            else { return true; }
        }

        private void AddDerivedWorkHandler()
        {
            AddDerivedWork(NewDerivedWork);
        }
        private void DeleteDerivedWorkHandler(string derived)
        {
            State.MetadataEdit.Dataset.DerivedWorks.Remove(derived);
        }
        #endregion

        #region Variable Functions
        private ModalRef variableModalRef;
        private async Task OpenVariableModalTemplate(Variable variable)
        {
            var templateOptions = new ViewModels.VariableModalViewModel
            {
                Variable = new Variable()
                {
                    Name = variable.Name,
                    Description = variable.Description,
                    Units = variable.Units,
                    Height = variable.Height,
                    Tags = variable.Tags,
                    Methods = variable.Methods,
                    QCApplied = variable.QCApplied,
                    ProcessingLevel = variable.ProcessingLevel,
                    VariableType = variable.VariableType
                },
                ProcessingLevels = State.AppConfig.ProcessingLevels,
                QCFlags = State.AppConfig.QCTags,
                VariableTypes = State.AppConfig.VariableTypes,
                Tags = State.AppConfig.Tags,
                SelectedTags = variable.Tags ??= new List<string>(),
                SelectedQCApplied = variable.QCApplied ??= new List<string>()
            };

            var modalConfig = new ModalOptions();
            modalConfig.Title = "Variable";
            modalConfig.Width = "70%";
            modalConfig.OnCancel = async (e) =>
            {
                await variableModalRef.CloseAsync();
            };
            modalConfig.OnOk = async (e) =>
            {
                variable.Name = templateOptions.Variable.Name;
                variable.Description = templateOptions.Variable.Description;
                variable.Units = templateOptions.Variable.Units;
                variable.Height = templateOptions.Variable.Height;
                variable.Tags = templateOptions.SelectedTags.ToList();
                variable.Methods = templateOptions.Variable.Methods;
                variable.QCApplied = templateOptions.SelectedQCApplied.ToList();
                variable.ProcessingLevel = templateOptions.Variable.ProcessingLevel;
                variable.VariableType = templateOptions.Variable.VariableType;
                await variableModalRef.CloseAsync();
            };

            modalConfig.AfterClose = () =>
            {
                RemoveBlankVariables();

                InvokeAsync(StateHasChanged);

                return Task.CompletedTask;
            };

            variableModalRef = await ModalService
                .CreateModalAsync<VariableModal, ViewModels.VariableModalViewModel>(
                    modalConfig, templateOptions);
        }

        private void RemoveBlankVariables()
        {
            List<Variable> variablesToRemove = new List<Variable>();
            foreach (Variable variable in State.MetadataEdit.Dataset.Variables)
            {
                if (string.IsNullOrWhiteSpace(variable.Name) &&
                    string.IsNullOrWhiteSpace(variable.Description) &&
                    string.IsNullOrWhiteSpace(variable.Units))
                {
                    variablesToRemove.Add(variable);
                }
            }
            foreach (Variable remove in variablesToRemove)
            {
                State.MetadataEdit.Dataset.Variables.Remove(remove);
            }
        }

        private async Task AddVariableHandler()
        {
            var variable = new Variable();

            await OpenVariableModalTemplate(variable);

            State.MetadataEdit.Dataset.Variables.Add(variable);
        }

        private void DeleteVariableHandler(Variable variable)
        {
            State.MetadataEdit.Dataset.Variables.Remove(variable);
        }
        #endregion

        #region Geometry
        private string GeometryTemplate { get; set; }
        private void OnGeometryItemChangedHandler(string value)
        {
            State.MetadataEdit.Dataset.Geometry = value;
        }

        public void Dispose()
        {
            //this.EditContext.OnFieldChanged -=
            //     EditContext_OnFieldChange;
            State.StateChanged -= async (source, property) =>
                 await LastUpdated_StateChanged(source, property);
        }
        #endregion

        private async Task<string> SaveDataset()
        {
            // TODO: Validate first!

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() },
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string jsonString;

            var now = DateTime.UtcNow;

            State.MetadataEdit.ModifiedDate = now;
            if (State.MetadataEdit.CreationDate == DateTime.MinValue)
                State.MetadataEdit.CreationDate = now;

            jsonString = JsonSerializer.Serialize(State.MetadataEdit, options);

            var buffer = Encoding.UTF8.GetBytes(jsonString);
            var stream = new MemoryStream(buffer);
            var fileBytes = stream.ToArray();
            
            await JS.InvokeAsync<string>(
                "saveAsFile", 
                $"{State.MetadataEdit.Dataset.Name}.midden",
                Convert.ToBase64String(fileBytes));

            return jsonString;
        }

        private ModalRef metadataDetailsModalRef;
        private async Task OpenMetadataDetailsModalTemplate(Metadata metadata)
        {
            var templateOptions = new ViewModels.MetadataDetailsViewModel
            {
                Metadata = metadata
            };

            var modalConfig = new ModalOptions();
            modalConfig.Title = "Metadata Preview";
            modalConfig.Width = "90%";
            modalConfig.DestroyOnClose = true;
            modalConfig.OnCancel = async (e) =>
            {
                await metadataDetailsModalRef.CloseAsync();
            };
            modalConfig.OnOk = async (e) =>
            {
                await metadataDetailsModalRef.CloseAsync();
            };

            modalConfig.AfterClose = () =>
            {
                InvokeAsync(StateHasChanged);

                return Task.CompletedTask;
            };

            metadataDetailsModalRef = await ModalService
                .CreateModalAsync<MetadataDetailsModal, ViewModels.MetadataDetailsViewModel>(
                    modalConfig, templateOptions);
        }
    }
}
