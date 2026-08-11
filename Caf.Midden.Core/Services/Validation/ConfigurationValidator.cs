// Aliased with a distinct name: the sibling namespace Caf.Midden.Core.Services.Configuration wins
// simple-name resolution against the model type, and an alias of the same name would not.
using AppConfiguration = Caf.Midden.Core.Models.v0_2.Configuration;

namespace Caf.Midden.Core.Services.Validation;

/// <summary>
/// Validates the app configuration. Because the configuration supplies every vocabulary the other
/// two editors offer, a mistake here degrades the experience of everyone using the tool - yet
/// <c>ConfigurationEditor</c> currently validates nothing at all.
/// </summary>
public sealed class ConfigurationValidator : IValidator<AppConfiguration>
{
    /// <summary>
    /// The section key used for every configuration issue; the editor is a single flat form.
    /// </summary>
    public const string Section = "configuration";

    public ValidationResult Validate(AppConfiguration model, AppConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        var issues = new IssueCollector();

        RequireText(issues, model.OrganizationName, "organizationName", "An organization name is required.");
        RequireText(issues, model.ToolName, "toolName", "A tool name is required.");
        RequireText(
            issues,
            model.CatalogPath,
            "catalogPath",
            "A catalog path is required.",
            "Without it the app cannot find catalog.json and the catalog will be empty.");

        ValidateCatalogPath(model, issues);

        ValidateGeometries(model, issues);

        // Zones drive a required field in the metadata editor, so an empty list makes it
        // impossible for a researcher to produce a valid dataset.
        if (model.Zones is null || model.Zones.Count == 0)
        {
            issues.Warn(
                Section,
                "configuration.zones",
                "configuration.zones.empty",
                "No zones are configured.",
                "Zone is required on every dataset, so the metadata editor will have nothing to offer.");
        }

        WarnOnDuplicates(issues, model.Zones, "zones", "Zone");
        WarnOnDuplicates(issues, model.Roles, "roles", "Role");
        WarnOnDuplicates(issues, model.ProjectStatuses, "projectStatuses", "Project status");
        WarnOnDuplicates(issues, model.ProcessingLevels, "processingLevels", "Processing level");
        WarnOnDuplicates(issues, model.VariableTypes, "variableTypes", "Variable type");
        WarnOnDuplicates(issues, model.DatasetStructures, "datasetStructures", "Dataset structure");
        WarnOnDuplicates(issues, model.QCTags, "qualityControlTags", "Quality control tag");
        WarnOnDuplicates(issues, model.Tags, "tags", "Tag");

        return issues.ToResult();
    }

    private static void RequireText(
        IssueCollector issues,
        string? value,
        string property,
        string message,
        string? hint = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Error(
                Section,
                $"configuration.{property}",
                $"configuration.{property}.required",
                message,
                hint);
        }
    }

    /// <summary>
    /// Checks the shape of the catalog path. <c>CatalogReaderHttp</c> deserializes whatever this
    /// points at as JSON, so a path that is not a <c>.json</c> file is almost always a mistake -
    /// but only almost. It is a request path, not a file path, so a server route or rewrite can
    /// legitimately serve JSON without the extension. Hence a warning rather than an error.
    /// </summary>
    private static void ValidateCatalogPath(AppConfiguration model, IssueCollector issues)
    {
        var path = model.CatalogPath?.Trim();

        if (string.IsNullOrEmpty(path))
        {
            // Already reported as a required-field error.
            return;
        }

        if (path.StartsWith('/'))
        {
            issues.Warn(
                Section,
                "configuration.catalogPath",
                "configuration.catalogPath.rooted",
                "The catalog path starts with \"/\".",
                "It is resolved relative to the app's base address, so a leading slash breaks the catalog when the app is hosted in a subfolder.");
        }

        // Ignore any query string; the reader appends its own cache-busting one.
        var withoutQuery = path.Split('?', 2)[0];

        if (!withoutQuery.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            issues.Warn(
                Section,
                "configuration.catalogPath",
                "configuration.catalogPath.notJson",
                "The catalog path does not end in \".json\".",
                "The catalog is read as JSON. This works only if the address still serves JSON, such as a server route.");
        }
    }

    private static void ValidateGeometries(AppConfiguration model, IssueCollector issues)
    {
        if (model.Geometries is null)
        {
            return;
        }

        for (var i = 0; i < model.Geometries.Count; i++)
        {
            var geometry = model.Geometries[i];
            var path = $"configuration.geometries[{i}]";
            var label = string.IsNullOrWhiteSpace(geometry.Name)
                ? $"Geometry {i + 1}"
                : $"Geometry \"{geometry.Name}\"";

            if (string.IsNullOrWhiteSpace(geometry.Name))
            {
                issues.Error(
                    Section,
                    $"{path}.name",
                    "configuration.geometry.name.required",
                    $"Geometry {i + 1} has no name.",
                    "The name is what researchers pick from the spatial extent list.");
            }

            if (string.IsNullOrWhiteSpace(geometry.GeoJson))
            {
                issues.Error(
                    Section,
                    $"{path}.geojson",
                    "configuration.geometry.geojson.required",
                    $"{label} has no shape.",
                    "Draw the shape on the map, or remove the row.");

                continue;
            }

            var status = GeoJsonGeometryValidator.Validate(geometry.GeoJson);

            if (status != GeoJsonGeometryStatus.Valid)
            {
                issues.Error(
                    Section,
                    $"{path}.geojson",
                    "configuration.geometry.geojson.invalid",
                    $"{label}: {GeoJsonGeometryValidator.DescribeProblem(status)}",
                    GeoJsonGeometryValidator.DescribeFix(status));
            }
        }

        WarnOnDuplicates(
            issues,
            [.. model.Geometries.Select(g => g.Name)],
            "geometries",
            "Geometry name");
    }

    private static void WarnOnDuplicates(
        IssueCollector issues,
        IReadOnlyList<string>? values,
        string property,
        string label)
    {
        if (values is null || values.Count == 0)
        {
            return;
        }

        var duplicates = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .GroupBy(v => v.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicate in duplicates)
        {
            issues.Warn(
                Section,
                $"configuration.{property}",
                $"configuration.{property}.duplicate",
                $"{label} \"{duplicate}\" is listed more than once.",
                "Duplicates appear twice in every dropdown built from this list.");
        }
    }
}
