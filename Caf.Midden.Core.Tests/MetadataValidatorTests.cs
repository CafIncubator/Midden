using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services.Validation;
using Xunit;

namespace Caf.Midden.Core.Tests;

public class MetadataValidatorTests
{
    private readonly MetadataValidator validator = new();

    /// <summary>
    /// A dataset that satisfies every blocking rule, so each test can introduce exactly one fault.
    /// </summary>
    private static Metadata ValidMetadata() => new()
    {
        Dataset = new Dataset
        {
            Zone = "Raw",
            Name = "CookEastMet",
            Project = "Cook Agronomy Farm",
            Description = "Meteorological measurements.",
            Contacts = [new Person { Name = "Some Researcher", Email = "researcher@example.org" }],
            Tags = ["weather"],
            Variables =
            [
                new Variable { Name = "AirTemperature", Description = "Air temperature", Units = "degC" }
            ]
        }
    };

    private static Configuration Configuration() => new()
    {
        Zones = ["Raw", "Curated"],
        Roles = ["Owner", "Technician"],
        Tags = ["weather", "soil"],
        ProcessingLevels = ["L0", "L1"],
        VariableTypes = ["Measured"],
        QCTags = ["Range"],
        DatasetStructures = ["Single", "Multiple"]
    };

    [Fact]
    public void Validate_CompleteDataset_HasNoIssuesAtAll()
    {
        var result = validator.Validate(ValidMetadata(), Configuration());

        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_NullModel_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => validator.Validate(null!));
    }

    [Theory]
    [InlineData("dataset.zone.required")]
    [InlineData("dataset.name.required")]
    [InlineData("dataset.project.required")]
    public void Validate_EmptyDataset_ReportsEachIdentityFieldAsAnError(string expectedCode)
    {
        var result = validator.Validate(new Metadata());

        var issue = Assert.Single(result.Issues, i => i.Code == expectedCode);
        Assert.Equal(ValidationSeverity.Error, issue.Severity);
    }

    [Fact]
    public void Validate_PathUnsafeName_IsAnError()
    {
        // Regression guard: an unsafe name is silently dropped by Collate's path filtering, so it
        // must never reach a downloaded file.
        var metadata = ValidMetadata();
        metadata.Dataset.Name = "Cook/East";

        var result = validator.Validate(metadata, Configuration());

        var issue = Assert.Single(result.Issues, i => i.Code == "dataset.name.unsafe");
        Assert.Equal(ValidationSeverity.Error, issue.Severity);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void Validate_MissingDescriptionAndContactsAndTags_AreWarningsNotErrors()
    {
        // These are quality problems, not correctness problems: a researcher must be able to
        // knowingly download without them.
        var metadata = ValidMetadata();
        metadata.Dataset.Description = null;
        metadata.Dataset.Contacts = [];
        metadata.Dataset.Tags = [];

        var result = validator.Validate(metadata, Configuration());

        Assert.False(result.HasErrors);
        Assert.Contains(result.Warnings, i => i.Code == "dataset.description.missing");
        Assert.Contains(result.Warnings, i => i.Code == "dataset.contacts.missing");
        Assert.Contains(result.Warnings, i => i.Code == "dataset.tags.missing");
    }

    [Fact]
    public void Validate_ContactWithoutName_IsAnError()
    {
        var metadata = ValidMetadata();
        metadata.Dataset.Contacts = [new Person { Email = "researcher@example.org" }];

        var result = validator.Validate(metadata, Configuration());

        var issue = Assert.Single(result.Issues, i => i.Code == "contact.name.required");
        Assert.Equal(ValidationSeverity.Error, issue.Severity);
        Assert.Equal("dataset.contacts[0].name", issue.Path);
    }

    [Theory]
    [InlineData(null, "contact.email.missing")]
    [InlineData("not-an-email", "contact.email.malformed")]
    public void Validate_ContactEmailProblems_AreWarnings(string? email, string expectedCode)
    {
        var metadata = ValidMetadata();
        metadata.Dataset.Contacts = [new Person { Name = "Some Researcher", Email = email }];

        var result = validator.Validate(metadata, Configuration());

        var issue = Assert.Single(result.Issues, i => i.Code == expectedCode);
        Assert.Equal(ValidationSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void Validate_VariableMissingNameAndUnits_AreErrorsButDescriptionIsAWarning()
    {
        var metadata = ValidMetadata();
        metadata.Dataset.Variables = [new Variable()];

        var result = validator.Validate(metadata, Configuration());

        Assert.Equal(
            ValidationSeverity.Error,
            Assert.Single(result.Issues, i => i.Code == "variable.name.required").Severity);
        Assert.Equal(
            ValidationSeverity.Error,
            Assert.Single(result.Issues, i => i.Code == "variable.units.required").Severity);
        Assert.Equal(
            ValidationSeverity.Warning,
            Assert.Single(result.Issues, i => i.Code == "variable.description.missing").Severity);
    }

    [Fact]
    public void Validate_VariableIssues_CarryIndexedPathsForRowHighlighting()
    {
        var metadata = ValidMetadata();
        metadata.Dataset.Variables =
        [
            new Variable { Name = "Good", Description = "d", Units = "u" },
            new Variable { Name = "NoUnits", Description = "d" }
        ];

        var result = validator.Validate(metadata, Configuration());

        var issue = Assert.Single(result.Issues, i => i.Code == "variable.units.required");
        Assert.Equal("dataset.variables[1].units", issue.Path);
    }

    [Fact]
    public void Validate_NoVariables_IsAWarning()
    {
        var metadata = ValidMetadata();
        metadata.Dataset.Variables = [];

        var result = validator.Validate(metadata, Configuration());

        Assert.False(result.HasErrors);
        Assert.Contains(result.Warnings, i => i.Code == "dataset.variables.missing");
    }

    [Fact]
    public void Validate_MalformedGeometry_IsAnError()
    {
        var metadata = ValidMetadata();
        metadata.Dataset.Geometry = "{not json";

        var result = validator.Validate(metadata, Configuration());

        Assert.Contains(result.Errors, i => i.Code == "dataset.geometry.invalid");
    }

    [Fact]
    public void Validate_WholeFeaturePastedAsGeometry_GetsItsOwnCodeAndAHint()
    {
        var metadata = ValidMetadata();
        metadata.Dataset.Geometry =
            """{"type":"Feature","properties":{},"geometry":{"type":"Point","coordinates":[0,0]}}""";

        var result = validator.Validate(metadata, Configuration());

        var issue = Assert.Single(result.Issues, i => i.Code == "dataset.geometry.notAGeometry");
        Assert.Equal(ValidationSeverity.Error, issue.Severity);
        Assert.False(string.IsNullOrWhiteSpace(issue.Hint));
    }

    [Fact]
    public void Validate_UnparseableTemporalExtent_IsAnError()
    {
        var metadata = ValidMetadata();
        metadata.Dataset.TemporalExtent = "sometime last summer";

        var result = validator.Validate(metadata, Configuration());

        Assert.Contains(result.Errors, i => i.Code == "dataset.temporalExtent.invalid");
    }

    [Fact]
    public void Validate_BackwardsTemporalExtent_IsOnlyAWarning()
    {
        // It parses and will not break the catalog, so it should not block a download.
        var metadata = ValidMetadata();
        metadata.Dataset.TemporalExtent = "2019-10-30/2011-01-01";

        var result = validator.Validate(metadata, Configuration());

        Assert.False(result.HasErrors);
        Assert.Contains(result.Warnings, i => i.Code == "dataset.temporalExtent.reversed");
    }

    [Fact]
    public void Validate_NegativeSpatialRepeats_IsAnError()
    {
        var metadata = ValidMetadata();
        metadata.Dataset.SpatialRepeats = -1;

        var result = validator.Validate(metadata, Configuration());

        Assert.Contains(result.Errors, i => i.Code == "dataset.spatialRepeats.negative");
    }

    [Fact]
    public void Validate_ValueOutsideConfiguredVocabulary_IsOnlyAWarning()
    {
        // A .midden authored at another organization legitimately carries unknown vocabulary;
        // hard-failing it would make the editor unusable for shared data.
        var metadata = ValidMetadata();
        metadata.Dataset.Zone = "SomeOtherOrgsZone";

        var result = validator.Validate(metadata, Configuration());

        Assert.False(result.HasErrors);
        Assert.Contains(result.Warnings, i => i.Code == "dataset.zone.unknown");
    }

    [Fact]
    public void Validate_WithoutConfiguration_SkipsVocabularyChecksEntirely()
    {
        var metadata = ValidMetadata();
        metadata.Dataset.Zone = "AnythingAtAll";
        metadata.Dataset.Structure = "AnythingAtAll";

        var result = validator.Validate(metadata);

        Assert.DoesNotContain(result.Issues, i => i.Code.EndsWith(".unknown", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WithEmptyVocabulary_SkipsThatCheck()
    {
        // An unconfigured app must not flood the user with "not in the list" warnings.
        var metadata = ValidMetadata();
        metadata.Dataset.Zone = "AnythingAtAll";

        var result = validator.Validate(metadata, new Configuration());

        Assert.DoesNotContain(result.Issues, i => i.Code == "dataset.zone.unknown");
    }

    [Fact]
    public void Validate_IssuesAreTaggedWithTheOwningEditorTab()
    {
        var metadata = new Metadata();
        metadata.Dataset.Geometry = "{not json";

        var result = validator.Validate(metadata);

        Assert.Contains(
            result.Issues,
            i => i.Code == "dataset.zone.required" && i.Section == MetadataSections.Basic);
        Assert.Contains(
            result.Issues,
            i => i.Code == "dataset.geometry.invalid" && i.Section == MetadataSections.Coverage);
        Assert.Contains(
            result.Issues,
            i => i.Code == "dataset.variables.missing" && i.Section == MetadataSections.Variables);
    }

    [Fact]
    public void CountsBySection_GroupsErrorsForTabBadges()
    {
        var metadata = new Metadata();
        metadata.Dataset.Variables = [new Variable(), new Variable()];

        var counts = validator.Validate(metadata).CountsBySection();

        // Zone, Name, Project.
        Assert.Equal(3, counts[MetadataSections.Basic]);
        // Name and Units for each of the two blank variables.
        Assert.Equal(4, counts[MetadataSections.Variables]);
    }
}
