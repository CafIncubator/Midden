using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Services.Metadata
{
    public interface IMetadataParser
    {
        Models.v0_1_0alpha4.Metadata Parse(string json);
    }
}
