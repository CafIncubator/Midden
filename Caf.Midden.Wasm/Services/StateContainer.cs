using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caf.Midden.Core.Models.v0_1_0alpha4;
using Microsoft.AspNetCore.Components;

namespace Caf.Midden.Wasm.Services
{
    public class StateContainer
    {
        public Metadata Metadata { get; private set; }

        public void SetMetadata(ComponentBase source, Metadata value)
        {
            this.Metadata = value;

            NotifyStateChanged(source, "Metadata");
        }

        public event Action<ComponentBase, string> StateChanged;
        private void NotifyStateChanged(
            ComponentBase source,
            string property) => StateChanged?.Invoke(source, property);
    }
}
