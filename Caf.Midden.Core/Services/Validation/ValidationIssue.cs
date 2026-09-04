namespace Caf.Midden.Core.Services.Validation;

/// <summary>
/// A single, presentation-agnostic validation finding.
/// </summary>
/// <remarks>
/// Deliberately carries no UI concepts. <see cref="Path"/> is the contract that lets a UI map an
/// issue back to a control (and therefore to a tab or a table row) without the validator knowing
/// anything about tabs or tables.
/// </remarks>
public sealed record ValidationIssue
{
    /// <summary>
    /// How much this issue should interfere with the user.
    /// </summary>
    public required ValidationSeverity Severity { get; init; }

    /// <summary>
    /// A stable, machine-readable identifier such as <c>dataset.zone.required</c>. Callers should
    /// branch on this rather than on <see cref="Message"/>, which is user-facing prose.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// A dotted/indexed path to the offending value, e.g. <c>dataset.variables[3].units</c> or
    /// <c>geometries[1].geojson</c>. Used by editors to focus the right control.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// What is wrong, phrased for a researcher rather than a developer.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Optional guidance on how to fix it.
    /// </summary>
    public string? Hint { get; init; }

    /// <summary>
    /// The logical section (in the metadata editor, the tab) that owns <see cref="Path"/>. Lets a
    /// UI render group badges without knowing how paths are structured. Defaults to the first
    /// segment of <see cref="Path"/> when a validator does not set it explicitly.
    /// </summary>
    public string Section
    {
        get
        {
            if (_section is not null)
            {
                return _section;
            }

            var separatorIndex = Path.IndexOfAny(['.', '[']);

            return separatorIndex < 0 ? Path : Path[..separatorIndex];
        }

        init => _section = value;
    }

    private readonly string? _section;

    public override string ToString() => $"{Severity}: {Path} - {Message}";
}
