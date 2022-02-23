using Caf.Midden.Core.Models.v0_2;
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
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Configuration config = await ConfigReader.Read();
            State.UpdateAppConfig(this, config);
        }
    }
}
