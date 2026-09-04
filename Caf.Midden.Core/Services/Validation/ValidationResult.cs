using System.Collections.ObjectModel;

namespace Caf.Midden.Core.Services.Validation;

/// <summary>
/// The complete set of findings for one model instance.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// A result with no findings at all.
    /// </summary>
    public static ValidationResult Empty { get; } = new([]);

    public ValidationResult(IEnumerable<ValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        Issues = new ReadOnlyCollection<ValidationIssue>([.. issues]);
    }

    /// <summary>
    /// Every finding, in the order the validator produced them (roughly editor reading order).
    /// </summary>
    public IReadOnlyList<ValidationIssue> Issues { get; }

    public IEnumerable<ValidationIssue> Errors =>
        Issues.Where(i => i.Severity == ValidationSeverity.Error);

    public IEnumerable<ValidationIssue> Warnings =>
        Issues.Where(i => i.Severity == ValidationSeverity.Warning);

    /// <summary>
    /// True when at least one <see cref="ValidationSeverity.Error"/> is present, i.e. the explicit
    /// save/download action must be blocked.
    /// </summary>
    public bool HasErrors => Issues.Any(i => i.Severity == ValidationSeverity.Error);

    public bool HasWarnings => Issues.Any(i => i.Severity == ValidationSeverity.Warning);

    /// <summary>
    /// Issue counts keyed by <see cref="ValidationIssue.Section"/>, for tab badges. Only counts
    /// issues at or above <paramref name="minimumSeverity"/>.
    /// </summary>
    public IReadOnlyDictionary<string, int> CountsBySection(
        ValidationSeverity minimumSeverity = ValidationSeverity.Error) =>
        Issues
            .Where(i => i.Severity >= minimumSeverity)
            .GroupBy(i => i.Section)
            .ToDictionary(g => g.Key, g => g.Count());
}
