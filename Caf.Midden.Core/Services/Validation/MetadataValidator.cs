using System.Text.RegularExpressions;
using Caf.Midden.Core.Models.v0_2;

// Aliased with distinct names: the sibling namespaces Caf.Midden.Core.Services.Configuration and
// Caf.Midden.Core.Services.Metadata win simple-name resolution against the model types, and an
// alias of the same name would not.
using AppConfiguration = Caf.Midden.Core.Models.v0_2.Configuration;
using DatasetMetadata = Caf.Midden.Core.Models.v0_2.Metadata;

namespace Caf.Midden.Core.Services.Validation;

/// <summary>
/// The single source of truth for whether a dataset's metadata is publishable.
/// </summary>
/// <remarks>
/// <para>
/// Lives in <c>Caf.Midden.Core</c>, rather than in the Blazor editor, for two reasons. First, the
/// editor renders its fields inside lazily-rendered <c>TabPane</c>s, so AntDesign's
/// <c>Form.Validate()</c> silently skips any field on a tab the user never opened - it cannot be
/// trusted as a save-time gate. Second, the CLI needs the identical rules so that
/// <c>collate</c> and a future <c>validate</c> command agree with the editor.
/// </para>
/// <para>
/// Severity is the important design decision here. Errors are reserved for things that break the
/// catalog or the file; everything that is merely low-quality is a warning the researcher can
/// knowingly accept. See <c>docs/implementation-plans/20260810_validation-improvements.md</c>.
/// </para>
/// </remarks>
public sealed partial class MetadataValidator : IValidator<DatasetMetadata>
{
    public ValidationResult Validate(DatasetMetadata model, AppConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        var issues = new IssueCollector();

        if (model.Dataset is null)
        {
            issues.Error(
                MetadataSections.Basic,
                "dataset",
                "dataset.missing",
                "This file does not contain a dataset.");

            return issues.ToResult();
        }

        var dataset = model.Dataset;

        ValidateIdentity(dataset, configuration, issues);
        ValidateDescriptionAndContacts(dataset, configuration, issues);
        ValidateTags(dataset, configuration, issues);
        ValidateVariables(dataset, configuration, issues);
        ValidateCoverage(dataset, issues);
        ValidateStructure(dataset, configuration, issues);

        return issues.ToResult();
    }

    private static void ValidateIdentity(
        Dataset dataset,
        AppConfiguration? configuration,
        IssueCollector issues)
    {
        if (string.IsNullOrWhiteSpace(dataset.Zone))
        {
            issues.Error(
                MetadataSections.Basic,
                "dataset.zone",
                "dataset.zone.required",
                "A zone is required.",
                "The zone determines where this dataset appears in the catalog.");
        }
        else
        {
            issues.WarnIfNotInVocabulary(
                MetadataSections.Basic,
                "dataset.zone",
                "dataset.zone.unknown",
                dataset.Zone,
                configuration?.Zones,
                "zones");
        }

        // The name is not just a label: the editor downloads it as "{Name}.midden" and the
        // crawlers derive the dataset path from that file name, so an unsafe name means the
        // dataset silently never reaches the catalog.
        var nameStatus = DatasetNameRules.Validate(dataset.Name);

        if (nameStatus == DatasetNameStatus.Empty)
        {
            issues.Error(
                MetadataSections.Basic,
                "dataset.name",
                "dataset.name.required",
                "A dataset name is required.",
                "The name becomes the file name of the downloaded .midden file.");
        }
        else if (nameStatus != DatasetNameStatus.Valid)
        {
            issues.Error(
                MetadataSections.Basic,
                "dataset.name",
                "dataset.name.unsafe",
                DatasetNameRules.DescribeProblem(nameStatus)!,
                DatasetNameRules.DescribeFix(nameStatus));
        }

        if (string.IsNullOrWhiteSpace(dataset.Project))
        {
            issues.Error(
                MetadataSections.Basic,
                "dataset.project",
                "dataset.project.required",
                "A project is required.",
                "Grouping datasets under a project gives data users the context behind the data.");
        }
    }

