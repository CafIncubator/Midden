using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Wasm.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class ConfigurationLoader
    {
        [Inject]
        public AppBootstrapService Bootstrapper { get; set; } = default!;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await Bootstrapper.EnsureConfigurationLoadedAsync(this);
        }
    }
}
