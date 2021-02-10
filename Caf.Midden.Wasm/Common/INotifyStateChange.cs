﻿using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Common
{
    public interface INotifyStateChange
    {
        event Action<ComponentBase, string> StateChanged;
        void NotifyStateChanged(ComponentBase source, string property);
    }
}
