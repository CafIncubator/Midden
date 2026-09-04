using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Models;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;

namespace Caf.Midden.Cli.Tests;

/// <summary>
/// A crawler that returns canned results, or throws, without contacting anything.
/// </summary>
internal sealed class FakeCrawler : ICrawl
{
    private readonly IReadOnlyList<Metadata> metadatas;
    private readonly Exception? throwOnCrawl;

    public FakeCrawler(IReadOnlyList<Metadata>? metadatas = null, Exception? throwOnCrawl = null)
    {
        this.metadatas = metadatas ?? [];
        this.throwOnCrawl = throwOnCrawl;
    }

    public bool WasDisposed { get; private set; }

    public IReadOnlyList<Metadata> GetMetadatas(IMetadataParser parser) =>
        throwOnCrawl is null ? metadatas : throw throwOnCrawl;

    public IReadOnlyList<Project> GetProjects(ProjectReader reader) => [];

    public void Dispose() => WasDisposed = true;

    /// <summary>
    /// Builds a metadata entry with just enough shape for collate to accept it.
    /// </summary>
    public static Metadata MetadataWithPath(string datasetPath) =>
        new() { Dataset = new Dataset { Name = datasetPath, DatasetPath = datasetPath } };
}

/// <summary>
/// Hands out preconfigured <see cref="FakeCrawler"/> instances by data store name, so collate
/// orchestration can be tested without any configured cloud account.
/// </summary>
internal sealed class FakeCrawlerFactory : ICrawlerFactory
{
    private readonly Dictionary<string, ICrawl?> crawlersByStoreName;

    public FakeCrawlerFactory(Dictionary<string, ICrawl?> crawlersByStoreName)
    {
        this.crawlersByStoreName = crawlersByStoreName;
    }

    public ICrawl? Create(
        DataStore dataStore,
        string? clientSecret,
        string? sharedAccessSignature,
        string googleTokenStorePath) =>
        crawlersByStoreName.TryGetValue(dataStore.Name, out var crawler) ? crawler : null;
}
