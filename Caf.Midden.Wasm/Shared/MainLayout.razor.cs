using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared
{
    public partial class MainLayout
    {
        bool collapsed;
        
        void OnCollapse(bool isCollapsed)
        {
            // Nothing
        }

        void toggle()
        {
            collapsed = !collapsed;
        }
    }
}
