using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;

namespace Caf.Midden.Cli.Common;

public interface ICrawl
{
    IReadOnlyList<string> GetFileNames(string fileExtension);
    IReadOnlyList<Metadata> GetMetadatas(IMetadataParser parser);
    IReadOnlyList<Project> GetProjects(ProjectReader reader);
}
