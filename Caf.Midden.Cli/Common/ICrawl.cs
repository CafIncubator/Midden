using Caf.Midden.Core.Models.v0_1_0alpha4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Cli.Common
{
    public interface ICrawl
    {
        List<string> GetFileNames();
        List<Metadata> GetMetadatas();
    }
}
