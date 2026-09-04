using Caf.Midden.Core.Models.v0_2;
using System.Text;

namespace Caf.Midden.Core.Services;

public sealed class ProjectReader
{
    private readonly IParseProjects parser;

    public ProjectReader(IParseProjects parser)
    {
        this.parser = parser;
    }

    public Project? Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var streamReader = new StreamReader(stream, Encoding.UTF8);
        return parser.Parse(streamReader);
    }

    public Project? Read(string fileString)
    {
        ArgumentNullException.ThrowIfNull(fileString);

        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(fileString));
        using var streamReader = new StreamReader(memoryStream, Encoding.UTF8);
        return parser.Parse(streamReader);
    }
}
