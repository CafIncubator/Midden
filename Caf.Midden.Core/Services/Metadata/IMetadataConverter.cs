using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Services.Metadata
{
    public interface IMetadataConverter
    {
        Models.v0_2.Metadata Convert(
            Models.v0_1_0alpha3.Metadata metadata);
        Models.v0_2.Metadata Convert(
            Models.v0_1_0alpha4.Metadata metadata);
        Models.v0_2.Metadata Convert(
            Models.v0_1.Metadata metadata);
        Models.v0_2.Metadata Convert(
            Models.v0_2.Metadata metadata);
    }
}
