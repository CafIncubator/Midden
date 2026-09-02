namespace Caf.Midden.Core.Services;

/// <summary>
/// File naming conventions shared by every crawler in <c>Caf.Midden.Cli</c>. Kept in
/// <c>Caf.Midden.Core</c>, rather than duplicated per crawler, so the four independent
/// implementations (local disk, Azure Data Lake, Azure File Shares, Google Drive) cannot drift.
/// </summary>
public static class MiddenFileConventions
{
    /// <summary>
    /// The extension of a dataset metadata file.
    /// </summary>
    public const string MiddenFileExtension = ".midden";

    /// <summary>
    /// The file name that marks a project description.
    /// </summary>
    public const string MippenFileSearchTerm = "DESCRIPTION.md";

    /// <summary>
    /// Removes exactly one trailing occurrence of <paramref name="suffix"/> from
    /// <paramref name="value"/>, if present. Unlike <see cref="string.Replace(string, string)"/>,
    /// this never touches any other occurrence of the suffix elsewhere in the path - for example
    /// only the trailing ".midden" is removed from "archive.midden.data/x.midden".
    /// </summary>
    public static string TrimSuffix(string value, string suffix)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrEmpty(suffix);

        return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? value[..^suffix.Length]
            : value;
    }

    /// <summary>
    /// Converts a crawler path to the forward-slash form stored in catalogs.
    /// </summary>
    public static string NormalizeDatasetPath(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Replace('\\', '/');
    }
}
