using Caf.Midden.Core.Models.v0_2;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Common
{
    public interface IUpdateAppConfig : INotifyStateChange
    {
        Configuration AppConfig { get; }

        void UpdateAppConfig(ComponentBase source, Configuration value);
    }
}
