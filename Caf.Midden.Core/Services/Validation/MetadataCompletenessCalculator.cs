using System.Collections.ObjectModel;

// Aliased with a distinct name: the sibling namespace Caf.Midden.Core.Services.Metadata wins
// simple-name resolution against the model type, and an alias of the same name would not.
using DatasetMetadata = Caf.Midden.Core.Models.v0_2.Metadata;

namespace Caf.Midden.Core.Services.Validation;

/// <summary>
/// One weighted contribution to a metadata completeness score.
/// </summary>
/// <param name="Key">Stable identifier, e.g. <c>variables</c>.</param>
/// <param name="Label">User-facing name of the thing being scored.</param>
/// <param name="Weight">How many of the 100 available points this is worth.</param>
/// <param name="IsComplete">Whether the points were earned.</param>
/// <param name="Suggestion">Why it is worth filling in, shown when incomplete.</param>
public sealed record CompletenessItem(
    string Key,
    string Label,
    int Weight,
    bool IsComplete,
    string Suggestion);

/// <summary>
/// The outcome of scoring a dataset's completeness.
/// </summary>
public sealed class CompletenessResult
{
    public CompletenessResult(IEnumerable<CompletenessItem> items)
    {
        Items = new ReadOnlyCollection<CompletenessItem>([.. items]);

        var total = Items.Sum(i => i.Weight);

        Percent = total == 0
            ? 100
            : (int)Math.Round(
                Items.Where(i => i.IsComplete).Sum(i => i.Weight) * 100d / total,
                MidpointRounding.AwayFromZero);
    }

    public IReadOnlyList<CompletenessItem> Items { get; }

    /// <summary>
    /// Completeness from 0 to 100.
    /// </summary>
    public int Percent { get; }

    /// <summary>
    /// The unearned items, heaviest first, so a UI can show the highest-leverage suggestions.
    /// </summary>
    public IEnumerable<CompletenessItem> TopSuggestions =>
        Items.Where(i => !i.IsComplete).OrderByDescending(i => i.Weight);
}

/// <summary>
/// Scores how thoroughly a dataset has been documented.
/// </summary>
/// <remarks>
/// Intentionally separate from <see cref="MetadataValidator"/>. Validation answers "may I publish
/// this?"; completeness answers "how good is this?". Conflating them produces a form that either
/// nags about optional fields or stays silent about them - and in practice a visible score moves
/// metadata quality further than any blocking rule, because it rewards effort instead of
/// punishing omission.
/// </remarks>
public static class MetadataCompletenessCalculator
{
    public static CompletenessResult Calculate(DatasetMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var dataset = metadata.Dataset;

        if (dataset is null)
        {
            return new CompletenessResult([]);
        }

        return new CompletenessResult(
        [
            Item("zone", "Zone", 10, HasText(dataset.Zone),
                "Sets where the dataset appears in the catalog."),
            Item("name", "Name", 10, HasText(dataset.Name),
                "Identifies the dataset."),
            Item("project", "Project", 10, HasText(dataset.Project),
                "Groups the dataset with related work."),
            Item("description", "Description", 15, HasText(dataset.Description),
                "The first thing a data user reads."),
            Item("variables", "Variables", 15, HasAny(dataset.Variables),
                "The data dictionary; without it users have to guess what the columns mean."),
            Item("contacts", "Contacts", 10, HasAny(dataset.Contacts),
                "How a potential user asks you for access."),
            Item("tags", "Tags", 10, HasAny(dataset.Tags),
                "How the dataset is found by browsing and searching."),
            Item("geometry", "Spatial extent", 5, HasText(dataset.Geometry),
                "Lets users find data by location."),
            Item("temporalExtent", "Temporal extent", 5, HasText(dataset.TemporalExtent),
                "Lets users tell at a glance whether the data covers their period of interest."),
            Item("structure", "File format and structure", 5,
                HasText(dataset.Format) || HasText(dataset.Structure),
                "Tells users what they will actually receive."),
            Item("methods", "Methods", 5, HasAny(dataset.Methods),
                "Documents how the data were produced.")
        ]);
    }

    private static CompletenessItem Item(
        string key,
        string label,
        int weight,
        bool isComplete,
        string suggestion) =>
        new(key, label, weight, isComplete, suggestion);

    private static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value);

    private static bool HasAny<T>(IReadOnlyCollection<T>? values) => values is { Count: > 0 };
}
