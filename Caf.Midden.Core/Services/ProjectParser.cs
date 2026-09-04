using Caf.Midden.Core.Models.v0_2;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Caf.Midden.Core.Services;

public sealed partial class ProjectParser : IParseProjects
{
    private const string ProjectVarName = "project";
    private const string ProjectVarLastModified = "lastModified";
    private const string ProjectVarStatus = "status";

    public Project? Parse(string contents)
    {
        ArgumentNullException.ThrowIfNull(contents);

        using var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contents));
        using var streamReader = new StreamReader(memoryStream, System.Text.Encoding.UTF8);
        return Parse(streamReader);
    }

    public Project? Parse(StreamReader streamReader)
    {
        ArgumentNullException.ThrowIfNull(streamReader);

        if (streamReader.ReadLine() != "---")
        {
            return null;
        }

        string? projectName = null;
        DateTime? lastModified = null;
        string? projectStatus = null;

        string? line;
        while ((line = streamReader.ReadLine()) is not null)
        {
            if (line == "---")
            {
                break;
            }

            if (line.StartsWith(ProjectVarName + ":", StringComparison.Ordinal))
            {
                projectName = ParseFrontMatter(line);
                continue;
            }

            if (line.StartsWith(ProjectVarLastModified + ":", StringComparison.Ordinal))
            {
                var modifiedDateTime = ParseFrontMatter(line);

                if (!string.IsNullOrWhiteSpace(modifiedDateTime)
                    && DateTime.TryParse(modifiedDateTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDate))
                {
                    lastModified = parsedDate;
                }

                continue;
            }

            if (line.StartsWith(ProjectVarStatus + ":", StringComparison.Ordinal))
            {
                projectStatus = ParseFrontMatter(line);
            }
        }

        if (string.IsNullOrWhiteSpace(projectName))
        {
            return null;
        }

        return new Project
        {
            Name = projectName,
            LastModified = lastModified,
            ProjectStatus = projectStatus,
            Description = streamReader.ReadToEnd(),
        };
    }

    private static string? ParseFrontMatter(string line)
    {
        var match = FrontMatterValueRegex().Match(line);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    [GeneratedRegex("\"(.*?)\"")]
    private static partial Regex FrontMatterValueRegex();
}
