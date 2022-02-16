using AntDesign;
using Caf.Midden.Core.Models.v0_1;
using Caf.Midden.Wasm.Shared.Modals;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class ProjectDetails : IDisposable
    {
        [Parameter]
        public string ProjectName { get; set; }

        public Project Project { get; set; }

        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property)
                => await StateChanged(source, property);

            if (State?.Catalog != null)
            {
                SetProject();
            }
        }

        private async Task StateChanged(
            ComponentBase source,
            string property)
        {
            if (source != this)
            {
                if (property == "UpdateCatalog")
                {
                    SetProject();
                }

                await InvokeAsync(StateHasChanged);
            }


        }

        private void SetProject()
        {
            Project = State.Catalog.Projects
                .Where(p =>
                    (String.IsNullOrEmpty(p.Name) || p.Name.ToLower() == this.ProjectName.ToLower()))
                .FirstOrDefault();
        }

        public void Dispose()
        {
            State.StateChanged -= async (source, property)
                => await StateChanged(source, property);
        }
    }
}
