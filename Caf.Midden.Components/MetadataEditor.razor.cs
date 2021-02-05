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

        #region Tag Functions
        private ModalRef tagModalRef;
        private async Task OpenTagModalTemplate(List<string> tags)
        {
            var templateOptions = new ViewModels.TagModalViewModel
            {
                Tag = "",
                Tags = AppConfig.Tags
            };

            var modalConfig = new ModalOptions();
            modalConfig.Title = "Tag";
            modalConfig.OnCancel = async (e) =>
            {
                await tagModalRef.CloseAsync();
            };
            modalConfig.OnOk = async (e) =>
            {
                if(!string.IsNullOrWhiteSpace(templateOptions.Tag) &&
                    !IsDuplicateTag(templateOptions.Tag))
                {
                    tags.Add(templateOptions.Tag);
                }
                

                await tagModalRef.CloseAsync();
            };

            modalConfig.AfterClose = () =>
            {
                InvokeAsync(StateHasChanged);

                return Task.CompletedTask;
            };

            tagModalRef = await ModalService
                .CreateModalAsync<TagModal, ViewModels.TagModalViewModel>(
                    modalConfig, templateOptions);
        }

        private bool IsDuplicateTag(string tag)
        {
            var dup = this.Metadata.Dataset.Tags.Find(s => s == tag);
            if (string.IsNullOrEmpty(dup))
                return false;
            else { return true; }
        }

        private async Task AddTagHandler()
        {
            await OpenTagModalTemplate(this.Metadata.Dataset.Tags);
        }

        private void DeleteTagHandlerIndex(int index)
        {
            this.Metadata.Dataset.Tags.RemoveAt(index);
        }
        #endregion

        #region Method Functions
        private ModalRef methodModalRef;
        private async Task OpenMethodModalTemplate(List<string> methods)
        {
            var templateOptions = new ViewModels.MethodModalViewModel
            {
                Method = ""
            };

            var modalConfig = new ModalOptions();
            modalConfig.Title = "Method";
            modalConfig.OnCancel = async (e) =>
            {
                await methodModalRef.CloseAsync();
            };
            modalConfig.OnOk = async (e) =>
            {
                if (!string.IsNullOrWhiteSpace(templateOptions.Method))
                {
                    methods.Add(templateOptions.Method);
                }

                await methodModalRef.CloseAsync();
            };

            modalConfig.AfterClose = () =>
            {
                InvokeAsync(StateHasChanged);

                return Task.CompletedTask;
            };

            methodModalRef = await ModalService
                .CreateModalAsync<MethodModal, ViewModels.MethodModalViewModel>(
                    modalConfig, templateOptions);
        }

        private async Task AddMethodHandler()
        {
            await OpenMethodModalTemplate(this.Metadata.Dataset.Methods);
        }

        private void DeleteMethodHandlerIndex(int index)
        {
            this.Metadata.Dataset.Methods.RemoveAt(index);
        }
        #endregion

        #region Derived Works Functions
        private ModalRef derivedWorkModalRef;
        private async Task OpenDerivedWorkModalTemplate(List<string> derivedWorks)
        {
            var templateOptions = new ViewModels.DerivedWorkModalViewModel
            {
                DerivedWork = ""
            };

            var modalConfig = new ModalOptions();
            modalConfig.Title = "Derived Work";
            modalConfig.OnCancel = async (e) =>
            {
                await derivedWorkModalRef.CloseAsync();
            };
            modalConfig.OnOk = async (e) =>
            {
                if (!string.IsNullOrWhiteSpace(templateOptions.DerivedWork))
                {
                    derivedWorks.Add(templateOptions.DerivedWork);
                }

                await derivedWorkModalRef.CloseAsync();
            };

            modalConfig.AfterClose = () =>
            {
                InvokeAsync(StateHasChanged);

                return Task.CompletedTask;
            };

            derivedWorkModalRef = await ModalService
                .CreateModalAsync<DerivedWorkModal, ViewModels.DerivedWorkModalViewModel>(
                    modalConfig, templateOptions);
        }

        private async Task AddDerivedWorkHandler()
        {
            await OpenDerivedWorkModalTemplate(this.Metadata.Dataset.DerivedWorks);
        }

        private void DeleteDerivedWorkHandlerIndex(int index)
        {
            this.Metadata.Dataset.DerivedWorks.RemoveAt(index);
        }
        #endregion

        private void OnGeometryItemChangedHandler(string value)
        {
            this.Metadata.Dataset.Geometry = value;
        }

        public void Dispose()
        {
            this.EditContext.OnFieldChanged -=
                EditContext_OnFieldChange;
        }
    }
}
