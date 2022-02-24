using Caf.Midden.Core.Models.v0_2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Cli.Common
{
    public interface ICrawl
    {
        List<string> GetFileNames(string fileExtension);
        List<Metadata> GetMetadatas();
        List<Project> GetProjects();
    }
}
