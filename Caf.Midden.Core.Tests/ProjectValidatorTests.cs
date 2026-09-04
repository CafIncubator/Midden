using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services.Validation;
using Xunit;

namespace Caf.Midden.Core.Tests;

public class ProjectValidatorTests
{
    private readonly ProjectValidator validator = new();

    private static Project ValidProject() => new()
    {
        Name = "Cook Agronomy Farm",
        ProjectStatus = "Active",
        Description = "Long-term agroecological research."
    };

    private static Configuration Configuration() => new()
    {
        ProjectStatuses = ["Active", "Complete"]
    };

    [Fact]
    public void Validate_CompleteProject_HasNoIssues()
    {
        Assert.Empty(validator.Validate(ValidProject(), Configuration()).Issues);
    }

    [Fact]
    public void Validate_NullModel_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => validator.Validate(null!));
    }

    [Fact]
    public void Validate_EmptyProject_ReportsNameAndStatusAsErrors()
    {
        var result = validator.Validate(new Project());

        Assert.Contains(result.Errors, i => i.Code == "project.name.required");
        Assert.Contains(result.Errors, i => i.Code == "project.status.required");
    }

    [Fact]
    public void Validate_SingleCharacterName_IsAnError()
    {
        var project = ValidProject();
        project.Name = "A";

        var result = validator.Validate(project, Configuration());

        Assert.Contains(result.Errors, i => i.Code == "project.name.tooShort");
    }

    [Fact]
    public void Validate_MissingDescription_IsOnlyAWarning()
    {
        var project = ValidProject();
        project.Description = null!;

        var result = validator.Validate(project, Configuration());

        Assert.False(result.HasErrors);
        Assert.Contains(result.Warnings, i => i.Code == "project.description.missing");
    }

    [Fact]
    public void Validate_StatusOutsideConfiguredVocabulary_IsOnlyAWarning()
    {
        var project = ValidProject();
        project.ProjectStatus = "Hibernating";

        var result = validator.Validate(project, Configuration());

        Assert.False(result.HasErrors);
        Assert.Contains(result.Warnings, i => i.Code == "project.status.unknown");
    }
}
