using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services.Validation;
using Xunit;

namespace Caf.Midden.Core.Tests;

public class MetadataCompletenessCalculatorTests
{
    [Fact]
    public void Calculate_EmptyDataset_ScoresZero()
    {
        var result = MetadataCompletenessCalculator.Calculate(new Metadata());

        Assert.Equal(0, result.Percent);
    }

    [Fact]
    public void Calculate_FullyDocumentedDataset_ScoresOneHundred()
    {
        var metadata = new Metadata
        {
            Dataset = new Dataset
            {
                Zone = "Raw",
                Name = "CookEastMet",
                Project = "Cook Agronomy Farm",
                Description = "Meteorological measurements.",
                Contacts = [new Person { Name = "Some Researcher" }],
                Tags = ["weather"],
                Variables = [new Variable { Name = "AirTemperature", Units = "degC" }],
                Geometry = """{"type":"Point","coordinates":[0,0]}""",
                TemporalExtent = "2011-01-01/2019-10-30",
                Format = ".csv",
                Methods = ["https://example.org/protocol"]
            }
        };

        var result = MetadataCompletenessCalculator.Calculate(metadata);

        Assert.Equal(100, result.Percent);
        Assert.All(result.Items, item => Assert.True(item.IsComplete));
    }

    [Fact]
    public void Calculate_PartialDataset_ScoresTheWeightOfWhatIsPresent()
    {
        // Zone (10) + Name (10) + Project (10) out of 100.
        var metadata = new Metadata
        {
            Dataset = new Dataset
            {
                Zone = "Raw",
                Name = "CookEastMet",
                Project = "Cook Agronomy Farm"
            }
        };

        var result = MetadataCompletenessCalculator.Calculate(metadata);

        Assert.Equal(30, result.Percent);
    }

    [Fact]
    public void Calculate_WeightsSumToOneHundred()
    {
        var result = MetadataCompletenessCalculator.Calculate(new Metadata());

        Assert.Equal(100, result.Items.Sum(i => i.Weight));
    }

    [Fact]
    public void TopSuggestions_AreOrderedByLeverage()
    {
        var metadata = new Metadata
        {
            Dataset = new Dataset
            {
                Zone = "Raw",
                Name = "CookEastMet",
                Project = "Cook Agronomy Farm"
            }
        };

        var suggestions = MetadataCompletenessCalculator.Calculate(metadata).TopSuggestions.ToList();

        // Description and Variables are the heaviest missing items at 15 points each.
        Assert.Equal(15, suggestions[0].Weight);
        Assert.All(suggestions, s => Assert.False(string.IsNullOrWhiteSpace(s.Suggestion)));
    }

    [Fact]
    public void Calculate_NullModel_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => MetadataCompletenessCalculator.Calculate(null!));
    }
}
