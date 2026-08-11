namespace Caf.Midden.Core.Services.Validation;

/// <summary>
/// Accumulates <see cref="ValidationIssue"/>s in reading order, keeping the validators themselves
/// free of repetitive object-initializer noise.
/// </summary>
internal sealed class IssueCollector
{
    private readonly List<ValidationIssue> issues = [];

    public void Error(string section, string path, string code, string message, string? hint = null) =>
        Add(ValidationSeverity.Error, section, path, code, message, hint);

    public void Warn(string section, string path, string code, string message, string? hint = null) =>
        Add(ValidationSeverity.Warning, section, path, code, message, hint);

    public void Info(string section, string path, string code, string message, string? hint = null) =>
        Add(ValidationSeverity.Info, section, path, code, message, hint);

    public void Add(
        ValidationSeverity severity,
        string section,
        string path,
        string code,
        string message,
        string? hint = null) =>
        issues.Add(new ValidationIssue
        {
            Severity = severity,
            Section = section,
            Path = path,
            Code = code,
            Message = message,
            Hint = hint
        });

    /// <summary>
    /// Reports a value that is absent from a configured vocabulary.
    /// </summary>
    /// <remarks>
    /// Always a warning, never an error. Vocabularies differ between organizations and change over
    /// time, so a <c>.midden</c> authored elsewhere or before a configuration change will
    /// legitimately contain unknown values. Skipped entirely when the vocabulary is unavailable or
    /// empty, so an unconfigured app does not flood the user with noise.
    /// </remarks>
    public void WarnIfNotInVocabulary(
        string section,
        string path,
        string code,
        string? value,
        IReadOnlyCollection<string>? vocabulary,
        string label)
    {
        if (string.IsNullOrWhiteSpace(value) || vocabulary is null || vocabulary.Count == 0)
        {
            return;
        }

        if (vocabulary.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        Warn(
            section,
            path,
            code,
            $"\"{value}\" is not one of the configured {label}.",
            $"This is allowed, but the value will not match anything else in the catalog. Add it to the configuration if it is intentional.");
    }

    public ValidationResult ToResult() => new(issues);
}
