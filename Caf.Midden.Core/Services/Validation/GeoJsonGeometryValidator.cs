using System.Text.Json;

namespace Caf.Midden.Core.Services.Validation;

/// <summary>
/// Why a candidate GeoJSON geometry string was rejected.
/// </summary>
public enum GeoJsonGeometryStatus
{
    Valid,

    /// <summary>The text is not parseable JSON at all.</summary>
    NotJson,

    /// <summary>The JSON parsed, but the root is not an object.</summary>
    NotAnObject,

    /// <summary>No <c>type</c> member.</summary>
    MissingType,

    /// <summary><c>type</c> is not one of the seven GeoJSON geometry types.</summary>
    UnknownType,

    /// <summary>
    /// A whole <c>Feature</c> was supplied. Midden stores only the geometry member, so this is
    /// separated from <see cref="UnknownType"/> so the UI can offer to unwrap it.
    /// </summary>
    IsFeature,

    /// <summary>A whole <c>FeatureCollection</c> was supplied. See <see cref="IsFeature"/>.</summary>
    IsFeatureCollection,

    /// <summary>A simple geometry type with no <c>coordinates</c> member.</summary>
    MissingCoordinates,

    /// <summary>A <c>GeometryCollection</c> with no <c>geometries</c> member.</summary>
    MissingGeometries
}

/// <summary>
/// A deterministic, C#-side structural check of a GeoJSON <em>geometry</em> object.
/// </summary>
/// <remarks>
/// <para>
/// The editor's map component only reports validity asynchronously through JS interop, which is
/// unusable as a save-time gate. This performs the structural subset of RFC 7946 that actually
/// matters for Midden: is it an object, does it declare a known geometry type, and does it carry
/// the member that type requires.
/// </para>
/// <para>
/// Coordinate values themselves are intentionally not range-checked; the map is the better tool
/// for catching a point in the wrong hemisphere, and false rejections here would be worse than
/// the miss.
/// </para>
/// </remarks>
public static class GeoJsonGeometryValidator
{
    private static readonly HashSet<string> SimpleGeometryTypes = new(StringComparer.Ordinal)
    {
        "Point",
        "MultiPoint",
        "LineString",
        "MultiLineString",
        "Polygon",
        "MultiPolygon"
    };

    /// <summary>
    /// Structurally validates <paramref name="geoJson"/> as a GeoJSON geometry object.
    /// </summary>
    /// <remarks>
    /// Null/whitespace returns <see cref="GeoJsonGeometryStatus.Valid"/>: "absent" is a separate
    /// concern from "malformed", and geometry is optional throughout Midden. Callers that require
    /// a value should check for emptiness themselves.
    /// </remarks>
    public static GeoJsonGeometryStatus Validate(string? geoJson)
    {
        if (string.IsNullOrWhiteSpace(geoJson))
        {
            return GeoJsonGeometryStatus.Valid;
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(geoJson);
        }
        catch (JsonException)
        {
            return GeoJsonGeometryStatus.NotJson;
        }

        using (document)
        {
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return GeoJsonGeometryStatus.NotAnObject;
            }

            if (!root.TryGetProperty("type", out var typeElement) ||
                typeElement.ValueKind != JsonValueKind.String)
            {
                return GeoJsonGeometryStatus.MissingType;
            }

            var type = typeElement.GetString();

            if (string.Equals(type, "Feature", StringComparison.Ordinal))
            {
                return GeoJsonGeometryStatus.IsFeature;
            }

            if (string.Equals(type, "FeatureCollection", StringComparison.Ordinal))
            {
                return GeoJsonGeometryStatus.IsFeatureCollection;
            }

            if (string.Equals(type, "GeometryCollection", StringComparison.Ordinal))
            {
                return root.TryGetProperty("geometries", out var geometries) &&
                       geometries.ValueKind == JsonValueKind.Array
                    ? GeoJsonGeometryStatus.Valid
                    : GeoJsonGeometryStatus.MissingGeometries;
            }

            if (type is null || !SimpleGeometryTypes.Contains(type))
            {
                return GeoJsonGeometryStatus.UnknownType;
            }

            return root.TryGetProperty("coordinates", out var coordinates) &&
                   coordinates.ValueKind == JsonValueKind.Array
                ? GeoJsonGeometryStatus.Valid
                : GeoJsonGeometryStatus.MissingCoordinates;
        }
    }

    /// <summary>
    /// A researcher-facing explanation of <paramref name="status"/>, or null when it is valid.
    /// </summary>
    public static string? DescribeProblem(GeoJsonGeometryStatus status) => status switch
    {
        GeoJsonGeometryStatus.Valid => null,
        GeoJsonGeometryStatus.NotJson => "This is not valid JSON.",
        GeoJsonGeometryStatus.NotAnObject => "GeoJSON must be a single object, e.g. { \"type\": \"Point\", ... }.",
        GeoJsonGeometryStatus.MissingType => "GeoJSON must include a \"type\" value, e.g. \"Point\" or \"Polygon\".",
        GeoJsonGeometryStatus.UnknownType => "\"type\" must be one of Point, MultiPoint, LineString, MultiLineString, Polygon, MultiPolygon, or GeometryCollection.",
        GeoJsonGeometryStatus.IsFeature => "This is a complete GeoJSON Feature. Midden stores only its \"geometry\" value.",
        GeoJsonGeometryStatus.IsFeatureCollection => "This is a GeoJSON FeatureCollection. Midden stores a single geometry.",
        GeoJsonGeometryStatus.MissingCoordinates => "This geometry is missing its \"coordinates\" value.",
        GeoJsonGeometryStatus.MissingGeometries => "A GeometryCollection is missing its \"geometries\" value.",
        _ => "This is not a valid GeoJSON geometry."
    };

    /// <summary>
    /// How to fix <paramref name="status"/>, or null when there is nothing useful to say.
    /// </summary>
    public static string? DescribeFix(GeoJsonGeometryStatus status) => status switch
    {
        GeoJsonGeometryStatus.IsFeature =>
            "Copy just the \"geometry\" value from the Feature, or redraw the shape on the map.",
        GeoJsonGeometryStatus.IsFeatureCollection =>
            "Copy the \"geometry\" value of a single feature, or redraw the shape on the map.",
        GeoJsonGeometryStatus.NotJson or GeoJsonGeometryStatus.NotAnObject =>
            "Drawing the shape on the map will produce correctly formatted GeoJSON.",
        _ => null
    };
}
