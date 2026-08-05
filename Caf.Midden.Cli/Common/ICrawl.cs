using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;

namespace Caf.Midden.Cli.Common;

/// <summary>
/// <c>GetFileNames</c> was previously public even though its semantics differ per crawler
/// (local returns full paths, Data Lake returns relative paths, Drive returns file *IDs*),
/// which made it an implementation detail leaking through the interface. It has been removed
/// from the public contract; each crawler still exposes it privately for its own
/// <c>GetMetadatas</c>/<c>GetProjects</c> implementations.
/// </summary>
public interface ICrawl : IDisposable
{
    IReadOnlyList<Metadata> GetMetadatas(IMetadataParser parser);
    IReadOnlyList<Project> GetProjects(ProjectReader reader);
}
