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

        public bool VarsHaveMethods { get; set; }

        public bool VarsHaveQCApplied { get; set; }

        public bool VarsHaveProcessingLevel { get; set; }

        public bool VarsHaveTags { get; set; }

        public bool VarsHaveHeight { get; set; }


        protected override void OnInitialized()
        {
            if(Metadata?.Dataset?.Variables != null)
            {
                int numMethods = Metadata.Dataset.Variables.SelectMany(v => v.Methods).Count();
                if (numMethods > 0) this.VarsHaveMethods = true;

                int numQCApplied = Metadata.Dataset.Variables.SelectMany(v => v.QCApplied).Count();
                if (numQCApplied > 0) this.VarsHaveQCApplied = true;

                int numProcessingLevel = Metadata.Dataset.Variables.Where(v => !string.IsNullOrEmpty(v.ProcessingLevel)).Count();
                if (numProcessingLevel > 0) this.VarsHaveProcessingLevel = true;

                int numTags = Metadata.Dataset.Variables.SelectMany(v => v.Tags).Count();
                if (numTags > 0) this.VarsHaveTags = true;

                int numHeight = Metadata.Dataset.Variables.Where(v => v.Height != null).Count();
                if (numHeight > 0) this.VarsHaveHeight = true;
            }
        }
    }
}
