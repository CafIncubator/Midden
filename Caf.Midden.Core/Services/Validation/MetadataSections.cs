namespace Caf.Midden.Core.Services.Validation;

/// <summary>
/// The section keys used by <see cref="MetadataValidator"/>, which deliberately match the
/// <c>TabPane</c> keys in <c>MetadataEditor.razor</c> so the editor can bind tab badges directly
/// to <see cref="ValidationResult.CountsBySection"/> without a translation table.
/// </summary>
public static class MetadataSections
{
    /// <summary>Zone, Name, Project, Description, Contacts, Tags.</summary>
    public const string Basic = "1";

    /// <summary>Variables.</summary>
    public const string Variables = "2";

    /// <summary>Spatial repeats, spatial extent, temporal resolution, temporal extent.</summary>
    public const string Coverage = "3";

    /// <summary>File format, path template, path description, dataset structure.</summary>
    public const string Structure = "4";

    /// <summary>Methods, parent datasets, derived works.</summary>
    public const string Processing = "5";
}
