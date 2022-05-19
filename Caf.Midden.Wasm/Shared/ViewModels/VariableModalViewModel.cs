using Caf.Midden.Core.Models.v0_2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared.ViewModels
{
    public class VariableModalViewModel
    {
        public Variable Variable { get; set; }
        public List<string> Tags { get; set; }
        public List<string> ProcessingLevels { get; set; }
        public List<string> QCFlags { get; set; }
        public IEnumerable<string> SelectedTags { get; set; }
        public IEnumerable<string> SelectedQCApplied { get; set; }
    }
}
