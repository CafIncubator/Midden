using Caf.Midden.Cli.Services;
using Caf.Midden.Core.Services.Metadata;

namespace Caf.Midden.Cli.Tests;

public class LocalFileSystemCrawlerTests
{
    [Fact]
    public void GetFileNames_ValidInput_ReturnsExpected()
    {
        var sut = new LocalFileSystemCrawler(@"Assets\MockDataStoreLocal");

        var actual = sut.GetFileNames(".midden");

        Assert.Equal(5, actual.Count);
    }

    [Fact]
    public void GetMetadatas_ValidInput_ReturnsWithVariableType()
    {
        var sut = new LocalFileSystemCrawler(@"Assets\MockDataStoreLocalVarTypes");

        var actual = sut.GetMetadatas(new MetadataParser(new MetadataConverter()));

        Assert.NotNull(actual[0].Dataset.Variables[0].VariableType);
    }
}
