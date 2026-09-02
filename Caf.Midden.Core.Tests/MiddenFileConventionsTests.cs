using Caf.Midden.Core.Services;
using Xunit;

namespace Caf.Midden.Core.Tests;

public class MiddenFileConventionsTests
{
    [Theory]
    [InlineData("Raw/CookEastMet.midden", "Raw/CookEastMet")]
    [InlineData("Raw/CookEastMet.MIDDEN", "Raw/CookEastMet")]
    [InlineData("Raw/CookEastMet", "Raw/CookEastMet")]
    public void TrimSuffix_RemovesOnlyTrailingSuffix(string input, string expected)
    {
        var actual = MiddenFileConventions.TrimSuffix(input, MiddenFileConventions.MiddenFileExtension);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrimSuffix_SuffixAppearsMidPath_OnlyTrailingOccurrenceIsRemoved()
    {
        // Regression: a naive Replace(".midden", "") would also mangle the directory segment.
        var actual = MiddenFileConventions.TrimSuffix("archive.midden.data/x.midden", MiddenFileConventions.MiddenFileExtension);

        Assert.Equal("archive.midden.data/x", actual);
    }

    [Theory]
    [InlineData(@"Raw\Dataset", "Raw/Dataset")]
    [InlineData("Raw/Dataset", "Raw/Dataset")]
    public void NormalizeDatasetPath_UsesForwardSlashes(string input, string expected)
    {
        var actual = MiddenFileConventions.NormalizeDatasetPath(input);

        Assert.Equal(expected, actual);
    }
}
