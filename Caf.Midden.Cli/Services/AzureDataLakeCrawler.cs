using Azure.Core;
using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using Caf.Midden.Cli.Common;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
using System.Text;

namespace Caf.Midden.Cli.Services;

public sealed class AzureDataLakeCrawler : ICrawl
{
    private readonly DataLakeFileSystemClient fileSystemClient;
    private readonly ICrawlLogger logger;
    private IReadOnlyList<Azure.Storage.Files.DataLake.Models.PathItem>? cachedPaths;

    public AzureDataLakeCrawler(
        string accountName,
        string tenantId,
        string clientId,
        string clientSecret,
        string fileSystemName,
        string? endpointSuffix = null,
        ICrawlLogger? logger = null)
        : this(
            accountName,
            fileSystemName,
            CreateClientSecretCredential(tenantId, clientId, clientSecret),
            endpointSuffix,
            logger)
    {
    }

    /// <summary>
    /// Creates a crawler using an already configured credential, allowing the caller to supply
    /// managed identity, an interactive browser sign in, or a client secret.
    /// </summary>
    /// <param name="endpointSuffix">
    /// Overrides the default "dfs.core.windows.net" endpoint suffix, for Azure Government, Azure
    /// China, or other sovereign clouds.
    /// </param>
    public AzureDataLakeCrawler(
        string accountName,
        string fileSystemName,
        TokenCredential credential,
        string? endpointSuffix = null,
        ICrawlLogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileSystemName);
        ArgumentNullException.ThrowIfNull(credential);

        var suffix = string.IsNullOrWhiteSpace(endpointSuffix) ? "dfs.core.windows.net" : endpointSuffix;
        var serviceClient = new DataLakeServiceClient(new Uri($"https://{accountName}.{suffix}"), credential);
        fileSystemClient = serviceClient.GetFileSystemClient(fileSystemName);
        this.logger = logger ?? ConsoleCrawlLogger.Instance;
    }

    private static TokenCredential CreateClientSecretCredential(string tenantId, string clientId, string clientSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);

        return new ClientSecretCredential(tenantId, clientId, clientSecret);
    }

    public void Dispose()
    {
        // DataLakeFileSystemClient/DataLakeServiceClient hold no unmanaged resources requiring
        // explicit disposal; this satisfies ICrawl's IDisposable contract for symmetry with
        // crawlers that do own disposable resources (e.g. DriveService).
    }

    internal IReadOnlyList<string> GetFileNames(string fileExtension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);

        var names = GetCachedPaths()
            .Where(pathItem => pathItem.IsDirectory != true && pathItem.Name.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
            .Select(pathItem => pathItem.Name)
            .ToList();

        logger.Info($"Found a total of {names.Count} files");
        return names;
    }

    /// <summary>
    /// Lists the file system once and caches the result so that <c>GetMetadatas</c> and
    /// <c>GetProjects</c> both filtering from the same listing does not issue a second remote
    /// listing call when a data store has <c>ShouldCollateProjects</c> enabled.
    /// </summary>
    private IReadOnlyList<Azure.Storage.Files.DataLake.Models.PathItem> GetCachedPaths() =>
        cachedPaths ??= fileSystemClient
            .GetPaths(path: null, recursive: true, userPrincipalName: false, cancellationToken: CancellationToken.None)
            .ToList();

    public IReadOnlyList<Metadata> GetMetadatas(IMetadataParser parser)
    {
        List<Metadata> metadatas = [];

        foreach (var fileName in GetFileNames(MiddenFileConventions.MiddenFileExtension))
        {
            try
            {
                var fileClient = fileSystemClient.GetFileClient(fileName);
                using var stream = fileClient.OpenRead();

                // A StreamReader with BOM detection avoids buffering the whole file into a
                // second byte array via ToArray() and correctly strips a UTF-8 BOM instead of
                // leaving it as a stray character in the parsed JSON.
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var json = reader.ReadToEnd();
                var metadata = parser.Parse(json);

                if (metadata.Dataset is null)
                {
                    logger.Warning($"Skipping file '{fileName}': the file has no 'Dataset' section.");
                    continue;
                }

                metadata.Dataset.DatasetPath = MiddenFileConventions.TrimSuffix(fileClient.Path, MiddenFileConventions.MiddenFileExtension);
                metadatas.Add(metadata);
            }
            catch (Exception exception)
            {
                logger.Warning($"Error parsing file '{fileName}': {exception.Message}");
            }
        }

        return metadatas;
    }

    public IReadOnlyList<Project> GetProjects(ProjectReader reader)
    {
        List<Project> projects = [];

        foreach (var fileName in GetFileNames(MiddenFileConventions.MippenFileSearchTerm))
        {
            var fileClient = fileSystemClient.GetFileClient(fileName);
            using var stream = fileClient.OpenRead();
            var project = reader.Read(stream);

            if (project is not null)
            {
                projects.Add(project);
            }
        }

        return projects;
    }
}
