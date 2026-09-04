using Caf.Midden.Core.Services.Validation;
using Xunit;

namespace Caf.Midden.Core.Tests;

public class GeoJsonGeometryValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_AbsentValue_IsValid(string? geoJson)
    {
        // Absent is a separate concern from malformed; geometry is optional throughout Midden.
        Assert.Equal(GeoJsonGeometryStatus.Valid, GeoJsonGeometryValidator.Validate(geoJson));
    }

    [Theory]
    [InlineData("""{"type":"Point","coordinates":[-117.0,46.7]}""")]
    [InlineData("""{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,0]]]}""")]
    [InlineData("""{"type":"MultiLineString","coordinates":[[[0,0],[1,1]]]}""")]
    public void Validate_WellFormedGeometry_IsValid(string geoJson)
    {
        Assert.Equal(GeoJsonGeometryStatus.Valid, GeoJsonGeometryValidator.Validate(geoJson));
    }

    [Fact]
    public void Validate_GeometryCollectionWithGeometries_IsValid()
    {
        var geoJson = """{"type":"GeometryCollection","geometries":[{"type":"Point","coordinates":[0,0]}]}""";

        Assert.Equal(GeoJsonGeometryStatus.Valid, GeoJsonGeometryValidator.Validate(geoJson));
    }

    [Fact]
    public void Validate_Feature_IsReportedDistinctlyFromUnknownType()
    {
        // Dataset.Geometry stores only the geometry member, so pasting a whole Feature is a very
        // likely user error that deserves an "unwrap this" hint rather than "invalid type".
        var geoJson = """{"type":"Feature","properties":{},"geometry":{"type":"Point","coordinates":[0,0]}}""";

        var status = GeoJsonGeometryValidator.Validate(geoJson);

        Assert.Equal(GeoJsonGeometryStatus.IsFeature, status);
        Assert.NotNull(GeoJsonGeometryValidator.DescribeFix(status));
    }

    [Fact]
    public void Validate_FeatureCollection_IsReportedDistinctly()
    {
        var geoJson = """{"type":"FeatureCollection","features":[]}""";

        Assert.Equal(
            GeoJsonGeometryStatus.IsFeatureCollection,
            GeoJsonGeometryValidator.Validate(geoJson));
    }

    [Theory]
    [InlineData("not json at all", GeoJsonGeometryStatus.NotJson)]
    [InlineData("[1,2,3]", GeoJsonGeometryStatus.NotAnObject)]
    [InlineData("""{"coordinates":[0,0]}""", GeoJsonGeometryStatus.MissingType)]
    [InlineData("""{"type":"Rhombus","coordinates":[0,0]}""", GeoJsonGeometryStatus.UnknownType)]
    [InlineData("""{"type":"Point"}""", GeoJsonGeometryStatus.MissingCoordinates)]
    [InlineData("""{"type":"GeometryCollection"}""", GeoJsonGeometryStatus.MissingGeometries)]
    public void Validate_MalformedGeometry_ReportsSpecificStatus(
        string geoJson,
        GeoJsonGeometryStatus expected)
    {
        Assert.Equal(expected, GeoJsonGeometryValidator.Validate(geoJson));
    }

    [Fact]
    public void DescribeProblem_ValidStatus_HasNoMessage()
    {
        Assert.Null(GeoJsonGeometryValidator.DescribeProblem(GeoJsonGeometryStatus.Valid));
    }

    [Theory]
    [InlineData(GeoJsonGeometryStatus.NotJson)]
    [InlineData(GeoJsonGeometryStatus.NotAnObject)]
    [InlineData(GeoJsonGeometryStatus.MissingType)]
    [InlineData(GeoJsonGeometryStatus.UnknownType)]
    [InlineData(GeoJsonGeometryStatus.IsFeature)]
    [InlineData(GeoJsonGeometryStatus.IsFeatureCollection)]
    [InlineData(GeoJsonGeometryStatus.MissingCoordinates)]
    [InlineData(GeoJsonGeometryStatus.MissingGeometries)]
    public void DescribeProblem_EveryFailure_HasAMessage(GeoJsonGeometryStatus status)
    {
        Assert.False(string.IsNullOrWhiteSpace(GeoJsonGeometryValidator.DescribeProblem(status)));
    }
}
