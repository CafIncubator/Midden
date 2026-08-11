using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services.Validation;
using Xunit;

namespace Caf.Midden.Core.Tests;

public class ConfigurationValidatorTests
{
    private readonly ConfigurationValidator validator = new();

    private static Configuration ValidConfiguration() => new()
    {
        OrganizationName = "CAF",
        ToolName = "Midden",
        CatalogPath = "data/catalog.json",
        Zones = ["Raw", "Curated"]
    };

    [Fact]
    public void Validate_CompleteConfiguration_HasNoIssues()
    {
        Assert.Empty(validator.Validate(ValidConfiguration()).Issues);
    }

    [Fact]
    public void Validate_NullModel_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => validator.Validate(null!));
    }

    [Theory]
    [InlineData("data/catalog.JSON")]
    [InlineData("catalog.json")]
    public void Validate_CatalogPathEndingInJson_IsAccepted(string catalogPath)
    {
        var configuration = ValidConfiguration();
        configuration.CatalogPath = catalogPath;

        Assert.DoesNotContain(
            validator.Validate(configuration).Issues,
            i => i.Code == "configuration.catalogPath.notJson");
    }

    [Fact]
    public void Validate_CatalogPathWithoutJsonExtension_IsAWarningNotAnError()
    {
        // The path is a request path, not a file path, so a server route that serves JSON without
        // the extension is unusual but legitimate. Flag it; do not block the download.
        var configuration = ValidConfiguration();
        configuration.CatalogPath = "api/catalog";

        var result = validator.Validate(configuration);

        Assert.Contains(result.Warnings, i => i.Code == "configuration.catalogPath.notJson");
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Validate_CatalogPathWithQueryString_IgnoresTheQueryWhenCheckingTheExtension()
    {
        var configuration = ValidConfiguration();
        configuration.CatalogPath = "data/catalog.json?v=2";

        Assert.DoesNotContain(
            validator.Validate(configuration).Issues,
            i => i.Code == "configuration.catalogPath.notJson");
    }

    [Fact]
    public void Validate_RootedCatalogPath_IsAWarning()
    {
        // Resolved against the app's base address, so a leading slash breaks subfolder hosting.
        var configuration = ValidConfiguration();
        configuration.CatalogPath = "/data/catalog.json";

        var result = validator.Validate(configuration);

        Assert.Contains(result.Warnings, i => i.Code == "configuration.catalogPath.rooted");
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Validate_MissingCatalogPath_DoesNotAlsoReportShapeWarnings()
    {
        var result = validator.Validate(new Configuration());

        Assert.Contains(result.Errors, i => i.Code == "configuration.catalogPath.required");
        Assert.DoesNotContain(result.Issues, i => i.Code == "configuration.catalogPath.notJson");
    }

    [Theory]
    [InlineData("configuration.organizationName.required")]
    [InlineData("configuration.toolName.required")]
    [InlineData("configuration.catalogPath.required")]
    public void Validate_EmptyConfiguration_ReportsEachRequiredFieldAsAnError(string expectedCode)
    {
        var result = validator.Validate(new Configuration());

        Assert.Contains(result.Errors, i => i.Code == expectedCode);
    }

    [Fact]
    public void Validate_NoZones_IsAWarning()
    {
        // Zone is required on every dataset, so an empty list makes the metadata editor unusable -
        // but it does not make the configuration file itself invalid.
        var configuration = ValidConfiguration();
        configuration.Zones = [];

        var result = validator.Validate(configuration);

        Assert.False(result.HasErrors);
        Assert.Contains(result.Warnings, i => i.Code == "configuration.zones.empty");
    }

    [Fact]
    public void Validate_GeometryWithoutName_IsAnError()
    {
        var configuration = ValidConfiguration();
        configuration.Geometries =
        [
            new Geometry { GeoJson = """{"type":"Point","coordinates":[0,0]}""" }
        ];

        var result = validator.Validate(configuration);

        var issue = Assert.Single(result.Issues, i => i.Code == "configuration.geometry.name.required");
        Assert.Equal("configuration.geometries[0].name", issue.Path);
    }

    [Fact]
    public void Validate_GeometryWithoutShape_IsAnError()
    {
        var configuration = ValidConfiguration();
        configuration.Geometries = [new Geometry { Name = "Cook East" }];

        var result = validator.Validate(configuration);

        Assert.Contains(result.Errors, i => i.Code == "configuration.geometry.geojson.required");
    }

    [Fact]
    public void Validate_GeometryWithMalformedShape_IsAnError()
    {
        var configuration = ValidConfiguration();
        configuration.Geometries = [new Geometry { Name = "Cook East", GeoJson = "{not json" }];

        var result = validator.Validate(configuration);

        var issue = Assert.Single(result.Issues, i => i.Code == "configuration.geometry.geojson.invalid");
        Assert.Contains("Cook East", issue.Message);
    }

    [Fact]
    public void Validate_DuplicateVocabularyEntries_AreWarnings()
    {
        var configuration = ValidConfiguration();
        configuration.Zones = ["Raw", "raw", "Curated"];

        var result = validator.Validate(configuration);

        Assert.False(result.HasErrors);
        Assert.Contains(result.Warnings, i => i.Code == "configuration.zones.duplicate");
    }

    [Fact]
    public void Validate_DuplicateGeometryNames_AreWarnings()
    {
        var configuration = ValidConfiguration();
        var geoJson = """{"type":"Point","coordinates":[0,0]}""";
        configuration.Geometries =
        [
            new Geometry { Name = "Cook East", GeoJson = geoJson },
            new Geometry { Name = "Cook East", GeoJson = geoJson }
        ];

        var result = validator.Validate(configuration);

        Assert.False(result.HasErrors);
        Assert.Contains(result.Warnings, i => i.Code == "configuration.geometries.duplicate");
    }
}
