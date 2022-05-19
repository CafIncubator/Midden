using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
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
        List<Metadata> GetMetadatas(IMetadataParser parser);
        List<Project> GetProjects(ProjectReader reader);
    }
}
