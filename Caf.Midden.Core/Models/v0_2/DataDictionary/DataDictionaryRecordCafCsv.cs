using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Models.v0_2.DataDictionary
{
    public class DataDictionaryRecordCafCsv
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Units { get; set; }
        public string Tags { get; set; }
        public string Methods { get; set; }
        public string TemporalResolution { get; set; }
        public string TemporalExtent { get; set; }
        public string SpatialRepeats { get; set; }
        public string IsQCSpecified { get; set; }
        public string QCApplied { get; set; }
        public string ProcessingLevel { get; set; }
        public string VariableType { get; set; }
    }
}
