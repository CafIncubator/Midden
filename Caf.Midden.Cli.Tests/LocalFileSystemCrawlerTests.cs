using Caf.Midden.Cli.Services;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;

namespace Caf.Midden.Cli.Tests;

public class LocalFileSystemCrawlerTests
{
    [Fact]
    public void GetProjects_ValidInput_ReturnsProductionProject()
    {
        var sut = new LocalFileSystemCrawler(Path.Combine("Assets", "MockDataStoreLocal"));

        var actual = sut.GetProjects(new ProjectReader(new ProjectParser()));

        Assert.Contains(actual, project => project.Name == "ProductionProject");
    }

    [Fact]
    public void GetFileNames_ValidInput_ReturnsExpected()
    {
        var sut = new LocalFileSystemCrawler(Path.Combine("Assets", "MockDataStoreLocal"));

        var actual = sut.GetFileNames(".midden");

        Assert.Equal(5, actual.Count);
    }

    [Fact]
    public void GetMetadatas_ValidInput_ReturnsWithVariableType()
    {
        var sut = new LocalFileSystemCrawler(Path.Combine("Assets", "MockDataStoreLocalVarTypes"));

        var actual = sut.GetMetadatas(new MetadataParser(new MetadataConverter()));

        Assert.NotNull(actual[0].Dataset.Variables[0].VariableType);
    }

    [Fact]
    public void GetMetadatas_MalformedFilesPresent_SkipsThemAndReturnsTheValidOnes()
    {
        var sut = new LocalFileSystemCrawler(Path.Combine("Assets", "MockDataStoreLocalMalformed"));

        var actual = sut.GetMetadatas(new MetadataParser(new MetadataConverter()));

        var dataset = Assert.Single(actual);
        Assert.Equal("ValidFile", dataset.Dataset.Name);
    }

    [Fact]
    public void GetMetadatas_MalformedFilesPresent_ReportsEachSkipAsAWarningOnTheInjectedLogger()
    {
        var logger = new RecordingCrawlLogger();
        var sut = new LocalFileSystemCrawler(Path.Combine("Assets", "MockDataStoreLocalMalformed"), logger);

        sut.GetMetadatas(new MetadataParser(new MetadataConverter()));

        // The malformed JSON file and the file with no 'Dataset' section are each reported,
        // and the reports are capturable rather than written straight to the console.
        Assert.Equal(2, logger.Warnings.Count);
        Assert.Contains(logger.Warnings, warning => warning.Contains("InvalidJson.midden", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Warnings, warning => warning.Contains("NullDataset.midden", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetFileNames_ExtensionAppearsInsideNamesAndDirectoryNames_OnlyMatchesFileSuffix()
    {
        var sut = new LocalFileSystemCrawler(Path.Combine("Assets", "MockDataStoreLocalPathologicalPath"));

        var actual = sut.GetFileNames(".midden");

        // "notes.midden.bak" does not end in ".midden" and must not match. "SomeFolder.midden" is
        // a directory, not a file, and must not be returned even though its name ends in ".midden".
        var file = Assert.Single(actual);
        Assert.EndsWith("x.midden", file, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetMetadatas_SuffixAppearsMidPath_OnlyTrailingSuffixIsTrimmedFromDatasetPath()
    {
        var sut = new LocalFileSystemCrawler(Path.Combine("Assets", "MockDataStoreLocalPathologicalPath"));

        var actual = sut.GetMetadatas(new MetadataParser(new MetadataConverter()));

        var dataset = Assert.Single(actual);
        Assert.Equal("archive.midden.data/x", dataset.Dataset.DatasetPath);
    }

    [Fact]
    public void GetFileNames_ReparsePointDirectoryPresent_DoesNotFollowIt()
    {
        // The on-disk equivalent of this test has to be skipped on machines without elevation or
        // Developer Mode. Faking the file system makes the reparse-point case run everywhere.
        var root = Path.Combine("fake", "root");
        var fileSystem = new FakeFileSystem()
            .AddDirectory(root)
            .AddFile(Path.Combine(root, "real", "real.midden"))
            .AddReparsePointDirectory(Path.Combine(root, "link"))
            .AddFile(Path.Combine(root, "link", "escaped.midden"));

        var sut = new LocalFileSystemCrawler(root, fileSystem: fileSystem);

        var actual = sut.GetFileNames(".midden");

        var file = Assert.Single(actual);
        Assert.EndsWith("real.midden", file, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetMetadatas_FakedTree_ReadsThroughTheInjectedFileSystem()
    {
        var root = Path.Combine("fake", "root");
        var fileSystem = new FakeFileSystem()
            .AddDirectory(root)
            .AddFile(Path.Combine(root, "Raw", "Dataset.midden"), """
                {
                    "file": { "schema-version": "v0.1.0-alpha2", "creation-date": "2020-07-29" },
                    "dataset": { "zone": "Raw", "project": "FakedProject", "name": "FakedDataset" }
                }
                """);

        var sut = new LocalFileSystemCrawler(root, fileSystem: fileSystem);

        var actual = sut.GetMetadatas(new MetadataParser(new MetadataConverter()));

        var metadata = Assert.Single(actual);
        Assert.Equal("FakedDataset", metadata.Dataset.Name);
        Assert.Equal("Raw/Dataset", metadata.Dataset.DatasetPath);
    }

    [Fact]
    public void GetFileNames_SymlinkedDirectoryPresent_DoesNotFollowIt()
    {
        var root = Path.Combine(Path.GetTempPath(), $"midden-symlink-test-{Guid.NewGuid()}");
        var realDirectory = Path.Combine(root, "real");
        var linkPath = Path.Combine(root, "link");

        Directory.CreateDirectory(realDirectory);
        File.WriteAllText(Path.Combine(realDirectory, "real.midden"), "{}");

        try
        {
            // A symbolic link back to an ancestor directory: following it would recurse forever,
            // and it can also be used to escape the configured root entirely.
            Directory.CreateSymbolicLink(linkPath, root);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Creating a symbolic link requires elevation or Developer Mode on Windows. Skip on
            // machines where that is not available rather than failing the build.
            Directory.Delete(root, recursive: true);
            return;
        }

        try
        {
            var sut = new LocalFileSystemCrawler(root);

            var actual = sut.GetFileNames(".midden");

            var file = Assert.Single(actual);
            Assert.EndsWith("real.midden", file, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
