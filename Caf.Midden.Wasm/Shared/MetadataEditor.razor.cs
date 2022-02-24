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
    public partial class MetadataEditor : ComponentBase//, IDisposable
    {
        //[Parameter]
        //public Configuration AppConfig { get; set; }

        //private Metadata metadata { set; get; }
        
        //[Parameter]
        ////public Metadata Metadata { get; set; }
        //public Metadata Metadata
        //{
        //    get => metadata;
        //    set
        //    {
        //        if (metadata == value) return;
        //        metadata = value;
        //        State.UpdateLastUpdated(this, DateTime.UtcNow);
        //        MetadataChanged.InvokeAsync(value);
        //    }
        //}

        //[Parameter]
        //public EventCallback<Metadata> MetadataChanged { get; set; }

        //private string LastUpdated { get; set; } =
        //    DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

        //private EditContext EditContext { get; set; }
        
        public void LoadMetadataTest()
        {
            // Mock for now
            var now = DateTime.UtcNow;

            Metadata metadata = new Metadata()
            {
                CreationDate = now,
                ModifiedDate = now,
                Dataset = new Dataset()
                {
                    Zone = "Raw",
                    Name = "Test",
                    Contacts = new List<Person>()
                    {
                        new Person()
                        {
                            Name = "Test",
                            Email = "Test@test.com",
                            Role = "User"
                        }
                    },
                    Tags = new List<string>()
                    {
                        "Foo",
                        "[ISO]someThing"
                    },
                    Variables = new List<Variable>()
                    {
                        new Variable()
                        {
                            Name = "Var1",
                            Description = "Varvar",
                            Units = "unitless",
                            QCApplied = new List<string>()
                            {
                                "Assurance", "Review"
                            },
                            ProcessingLevel = "Calculated",
                            Methods = new List<string>(){"Tiagatron 3000" },
                            Tags = new List<string>()
                            {
                                "Met", "CAF", "Test"
                            }
                        },
                        new Variable()
                        {
                            Name = "Var3",
                            Description = "Varvarbst",
                            Units = "unitless",
                            QCApplied = new List<string>()
                            {
                                "Assurance"
                            },
                            ProcessingLevel = "Unknown",
                            Tags = new List<string>()
                            {
                                "Met"
                            }
                        },
                        new Variable()
                        {
                            Name = "Var4",
                            Description = "Calculation of the slope and specific catchment area based Topographic Wetness Index. It shows water accumulation. This can be useful for soil or flood mapping",
                            Units = "unitless",
                            QCApplied = new List<string>()
                            {
                                "Assurance"
                            },
                            ProcessingLevel = "Unknown",
                            Tags = new List<string>()
                            {
                                "Met"
                            }
                        }
                    }
                }
            };

            State.UpdateMetadataEdit(this, metadata);
        }

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
            State.StateChanged += async (source, property) =>
                await LastUpdated_StateChanged(source, property);
        }

        private void EditContext_OnFieldChange(
            object sender, 
            FieldChangedEventArgs e)
        {
            //MetadataChanged.InvokeAsync(this.Metadata);

            //LastUpdated = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
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
                    ProcessingLevel = variable.ProcessingLevel
                },
                ProcessingLevels = State.AppConfig.ProcessingLevels,
                QCFlags = State.AppConfig.QCTags,
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
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                IgnoreNullValues = true,
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
                fileBytes);

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
