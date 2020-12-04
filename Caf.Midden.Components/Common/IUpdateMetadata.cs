using Caf.Midden.Core.Models.v0_1_0alpha4;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Components.Common
{
    public interface IUpdateMetadata : INotifyStateChange
    {
        Metadata Metadata { get; }

        void SetMetadata(ComponentBase source, Metadata value);
    }
}
