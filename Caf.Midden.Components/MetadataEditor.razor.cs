﻿using Caf.Midden.Core.Models.v0_1_0alpha4;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using AntDesign;
using Microsoft.AspNetCore.Components.Web;

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

        public void Dispose()
        {
            this.EditContext.OnFieldChanged -=
                EditContext_OnFieldChange;
        }

        private void AddContactHandler()
        {
            if (this.Metadata.Dataset.Contacts == null)
                this.Metadata.Dataset.Contacts = new List<Person>();
            this.Metadata.Dataset.Contacts.Add(new Person());
        }

        private void DeleteContactHandler(Person person)
        {
            this.Metadata.Dataset.Contacts.Remove(person);
        }

        private void OnGeometryItemChangedHandler(string value)
        {
            this.Metadata.Dataset.Geometry = value;
        }
    }
}