    private static void ValidateDescriptionAndContacts(
        Dataset dataset,
        AppConfiguration? configuration,
        IssueCollector issues)
    {
        if (string.IsNullOrWhiteSpace(dataset.Description))
        {
            issues.Warn(
                MetadataSections.Basic,
                "dataset.description",
                "dataset.description.missing",
                "This dataset has no description.",
                "A short paragraph on the origin and purpose of the data is the single most useful thing you can add.");
        }

        if (dataset.Contacts is null || dataset.Contacts.Count == 0)
        {
            issues.Warn(
                MetadataSections.Basic,
                "dataset.contacts",
                "dataset.contacts.missing",
                "This dataset has no contacts.",
                "Midden does not link to the data itself, so a contact is how a potential user starts the conversation about access.");

            return;
        }

        for (var i = 0; i < dataset.Contacts.Count; i++)
        {
            var contact = dataset.Contacts[i];
            var path = $"dataset.contacts[{i}]";

            // A contact row with no name cannot be acted on by anyone, so it is worse than no
            // contact at all.
            if (string.IsNullOrWhiteSpace(contact.Name))
            {
                issues.Error(
                    MetadataSections.Basic,
                    $"{path}.name",
                    "contact.name.required",
                    $"Contact {i + 1} has no name.",
                    "Remove the row, or give the contact a name.");
            }

            if (string.IsNullOrWhiteSpace(contact.Email))
            {
                issues.Warn(
                    MetadataSections.Basic,
                    $"{path}.email",
                    "contact.email.missing",
                    $"Contact {i + 1} has no email address.");
            }
            else if (!EmailPattern().IsMatch(contact.Email))
            {
                issues.Warn(
                    MetadataSections.Basic,
                    $"{path}.email",
                    "contact.email.malformed",
                    $"\"{contact.Email}\" does not look like an email address.");
            }

            issues.WarnIfNotInVocabulary(
                MetadataSections.Basic,
                $"{path}.role",
                "contact.role.unknown",
                contact.Role,
                configuration?.Roles,
                "roles");
        }
    }

    private static void ValidateTags(
        Dataset dataset,
        AppConfiguration? configuration,
        IssueCollector issues)
    {
        if (dataset.Tags is null || dataset.Tags.Count == 0)
        {
            issues.Warn(
                MetadataSections.Basic,
                "dataset.tags",
                "dataset.tags.missing",
                "This dataset has no tags.",
                "Tags are how users browse and search the catalog.");

            return;
        }

        for (var i = 0; i < dataset.Tags.Count; i++)
        {
            issues.WarnIfNotInVocabulary(
                MetadataSections.Basic,
                $"dataset.tags[{i}]",
                "dataset.tag.unknown",
                dataset.Tags[i],
                configuration?.Tags,
                "tags");
        }
    }

