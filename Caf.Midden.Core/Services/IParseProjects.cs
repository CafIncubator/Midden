namespace Caf.Midden.Core.Services;

public interface IParseProjects
{
    Models.v0_2.Project? Parse(string contents);
    Models.v0_2.Project? Parse(StreamReader contents);
}
