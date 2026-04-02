using Caf.Midden.Cli.Common;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;

namespace Caf.Midden.Cli.Services;

public sealed class LocalFileSystemCrawler : ICrawl
{
    private const string MiddenFileExtension = ".midden";
    private const string MippenFileSearchTerm = "DESCRIPTION.md";

    private readonly string rootDirectory;

    public LocalFileSystemCrawler(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        if (!Directory.Exists(rootDirectory))
        {
            throw new DirectoryNotFoundException($"Directory '{rootDirectory}' does not exist.");
        }

        this.rootDirectory = rootDirectory;
    }

    public IReadOnlyList<string> GetFileNames(string fileExtension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);

        var files = Directory.EnumerateFiles(rootDirectory, $"*{fileExtension}", SearchOption.AllDirectories).ToList();
        Console.WriteLine($"Found a total of {files.Count} files");
        return files;
    }

    public IReadOnlyList<Metadata> GetMetadatas(IMetadataParser parser)
    {
        List<Metadata> metadatas = [];

        foreach (var file in GetFileNames(MiddenFileExtension))
        {
            var metadata = parser.Parse(File.ReadAllText(file));
            var relativePath = Path.GetRelativePath(rootDirectory, file);

            metadata.Dataset.DatasetPath = relativePath.Replace(MiddenFileExtension, string.Empty, StringComparison.OrdinalIgnoreCase);
            metadatas.Add(metadata);
        }

        return metadatas;
    }

    public IReadOnlyList<Project> GetProjects(ProjectReader reader)
    {
        List<Project> projects = [];

        foreach (var file in GetFileNames(MippenFileSearchTerm))
        {
            using var stream = File.OpenRead(file);
            var project = reader.Read(stream);

            if (project is not null)
            {
                projects.Add(project);
            }
        }

        return projects;
    }
}
