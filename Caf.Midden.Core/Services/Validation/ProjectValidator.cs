using Caf.Midden.Core.Models.v0_2;

// Aliased with a distinct name: the sibling namespace Caf.Midden.Core.Services.Configuration wins
// simple-name resolution against the model type, and an alias of the same name would not.
using AppConfiguration = Caf.Midden.Core.Models.v0_2.Configuration;

namespace Caf.Midden.Core.Services.Validation;

/// <summary>
/// Validates a project description. Counterpart to <see cref="MetadataValidator"/> for
/// <c>ProjectEditor</c>, whose form currently has no validation rules at all.
/// </summary>
public sealed class ProjectValidator : IValidator<Project>
{
    /// <summary>
    /// The section key used for every project issue. The project editor is a single flat form, so
    /// there is only one.
    /// </summary>
    public const string Section = "project";

    private const int MinimumNameLength = 2;

    public ValidationResult Validate(Project model, AppConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        var issues = new IssueCollector();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            issues.Error(
                Section,
                "project.name",
                "project.name.required",
                "A project name is required.",
                "Datasets reference this name to group themselves under the project.");
        }
        else if (model.Name.Trim().Length < MinimumNameLength)
        {
            issues.Error(
                Section,
                "project.name",
                "project.name.tooShort",
                $"A project name must be at least {MinimumNameLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(model.ProjectStatus))
        {
            issues.Error(
                Section,
                "project.status",
                "project.status.required",
                "A project status is required.");
        }
        else
        {
            issues.WarnIfNotInVocabulary(
                Section,
                "project.status",
                "project.status.unknown",
                model.ProjectStatus,
                configuration?.ProjectStatuses,
                "project statuses");
        }

        if (string.IsNullOrWhiteSpace(model.Description))
        {
            issues.Warn(
                Section,
                "project.description",
                "project.description.missing",
                "This project has no description.",
                "The description is the main thing a data user reads on the project page.");
        }

        return issues.ToResult();
    }
}
