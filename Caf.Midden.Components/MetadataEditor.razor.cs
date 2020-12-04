using Caf.Midden.Core.Models.v0_1_0alpha4;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace Caf.Midden.Components
{
    public partial class MetadataEditor : ComponentBase
    {
        void LoadMetadataFile()
        {
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
                            Role = "Tool"
                        }
                    }
                }
            };

            State.SetMetadata(this, metadata);
        }
        private async Task State_StateChanged(
            ComponentBase source,
            string property)
        {
            if(source != this)
            {
                // Do work here, provided change of state. Perhaps inspect Metadata to see if changed and save to local cache?
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
