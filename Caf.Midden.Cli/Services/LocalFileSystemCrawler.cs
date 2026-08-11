using Caf.Midden.Cli.Common;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;

namespace Caf.Midden.Cli.Services;

public sealed class LocalFileSystemCrawler : ICrawl
{
    private readonly string rootDirectory;
    private readonly ICrawlLogger logger;
    private readonly IFileSystem fileSystem;

    public LocalFileSystemCrawler(string rootDirectory, ICrawlLogger? logger = null, IFileSystem? fileSystem = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        this.fileSystem = fileSystem ?? PhysicalFileSystem.Instance;

        if (!this.fileSystem.DirectoryExists(rootDirectory))
        {
            throw new DirectoryNotFoundException($"Directory '{rootDirectory}' does not exist.");
        }

        this.rootDirectory = rootDirectory;
        this.logger = logger ?? ConsoleCrawlLogger.Instance;
    }

    public void Dispose()
    {
        // No unmanaged resources to release; local disk access needs no disposal.
    }

    internal IReadOnlyList<string> GetFileNames(string fileExtension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);

        var files = EnumerateFiles(rootDirectory, fileExtension).ToList();
        logger.Info($"Found a total of {files.Count} files");
        return files;
    }

    /// <summary>
    /// Walks the tree manually rather than using <c>Directory.EnumerateFiles(..., AllDirectories)</c>,
    /// which follows reparse points. A symlink or junction under the configured root could
    /// otherwise escape the root entirely, or loop forever if it points back at an ancestor.
    /// </summary>
    private IEnumerable<string> EnumerateFiles(string directory, string fileExtension)
    {
        foreach (var entry in SafeGetFileSystemEntries(directory))
        {
            var attributes = fileSystem.GetAttributes(entry);

            if (attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                continue;
            }

            if (attributes.HasFlag(FileAttributes.Directory))
            {
                foreach (var nested in EnumerateFiles(entry, fileExtension))
                {
                    yield return nested;
                }
            }
            else if (entry.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
            {
                yield return entry;
            }
        }
    }

    private string[] SafeGetFileSystemEntries(string directory)
    {
        try
        {
            return fileSystem.GetFileSystemEntries(directory);
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    public IReadOnlyList<Metadata> GetMetadatas(IMetadataParser parser)
    {
        List<Metadata> metadatas = [];

        foreach (var file in GetFileNames(MiddenFileConventions.MiddenFileExtension))
        {
            Metadata metadata;

            try
            {
                metadata = parser.Parse(fileSystem.ReadAllText(file));
            }
            catch (Exception exception)
            {
                // A single malformed .midden file must not abort the whole data store.
                logger.Warning($"Skipping file '{file}': {exception.Message}");
                continue;
            }

            if (metadata.Dataset is null)
            {
                logger.Warning($"Skipping file '{file}': the file has no 'Dataset' section.");
                continue;
            }

            var relativePath = Path.GetRelativePath(rootDirectory, file);

            metadata.Dataset.DatasetPath = MiddenFileConventions.TrimSuffix(relativePath, MiddenFileConventions.MiddenFileExtension);
            metadatas.Add(metadata);
        }

        return metadatas;
    }

    public IReadOnlyList<Project> GetProjects(ProjectReader reader)
    {
        List<Project> projects = [];

        foreach (var file in GetFileNames(MiddenFileConventions.MippenFileSearchTerm))
        {
            using var stream = fileSystem.OpenRead(file);
            var project = reader.Read(stream);

            if (project is not null)
            {
                projects.Add(project);
            }
        }

        return projects;
    }
}
