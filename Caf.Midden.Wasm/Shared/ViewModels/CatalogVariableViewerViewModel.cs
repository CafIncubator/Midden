using Caf.Midden.Core.Models.v0_2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caf.Midden.Wasm.Shared.ViewModels
{
    public class CatalogVariableViewerViewModel
    {
        public List<CatalogVariable> CatalogVariables { get; set; } = new List<CatalogVariable>();

        public List<CatalogVariable> FilteredCatalogVariables { get; set; } = new List<CatalogVariable>();
        public string SearchTerm { get; set; }
    }

    public class CatalogVariable
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Units { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Methods { get; set; } = new List<string>();
        public string TemporalResolution { get; set; }
        public string TemporalExtent { get; set; }

        public List<string> QCApplied { get; set; } = new List<string>();

        public string ProcessingLevel { get; set; }

        public string VariableType { get; set; }

        public string Zone { get; set; }

        public string DatasetName { get; set; }

        public string ProjectName { get; set; }

        public CatalogVariable DeepCopy()
        {
            CatalogVariable other = (CatalogVariable)this.MemberwiseClone();

            other.Tags = new List<string>(this.Tags);
            other.Methods = new List<string>(this.Methods);
            other.QCApplied = new List<string>(this.QCApplied);

            return other;
        }
    }
}
