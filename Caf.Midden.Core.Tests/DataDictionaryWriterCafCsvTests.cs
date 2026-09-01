using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using System.Text;
using Xunit;

namespace Caf.Midden.Core.Tests;

public class DataDictionaryWriterCafCsvTests
{
    [Fact]
    public void Write_MissingVariableValues_InheritDatasetValues()
    {
        var dataset = new Dataset
        {
            Tags = ["weather", " station "],
            Methods = ["dataset method"],
            TemporalResolution = "1 hour",
            TemporalExtent = "2011/2019",
            SpatialRepeats = 3,
            Variables =
            [
                new Variable
                {
                    Name = "AirTemperature",
                    Description = "First line, with comma\nSecond \"quoted\" line",
                    Units = "degC",
                    Tags = [" "],
                    Methods = [],
                    TemporalResolution = " ",
                    TemporalExtent = " / ",
                    SpatialRepeats = null
                }
            ]
        };

        Variable variable = Read(DataDictionaryWriterCafCsv.Write(dataset)).Single();

        Assert.Equal(["weather", "station"], variable.Tags);
        Assert.Equal(["dataset method"], variable.Methods);
        Assert.Equal("1 hour", variable.TemporalResolution);
        Assert.Equal("2011/2019", variable.TemporalExtent);
        Assert.Equal(3, variable.SpatialRepeats);
        Assert.Equal("First line, with comma\nSecond \"quoted\" line", variable.Description);
    }

    [Fact]
    public void Write_VariableValuesOverrideDatasetValues_WithoutMutatingMetadata()
    {
        var variable = new Variable
        {
            Name = "Rainfall",
            Description = "Rainfall total",
            Units = "mm",
            Tags = [" variable-tag "],
            Methods = ["variable method"],
            TemporalResolution = "5 minutes",
            TemporalExtent = "2020/2021",
            SpatialRepeats = 0
        };
        var dataset = new Dataset
        {
            Tags = ["dataset-tag"],
            Methods = ["dataset method"],
            TemporalResolution = "1 day",
            TemporalExtent = "2010/2020",
            SpatialRepeats = 5,
            Variables = [variable]
        };

        Variable exported = Read(DataDictionaryWriterCafCsv.Write(dataset)).Single();

        Assert.Equal(["variable-tag"], exported.Tags);
        Assert.Equal(["variable method"], exported.Methods);
        Assert.Equal("5 minutes", exported.TemporalResolution);
        Assert.Equal("2020/2021", exported.TemporalExtent);
        Assert.Equal(0, exported.SpatialRepeats);
        Assert.Equal([" variable-tag "], variable.Tags);
    }

    [Fact]
    public void Write_OmitsIsQCSpecified_ButReaderAcceptsLegacyColumn()
    {
        var dataset = new Dataset
        {
            Variables =
            [
                new Variable
                {
                    Name = "QualityFlag",
                    Description = "Quality flag",
                    Units = "1",
                    IsQCSpecified = true,
                    QCApplied = ["Point"]
                }
            ]
        };

        string csv = DataDictionaryWriterCafCsv.Write(dataset);
        string header = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0];

        Assert.DoesNotContain(nameof(Variable.IsQCSpecified), header.Split(','));
        Assert.Equal(["Point"], Read(csv).Single().QCApplied);

        const string legacyCsv = "Name,Description,Units,IsQCSpecified\r\nFlag,Quality flag,1,true\r\n";
        Assert.True(Read(legacyCsv).Single().IsQCSpecified);
    }

    [Fact]
    public void Write_EmptyDataset_ReturnsHeaderOnlyCsv()
    {
        string csv = DataDictionaryWriterCafCsv.Write(new Dataset());

        Assert.False(string.IsNullOrWhiteSpace(csv));
        Assert.Contains(nameof(Variable.Name), csv);
        Assert.DoesNotContain(nameof(Variable.IsQCSpecified), csv);
        Assert.Empty(Read(csv));
    }

    private static List<Variable> Read(string csv)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        return new DataDictionaryReaderCafCsv().Read(stream);
    }
}
