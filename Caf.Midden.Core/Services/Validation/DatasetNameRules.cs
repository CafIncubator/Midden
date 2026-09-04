namespace Caf.Midden.Core.Services.Validation;

/// <summary>
/// Why a dataset name is unusable as a file name.
/// </summary>
public enum DatasetNameStatus
{
    Valid,

    /// <summary>Null, empty, or whitespace only.</summary>
    Empty,

    /// <summary>Contains a character that is illegal or unsafe in a file name.</summary>
    IllegalCharacter,

    /// <summary>Leading or trailing whitespace or dots, which many file systems silently strip.</summary>
    LeadingOrTrailingWhitespaceOrDot,

    /// <summary>A name reserved by Windows, e.g. <c>CON</c> or <c>LPT1</c>.</summary>
    ReservedName
}

/// <summary>
/// Rules for <see cref="Models.v0_2.Dataset.Name"/>, which is not merely a label: the editor
/// downloads it as <c>{Name}.midden</c> and the CLI crawlers derive the dataset path from the
/// file name. A name that is unsafe as a path silently disappears from the catalog, because
/// <c>Collate</c> filters unsafe paths without reporting them.
/// </summary>
/// <remarks>
/// The illegal character set is hard-coded rather than taken from
/// <see cref="System.IO.Path.GetInvalidFileNameChars"/> so that a name authored in the browser on
/// Linux validates identically to one checked by the CLI on Windows.
/// </remarks>
public static class DatasetNameRules
{
    /// <summary>
    /// Characters that are illegal in a Windows file name, plus both path separators.
    /// </summary>
    public static readonly char[] IllegalCharacters =
        ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static DatasetNameStatus Validate(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DatasetNameStatus.Empty;
        }

        if (name.IndexOfAny(IllegalCharacters) >= 0 || name.Any(char.IsControl))
        {
            return DatasetNameStatus.IllegalCharacter;
        }

        if (char.IsWhiteSpace(name[0]) ||
            char.IsWhiteSpace(name[^1]) ||
            name[0] == '.' ||
            name[^1] == '.')
        {
            return DatasetNameStatus.LeadingOrTrailingWhitespaceOrDot;
        }

        // Windows reserves these regardless of extension, so "CON.midden" is unusable too.
        var stem = name.Split('.')[0];

        return ReservedNames.Contains(stem)
            ? DatasetNameStatus.ReservedName
            : DatasetNameStatus.Valid;
    }

    /// <summary>
    /// A researcher-facing explanation of <paramref name="status"/>, or null when it is valid.
    /// </summary>
    public static string? DescribeProblem(DatasetNameStatus status) => status switch
    {
        DatasetNameStatus.Valid => null,
        DatasetNameStatus.Empty => "A name is required.",
        DatasetNameStatus.IllegalCharacter =>
            $"The name cannot contain any of: {string.Join(' ', IllegalCharacters)}",
        DatasetNameStatus.LeadingOrTrailingWhitespaceOrDot =>
            "The name cannot start or end with a space or a dot.",
        DatasetNameStatus.ReservedName => "This name is reserved by Windows and cannot be used for a file.",
        _ => "This name cannot be used as a file name."
    };

    /// <summary>
    /// How to fix <paramref name="status"/>, or null when there is nothing useful to say.
    /// </summary>
    public static string? DescribeFix(DatasetNameStatus status) => status switch
    {
        DatasetNameStatus.Valid or DatasetNameStatus.Empty => null,
        _ => "The name becomes the file name of the downloaded .midden file, so it has to be a valid file name."
    };
}
