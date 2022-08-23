using Caf.Midden.Core.Models.v0_2;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class MainLayout : IDisposable
    {
        //string DebugMsg { get; set; } = "";

        bool collapsed;

        private async Task LastUpdated_StateChanged(
            ComponentBase source,
            string lastUpdated)
        {
            if(source != this)
            {
                await InvokeAsync(StateHasChanged);
                //DebugMsg = "StateHasChanged";
            }
        }

        protected override void OnInitialized()
        {
            State.StateChanged += async (source, property)
                => await LastUpdated_StateChanged(source, property);
        }
        public void Dispose()
        {
            State.StateChanged -= async (source, property)
                => await LastUpdated_StateChanged(source, property);
        }

        //void OnCollapse(bool isCollapsed)
        //{
        //    // Nothing
        //}

        //void toggle()
        //{
        //    collapsed = !collapsed;
        //}
    }
}
