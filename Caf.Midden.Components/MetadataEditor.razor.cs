using Caf.Midden.Core.Models.v0_1_0alpha4;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using AntDesign;
using Microsoft.AspNetCore.Components.Web;
using Caf.Midden.Components.Modals;

namespace Caf.Midden.Components
{
    public partial class MetadataEditor : ComponentBase, IDisposable
    {
        [Parameter]
        public Configuration AppConfig { get; set; }

        private Metadata metadata { set; get; }
        
        [Parameter]
        //public Metadata Metadata { get; set; }
        public Metadata Metadata
        {
            get => metadata;
            set
            {
                if (metadata == value) return;
                metadata = value;
                State.UpdateLastUpdated(this, DateTime.UtcNow);
                MetadataChanged.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<Metadata> MetadataChanged { get; set; }

        //private string LastUpdated { get; set; } =
        //    DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

        private EditContext EditContext { get; set; }
        Form<Metadata> _form;
        
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
                            Method = "Tiagatron 3000",
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
            
            this.Metadata = metadata;
        }

        protected override void OnInitialized()
        {
            this.EditContext = new EditContext(this.Metadata);
            this.EditContext.OnFieldChanged +=
                EditContext_OnFieldChange;
        }

        private void EditContext_OnFieldChange(
            object sender, 
            FieldChangedEventArgs e)
        {
            MetadataChanged.InvokeAsync(this.Metadata);
            //LastUpdated = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            State.UpdateLastUpdated(this, DateTime.UtcNow);
        }

        private void NewMetadata()
        {
            DateTime dt = DateTime.UtcNow;

            this.Metadata = new Metadata()
            {
                Dataset = new Dataset(),
                CreationDate = dt,
                ModifiedDate = dt
            };
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
                Roles = AppConfig.Roles
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
            foreach(Person contact in this.Metadata.Dataset.Contacts)
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
                this.Metadata.Dataset.Contacts.Remove(remove);
            }
        }

        private async Task AddContactHandler()
        {
            if (this.Metadata.Dataset.Contacts == null)
                this.Metadata.Dataset.Contacts = new List<Person>();

            var contact = new Person();

            await OpenPersonModalTemplate(contact);

            this.Metadata.Dataset.Contacts.Add(contact);
        }

        private void DeleteContactHandler(Person person)
        {
            this.Metadata.Dataset.Contacts.Remove(person);
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
                this.Metadata.Dataset.Tags.Add(tag);
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
            this.Metadata.Dataset.Tags.Remove(tag);
        }

        private bool IsDuplicateDatasetTag(string tag)
        {
            var dup = this.Metadata.Dataset.Tags.Find(s => s == tag);
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
                this.Metadata.Dataset.Methods.Add(method);
                NewDatasetMethod = "";
            }
        }
        private bool IsDuplicateDatasetMethod(string method)
        {
            var dup = this.Metadata.Dataset.Methods.Find(s => s == method);
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
            this.Metadata.Dataset.Methods.Remove(method);
        }
        #endregion

        #region Derived Works
        private string NewDerivedWork { get; set; }


        private void AddDerivedWork(string derived)
        {
            if (!string.IsNullOrWhiteSpace(derived) &&
                !IsDuplicateDerivedWork(derived))
            {
                this.Metadata.Dataset.DerivedWorks.Add(derived);
                NewDerivedWork = "";
            }
        }
        private bool IsDuplicateDerivedWork(string derived)
        {
            var dup = this.Metadata.Dataset.DerivedWorks.Find(s => s == derived);
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
            this.Metadata.Dataset.DerivedWorks.Remove(derived);
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
                    Method = variable.Method
                },
                ProcessingLevels = AppConfig.ProcessingLevels,
                QCFlags = AppConfig.QCTags,
                Tags = AppConfig.Tags,
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
                variable.Method = templateOptions.Variable.Method;

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
            foreach (Variable variable in this.Metadata.Dataset.Variables)
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
                this.Metadata.Dataset.Variables.Remove(remove);
            }
        }

        private async Task AddVariableHandler()
        {
            var variable = new Variable();

            await OpenVariableModalTemplate(variable);

            this.Metadata.Dataset.Variables.Add(variable);
        }

        private void DeleteVariableHandler(Variable variable)
        {
            this.Metadata.Dataset.Variables.Remove(variable);
        }
        #endregion

        #region Geometry
        private string GeometryTemplate { get; set; }
        private void OnGeometryItemChangedHandler(string value)
        {
            this.Metadata.Dataset.Geometry = value;
        }

        public void Dispose()
        {
            this.EditContext.OnFieldChanged -=
                EditContext_OnFieldChange;
        }
        #endregion

    }
}
