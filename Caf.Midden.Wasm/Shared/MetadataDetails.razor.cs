using Caf.Midden.Core.Models.v0_1;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class MetadataDetails : ComponentBase
    {
        [Parameter]
        public Metadata Metadata { get; set; }
    }
}
