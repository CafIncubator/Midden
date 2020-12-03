using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Services.Metadata
{
    public class MetadataReader
    {
        private readonly IMetadataParser parser;

        public MetadataReader(
            IMetadataParser parser)
        {
            this.parser = parser;
        }

        public Models.v0_1_0alpha4.Metadata Read()
        {
            throw new NotImplementedException();
        }
    }
}
