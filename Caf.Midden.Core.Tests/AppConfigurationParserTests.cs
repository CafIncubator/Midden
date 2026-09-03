using Caf.Midden.Core.Services.Configuration;
using Caf.Midden.Core.Services.Validation;
using System.Text.Json;
using Xunit;

namespace Caf.Midden.Core.Tests;

public class AppConfigurationParserTests
{
    [Theory]
    [InlineData("v0.2")]
    [InlineData("v0.1")]
    public void Parse_SupportedVersion_ReturnsCurrentRuntimeModel(string version)
    {
        var configuration = AppConfigurationParser.Parse(ValidJson(version));

        Assert.Equal(version, configuration.SchemaVersion);
        Assert.Equal("Example Research Organization", configuration.OrganizationName);
    }

    [Fact]
    public void Parse_UnknownVersion_ThrowsClearError()
    {
        var exception = Assert.Throws<JsonException>(
            () => AppConfigurationParser.Parse(ValidJson("vBanana")));

        Assert.Contains("is not supported", exception.Message);
        Assert.Contains("v0.2", exception.Message);
        Assert.Contains("v0.1", exception.Message);
    }

    [Fact]
    public void Parse_MissingVersion_ThrowsClearError()
    {
        var exception = Assert.Throws<JsonException>(
            () => AppConfigurationParser.Parse("""{"organizationName":"Example"}"""));

        Assert.Contains("requires a 'schemaVersion'", exception.Message);
    }

    [Fact]
    public void DocumentedExample_ParsesAndValidatesWithoutIssues()
    {
        var json = File.ReadAllText("Assets/ConfigFiles/app-config.example.json");
        var configuration = AppConfigurationParser.Parse(json);

        Assert.Equal(AppConfigurationVersions.Current, configuration.SchemaVersion);
        Assert.Empty(new ConfigurationValidator().Validate(configuration).Issues);
    }

    private static string ValidJson(string version) => $$"""
        {
          "schemaVersion": "{{version}}",
          "organizationName": "Example Research Organization",
          "toolName": "Research Data Catalog",
          "catalogPath": "catalog.json",
          "zones": ["Raw"]
        }
        """;
}