    private static void ValidateVariables(
        Dataset dataset,
        AppConfiguration? configuration,
        IssueCollector issues)
    {
        if (dataset.Variables is null || dataset.Variables.Count == 0)
        {
            issues.Warn(
                MetadataSections.Variables,
                "dataset.variables",
                "dataset.variables.missing",
                "This dataset has no variables.",
                "Variables are the data dictionary. Without them a user has to guess what the columns mean.");

            return;
        }

        for (var i = 0; i < dataset.Variables.Count; i++)
        {
            var variable = dataset.Variables[i];
            var path = $"dataset.variables[{i}]";
            var label = string.IsNullOrWhiteSpace(variable.Name)
                ? $"Variable {i + 1}"
                : $"Variable \"{variable.Name}\"";

            if (string.IsNullOrWhiteSpace(variable.Name))
            {
                issues.Error(
                    MetadataSections.Variables,
                    $"{path}.name",
                    "variable.name.required",
                    $"Variable {i + 1} has no name.",
                    "Remove the row, or give the variable a name.");
            }

            if (string.IsNullOrWhiteSpace(variable.Units))
            {
                issues.Error(
                    MetadataSections.Variables,
                    $"{path}.units",
                    "variable.units.required",
                    $"{label} has no units.",
                    "Use \"unitless\" or \"n/a\" if the variable genuinely has no units.");
            }

            if (string.IsNullOrWhiteSpace(variable.Description))
            {
                issues.Warn(
                    MetadataSections.Variables,
                    $"{path}.description",
                    "variable.description.missing",
                    $"{label} has no description.");
            }

            issues.WarnIfNotInVocabulary(
                MetadataSections.Variables,
                $"{path}.processingLevel",
                "variable.processingLevel.unknown",
                variable.ProcessingLevel,
                configuration?.ProcessingLevels,
                "processing levels");

            issues.WarnIfNotInVocabulary(
                MetadataSections.Variables,
                $"{path}.variableType",
                "variable.variableType.unknown",
                variable.VariableType,
                configuration?.VariableTypes,
                "variable types");

            if (variable.QCApplied is not null)
            {
                for (var q = 0; q < variable.QCApplied.Count; q++)
                {
                    issues.WarnIfNotInVocabulary(
                        MetadataSections.Variables,
                        $"{path}.qcApplied[{q}]",
                        "variable.qcApplied.unknown",
                        variable.QCApplied[q],
                        configuration?.QCTags,
                        "quality control tags");
                }
            }
        }
    }

    private static void ValidateCoverage(Dataset dataset, IssueCollector issues)
    {
        // Until now the only geometry check lived in the map's JavaScript, which is asynchronous
        // and therefore unusable as a save-time gate.
        var geometryStatus = GeoJsonGeometryValidator.Validate(dataset.Geometry);

        if (geometryStatus != GeoJsonGeometryStatus.Valid)
        {
            var code = geometryStatus is GeoJsonGeometryStatus.IsFeature
                or GeoJsonGeometryStatus.IsFeatureCollection
                ? "dataset.geometry.notAGeometry"
                : "dataset.geometry.invalid";

            issues.Error(
                MetadataSections.Coverage,
                "dataset.geometry",
                code,
                GeoJsonGeometryValidator.DescribeProblem(geometryStatus)!,
                GeoJsonGeometryValidator.DescribeFix(geometryStatus));
        }

        var extentStatus = TemporalExtentValidator.Validate(dataset.TemporalExtent);

        if (extentStatus != TemporalExtentStatus.Valid)
        {
            // A backwards interval parses fine and will not break the catalog, so it is a warning
            // rather than a hard stop.
            var severity = extentStatus == TemporalExtentStatus.EndBeforeStart
                ? ValidationSeverity.Warning
                : ValidationSeverity.Error;

            issues.Add(
                severity,
                MetadataSections.Coverage,
                "dataset.temporalExtent",
                $"dataset.temporalExtent.{(severity == ValidationSeverity.Warning ? "reversed" : "invalid")}",
                TemporalExtentValidator.DescribeProblem(extentStatus)!,
                TemporalExtentValidator.DescribeFix(extentStatus));
        }

        if (dataset.SpatialRepeats is < 0)
        {
            issues.Error(
                MetadataSections.Coverage,
                "dataset.spatialRepeats",
                "dataset.spatialRepeats.negative",
                "Spatial repeats cannot be negative.");
        }
    }

    private static void ValidateStructure(
        Dataset dataset,
        AppConfiguration? configuration,
        IssueCollector issues) =>
        issues.WarnIfNotInVocabulary(
            MetadataSections.Structure,
            "dataset.structure",
            "dataset.structure.unknown",
            dataset.Structure,
            configuration?.DatasetStructures,
            "dataset structures");

    /// <summary>
    /// Deliberately permissive. The goal is to catch a typo such as a missing "@", not to
    /// adjudicate RFC 5322 and reject a researcher's legitimate address.
    /// </summary>
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
