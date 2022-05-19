using Caf.Midden.Core.Models.v0_2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared.ViewModels
{
    public class PersonModalViewModel
    {
        public Person Person { get; set; }
        public List<string> Roles { get; set; }

    }
}
