using Caf.Midden.Core.Models.v0_1_0alpha4;
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
        /*private Configuration configuration { set; get; }
        
        [Parameter]
        public Configuration Configuration
        {
            get => configuration;
            set
            {
                if (configuration == value) return;
                configuration = value;
                ConfigurationChanged.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<Configuration> ConfigurationChanged { get; set; }*/

        protected override async Task OnInitializedAsync()
        {
            Configuration config = await ConfigReader.Read();
            State.UpdateAppConfig(this, config);
        }
    }
}
