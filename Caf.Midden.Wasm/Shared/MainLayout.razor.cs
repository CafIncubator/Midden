using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Wasm.Services;
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

        private IDisposable? _stateSubscription;

        private bool collapsed = false;

        private async Task OnStateChanged(AppStateChangedEventArgs args)
        {
            await InvokeAsync(StateHasChanged);
            //DebugMsg = "StateHasChanged";
        }

        protected override void OnInitialized()
        {
            _stateSubscription = State.Subscribe(
                this,
                OnStateChanged,
                AppStateChange.LastUpdated,
                AppStateChange.AppConfig,
                AppStateChange.Catalog,
                AppStateChange.MetadataEdit,
                AppStateChange.ProjectEdit);
        }

        public void Dispose()
        {
            _stateSubscription?.Dispose();
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
