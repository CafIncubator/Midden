using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Services.Metadata
{
    public interface IMetadataParser
    {
        Models.v0_2.Metadata Parse(string json);
    }
}
