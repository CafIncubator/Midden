using Caf.Midden.Core.Models.v0_1_0alpha4;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Components
{
    public partial class ConfigurationLoader
    {
        private Configuration configuration { set; get; }
        
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
        public EventCallback<Configuration> ConfigurationChanged { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            var config = await ConfigReader.Read();
            this.Configuration = config;
        }
    }
}
