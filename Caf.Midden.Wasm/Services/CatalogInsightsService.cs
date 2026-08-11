using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services.Validation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Caf.Midden.Wasm.Services;

public sealed record KeyCount(string Key, int Count);

public sealed record ZoneCount(string Zone, int Count);

public sealed record StatusCount(string Status, int Count);

/// <summary>
/// A point on the dataset growth timeline. <paramref name="Date"/> is the first day of a month,
/// formatted as "yyyy-MM".
/// </summary>
public sealed record TimelinePoint(string Date, int Count);

/// <summary>
/// A project name referenced by at least one dataset for which no <see cref="Project"/> record
/// exists in the catalog.
/// </summary>
public sealed record OrphanedProject(string Name, int DatasetCount);

/// <summary>
/// A dataset with a low <see cref="MetadataCompletenessCalculator"/> score, surfaced so a
/// researcher can jump straight to the record that most needs work.
/// </summary>
public sealed record LowCompletenessDataset(string Name, string Zone, string Project, int Percent);

/// <summary>
/// Catalog-wide rollup of <see cref="MetadataCompletenessCalculator"/> scores.
/// </summary>
public sealed record CompletenessSummary(
    int AveragePercent,
    IReadOnlyList<LowCompletenessDataset> LowestScoring)
{
    public static CompletenessSummary Empty { get; } = new(100, Array.Empty<LowCompletenessDataset>());
}

/// <summary>
/// Catalog-wide rollup of <see cref="MetadataValidator"/> results.
/// </summary>
public sealed record ValidationHealthSummary(int Clean, int WithWarnings, int WithErrors)
{
    public static ValidationHealthSummary Empty { get; } = new(0, 0, 0);
}

/// <summary>
/// Whether documented projects actually have datasets, and how project status breaks down by
/// the amount of dataset activity behind it rather than by raw project count.
/// </summary>
public sealed record ProjectCoverageSummary(
    int ProjectsWithDatasets,
    int ProjectsWithoutDatasets,
    IReadOnlyList<StatusCount> DatasetWeightedStatusCounts)
{
    public static ProjectCoverageSummary Empty { get; } = new(0, 0, Array.Empty<StatusCount>());
}

/// <summary>
/// How many datasets fall within a given calendar year of <see cref="Dataset.TemporalExtent"/>
/// coverage. Years with no coverage are gaps a data user would otherwise have to discover by hand.
/// </summary>
public sealed record TemporalCoverageYear(int Year, int Count);

/// <summary>
/// The min/max envelope of a single dataset's <see cref="Dataset.Geometry"/>, in degrees.
/// Collapsing each geometry to four corners server-side keeps the catalog-wide coverage map
/// cheap: the browser never parses full geometry, and vertex count stops scaling with detail.
/// <para>
/// When <paramref name="IsPoint"/> is true the envelope is degenerate (zero width and/or height),
/// which happens for point geometries and for perfectly axis-aligned lines. Such a box cannot be
/// drawn as a rectangle because it would have no visible area, so the map renders it as a marker
/// instead. The dataset genuinely claims no area, so it is not buffered into a fake one.
/// </para>
/// </summary>
public sealed record SpatialBoundingBox(
    double West,
    double South,
    double East,
    double North,
    bool IsPoint)
{
    /// <summary>The envelope centre, used as the marker position for degenerate boxes.</summary>
    public double CenterLongitude => (West + East) / 2;

    /// <summary>The envelope centre, used as the marker position for degenerate boxes.</summary>
    public double CenterLatitude => (South + North) / 2;
}

/// <summary>
/// Catalog-wide spatial coverage, expressed as per-dataset bounding boxes. <paramref name="Shown"/>
/// is capped; <paramref name="TotalWithGeometry"/> reports how many datasets actually declare a
/// geometry so the UI can say when it is showing a subset.
/// </summary>
public sealed record SpatialCoverageSummary(
    IReadOnlyList<SpatialBoundingBox> Boxes,
    int Shown,
    int TotalWithGeometry)
{
    public static SpatialCoverageSummary Empty { get; } = new(Array.Empty<SpatialBoundingBox>(), 0, 0);

    public bool IsTruncated => TotalWithGeometry > Shown;

    /// <summary>Datasets drawn as markers because they declare a location but no area.</summary>
    public int PointCount => Boxes.Count(box => box.IsPoint);

    /// <summary>Datasets drawn as rectangles because they declare a real extent.</summary>
    public int AreaCount => Shown - PointCount;
}

public sealed record CatalogInsightsSnapshot(
    int TotalDatasets,
    int TotalVariables,
    int TotalTags,
    int TotalContacts,
    int TotalProjects,
    DateTime CatalogLastUpdate,
    IReadOnlyList<KeyCount> TopDatasetTags,
    IReadOnlyList<KeyCount> TopVariableTags,
    IReadOnlyList<KeyCount> TopTags,
    IReadOnlyList<KeyCount> TopContacts,
    IReadOnlyList<ZoneCount> DatasetsByZone,
    IReadOnlyList<StatusCount> ProjectsByStatus,
    IReadOnlyList<TimelinePoint> DatasetGrowth,
    IReadOnlyList<OrphanedProject> OrphanedProjects,
    CompletenessSummary Completeness,
    ValidationHealthSummary ValidationHealth,
    IReadOnlyList<KeyCount> DatasetsPerProject,
    int DatasetsPerProjectTotal,
    ProjectCoverageSummary ProjectCoverage,
    IReadOnlyList<TemporalCoverageYear> TemporalCoverage,
    SpatialCoverageSummary SpatialCoverage)
{
    public static CatalogInsightsSnapshot Empty { get; } = new(
        0,
        0,
        0,
        0,
        0,
        DateTime.MinValue,
        Array.Empty<KeyCount>(),
        Array.Empty<KeyCount>(),
        Array.Empty<KeyCount>(),
        Array.Empty<KeyCount>(),
        Array.Empty<ZoneCount>(),
        Array.Empty<StatusCount>(),
        Array.Empty<TimelinePoint>(),
        Array.Empty<OrphanedProject>(),
        CompletenessSummary.Empty,
        ValidationHealthSummary.Empty,
        Array.Empty<KeyCount>(),
        0,
        ProjectCoverageSummary.Empty,
        Array.Empty<TemporalCoverageYear>(),
        SpatialCoverageSummary.Empty);
}

public sealed class CatalogInsightsService
{
    private const int TopCountLimit = 5;
    private const int LowestCompletenessLimit = 5;
    private const int DatasetsPerProjectLimit = 12;
    private const int SpatialCoverageLimit = 250;

    // ~1e-6 degrees is roughly 0.1 m; below this an extent is a point for display purposes.
    private const double DegenerateExtentTolerance = 1e-6;

    private static readonly MetadataValidator Validator = new();

    public CatalogInsightsSnapshot BuildSnapshot(Catalog? catalog, Configuration? configuration)
    {
        if (catalog is null || configuration is null)
        {
            return CatalogInsightsSnapshot.Empty;
        }

        List<Metadata> metadatas = catalog.Metadatas ?? new List<Metadata>();
        List<Project> projects = catalog.Projects ?? new List<Project>();

        List<string> datasetTags = new();
        List<string> variableTags = new();
        List<string> allTags = new();
        List<string> contacts = new();

        foreach (Metadata metadata in metadatas)
        {
            Dataset dataset = metadata.Dataset ?? new Dataset();
            List<string> currentDatasetTags = dataset.Tags ?? new List<string>();
            List<Variable> variables = dataset.Variables ?? new List<Variable>();

            datasetTags.AddRange(currentDatasetTags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
            allTags.AddRange(currentDatasetTags.Where(tag => !string.IsNullOrWhiteSpace(tag)));

            foreach (Variable variable in variables)
            {
                List<string> currentVariableTags = variable.Tags ?? new List<string>();
                variableTags.AddRange(currentVariableTags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
                allTags.AddRange(currentVariableTags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
            }

            contacts.AddRange(
                (dataset.Contacts ?? new List<Person>())
                    .Select(person => person.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!));
        }

        // Datasets per (normalized) project name is the join key behind orphaned projects,
        // project coverage, and the "datasets per project" chart, so it is computed once.
        Dictionary<string, int> datasetCountByNormalizedProject = metadatas
            .Select(metadata => metadata.Dataset?.Project)
            .Where(project => !string.IsNullOrWhiteSpace(project))
            .GroupBy(project => Normalize(project), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        return new CatalogInsightsSnapshot(
            TotalDatasets: metadatas.Count,
            TotalVariables: metadatas.SelectMany(metadata => metadata.Dataset?.Variables ?? new List<Variable>()).Count(),
            TotalTags: allTags.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            TotalContacts: contacts.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            // A "project" is a documented Project record, matching the Projects page and the
            // status chart. Datasets that reference an undocumented project are surfaced
            // separately as OrphanedProjects rather than folded into this count.
            TotalProjects: projects.Count,
            CatalogLastUpdate: catalog.CreationDate,
            TopDatasetTags: BuildTopCounts(datasetTags),
            TopVariableTags: BuildTopCounts(variableTags),
            TopTags: BuildTopCounts(allTags),
            TopContacts: BuildTopCounts(contacts),
            DatasetsByZone: BuildDatasetsByZone(metadatas, configuration),
            ProjectsByStatus: BuildProjectsByStatus(projects, configuration),
            DatasetGrowth: BuildDatasetGrowth(metadatas),
            OrphanedProjects: BuildOrphanedProjects(projects, datasetCountByNormalizedProject),
            Completeness: BuildCompletenessSummary(metadatas),
            ValidationHealth: BuildValidationHealth(metadatas, configuration),
            DatasetsPerProject: BuildDatasetsPerProject(datasetCountByNormalizedProject),
            DatasetsPerProjectTotal: datasetCountByNormalizedProject.Count,
            ProjectCoverage: BuildProjectCoverage(projects, datasetCountByNormalizedProject),
            TemporalCoverage: BuildTemporalCoverage(metadatas),
            SpatialCoverage: BuildSpatialCoverage(metadatas));
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim();

    private static IReadOnlyList<KeyCount> BuildTopCounts(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new KeyCount(group.First(), group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Take(TopCountLimit)
            .ToArray();
    }

    private static IReadOnlyList<ZoneCount> BuildDatasetsByZone(IEnumerable<Metadata> metadatas, Configuration configuration)
    {
        return configuration.Zones
            .Select(zone =>
            {
                string normalizedZone = Normalize(zone);
                int count = metadatas.Count(metadata =>
                    string.Equals(
                        Normalize(metadata.Dataset?.Zone),
                        normalizedZone,
                        StringComparison.OrdinalIgnoreCase));

                return new ZoneCount(zone, count);
            })
            .ToArray();
    }

    private static IReadOnlyList<StatusCount> BuildProjectsByStatus(IEnumerable<Project> projects, Configuration configuration)
    {
        return configuration.ProjectStatuses
            .Select(status =>
            {
                string normalizedStatus = Normalize(status);
                int count = projects.Count(project =>
                    string.Equals(
                        Normalize(project.ProjectStatus),
                        normalizedStatus,
                        StringComparison.OrdinalIgnoreCase));

                return new StatusCount(status, count);
            })
            .ToArray();
    }

    private static IReadOnlyList<TimelinePoint> BuildDatasetGrowth(IEnumerable<Metadata> metadatas)
    {
        Dictionary<DateTime, int> createdPerMonth = metadatas
            .Where(metadata => metadata.CreationDate != DateTime.MinValue)
            .GroupBy(metadata => new DateTime(metadata.CreationDate.Year, metadata.CreationDate.Month, 1))
            .ToDictionary(group => group.Key, group => group.Count());

        if (createdPerMonth.Count == 0)
        {
            return Array.Empty<TimelinePoint>();
        }

        DateTime firstMonth = createdPerMonth.Keys.Min();
        DateTime currentMonth = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        DateTime earliestVisibleMonth = new(currentMonth.Year - 10, 1, 1);
        DateTime startMonth = firstMonth < earliestVisibleMonth ? earliestVisibleMonth : firstMonth;
        int runningTotal = createdPerMonth
            .Where(entry => entry.Key < startMonth)
            .Sum(entry => entry.Value);

        List<TimelinePoint> points = new();

        for (DateTime month = startMonth; month <= currentMonth; month = month.AddMonths(1))
        {
            if (createdPerMonth.TryGetValue(month, out int count))
            {
                runningTotal += count;
            }

            points.Add(new TimelinePoint(
                month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                runningTotal));
        }

        return points;
    }

    private static IReadOnlyList<OrphanedProject> BuildOrphanedProjects(
        IEnumerable<Project> projects,
        IReadOnlyDictionary<string, int> datasetCountByNormalizedProject)
    {
        HashSet<string> documentedProjectNames = new(
            projects.Select(project => Normalize(project.Name)),
            StringComparer.OrdinalIgnoreCase);

        return datasetCountByNormalizedProject
            .Where(entry => !documentedProjectNames.Contains(entry.Key))
            .Select(entry => new OrphanedProject(entry.Key, entry.Value))
            .OrderByDescending(orphan => orphan.DatasetCount)
            .ThenBy(orphan => orphan.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static CompletenessSummary BuildCompletenessSummary(IReadOnlyList<Metadata> metadatas)
    {
        if (metadatas.Count == 0)
        {
            return CompletenessSummary.Empty;
        }

        List<(Metadata Metadata, int Percent)> scored = metadatas
            .Select(metadata => (Metadata: metadata, Percent: MetadataCompletenessCalculator.Calculate(metadata).Percent))
            .ToList();

        int averagePercent = (int)Math.Round(scored.Average(item => item.Percent), MidpointRounding.AwayFromZero);

        List<LowCompletenessDataset> lowestScoring = scored
            .OrderBy(item => item.Percent)
            .ThenBy(item => item.Metadata.Dataset?.Name, StringComparer.OrdinalIgnoreCase)
            .Take(LowestCompletenessLimit)
            .Select(item => new LowCompletenessDataset(
                item.Metadata.Dataset?.Name ?? string.Empty,
                item.Metadata.Dataset?.Zone ?? string.Empty,
                item.Metadata.Dataset?.Project ?? string.Empty,
                item.Percent))
            .ToList();

        return new CompletenessSummary(averagePercent, lowestScoring);
    }

    private static ValidationHealthSummary BuildValidationHealth(
        IEnumerable<Metadata> metadatas,
        Configuration configuration)
    {
        int clean = 0;
        int withWarnings = 0;
        int withErrors = 0;

        foreach (Metadata metadata in metadatas)
        {
            var result = Validator.Validate(metadata, configuration);

            if (result.HasErrors)
            {
                withErrors++;
            }
            else if (result.HasWarnings)
            {
                withWarnings++;
            }
            else
            {
                clean++;
            }
        }

        return new ValidationHealthSummary(clean, withWarnings, withErrors);
    }

    private static IReadOnlyList<KeyCount> BuildDatasetsPerProject(
        IReadOnlyDictionary<string, int> datasetCountByNormalizedProject)
    {
        return datasetCountByNormalizedProject
            .Select(entry => new KeyCount(entry.Key, entry.Value))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Take(DatasetsPerProjectLimit)
            .ToArray();
    }

    private static ProjectCoverageSummary BuildProjectCoverage(
        IReadOnlyList<Project> projects,
        IReadOnlyDictionary<string, int> datasetCountByNormalizedProject)
    {
        if (projects.Count == 0)
        {
            return ProjectCoverageSummary.Empty;
        }

        int DatasetCountFor(Project project) =>
            datasetCountByNormalizedProject.TryGetValue(Normalize(project.Name), out int count) ? count : 0;

        int projectsWithDatasets = projects.Count(project => DatasetCountFor(project) > 0);
        int projectsWithoutDatasets = projects.Count - projectsWithDatasets;

        List<StatusCount> weightedStatusCounts = projects
            .GroupBy(project => Normalize(project.ProjectStatus), StringComparer.OrdinalIgnoreCase)
            .Select(group => new StatusCount(
                group.First().ProjectStatus ?? string.Empty,
                group.Sum(DatasetCountFor)))
            .OrderByDescending(status => status.Count)
            .ToList();

        return new ProjectCoverageSummary(projectsWithDatasets, projectsWithoutDatasets, weightedStatusCounts);
    }

    private static IReadOnlyList<TemporalCoverageYear> BuildTemporalCoverage(IEnumerable<Metadata> metadatas)
    {        Dictionary<int, int> countByYear = new();

        foreach (Metadata metadata in metadatas)
        {
            string? temporalExtent = metadata.Dataset?.TemporalExtent;

            if (TemporalExtentValidator.Validate(temporalExtent, out DateTimeOffset? start, out DateTimeOffset? end)
                != TemporalExtentStatus.Valid)
            {
                continue;
            }

            if (start is null && end is null)
            {
                continue;
            }

            int startYear = (start ?? end!.Value).Year;
            int endYear = (end ?? start!.Value).Year;

            for (int year = startYear; year <= endYear; year++)
            {
                countByYear[year] = countByYear.GetValueOrDefault(year) + 1;
            }
        }

        return countByYear
            .OrderBy(entry => entry.Key)
            .Select(entry => new TemporalCoverageYear(entry.Key, entry.Value))
            .ToArray();
    }

    /// <summary>
    /// Collapses each dataset's GeoJSON geometry to its bounding box for the catalog-wide
    /// coverage map. One pass over each geometry's coordinates, so cost scales with total vertex
    /// count once here rather than with rendered vertices in the browser on every paint.
    /// Unparseable or empty geometries are skipped rather than throwing.
    /// </summary>
    private static SpatialCoverageSummary BuildSpatialCoverage(IEnumerable<Metadata> metadatas)
    {
        List<SpatialBoundingBox> boxes = new();
        int totalWithGeometry = 0;

        foreach (Metadata metadata in metadatas)
        {
            string? geometry = metadata.Dataset?.Geometry;

            if (string.IsNullOrWhiteSpace(geometry))
            {
                continue;
            }

            SpatialBoundingBox? box = TryGetBoundingBox(geometry);

            if (box is null)
            {
                continue;
            }

            totalWithGeometry++;

            if (boxes.Count < SpatialCoverageLimit)
            {
                boxes.Add(box);
            }
        }

        return boxes.Count == 0
            ? SpatialCoverageSummary.Empty
            : new SpatialCoverageSummary(boxes, boxes.Count, totalWithGeometry);
    }

    /// <summary>
    /// Walks every coordinate pair in a GeoJSON geometry, tracking min/max longitude and
    /// latitude. Works for any geometry type because GeoJSON nests coordinates uniformly:
    /// the leaves are always [lon, lat] arrays.
    /// </summary>
    private static SpatialBoundingBox? TryGetBoundingBox(string geometry)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(geometry);

            if (!document.RootElement.TryGetProperty("coordinates", out JsonElement coordinates))
            {
                return null;
            }

            double west = double.MaxValue;
            double south = double.MaxValue;
            double east = double.MinValue;
            double north = double.MinValue;
            bool any = false;

            void Visit(JsonElement element)
            {
                if (element.ValueKind != JsonValueKind.Array)
                {
                    return;
                }

                // A coordinate leaf is [lon, lat, ...]; anything else is a nesting level.
                if (element.GetArrayLength() >= 2
                    && element[0].ValueKind == JsonValueKind.Number
                    && element[1].ValueKind == JsonValueKind.Number)
                {
                    double longitude = element[0].GetDouble();
                    double latitude = element[1].GetDouble();

                    west = Math.Min(west, longitude);
                    east = Math.Max(east, longitude);
                    south = Math.Min(south, latitude);
                    north = Math.Max(north, latitude);
                    any = true;
                    return;
                }

                foreach (JsonElement child in element.EnumerateArray())
                {
                    Visit(child);
                }
            }

            Visit(coordinates);

            if (!any)
            {
                return null;
            }

            // A zero-area envelope cannot be drawn as a rectangle. Rather than inventing a
            // buffer, flag it so the map can render a marker at the location instead. The
            // tolerance absorbs float noise and sub-metre extents that would render as a hairline.
            bool isPoint = (east - west) < DegenerateExtentTolerance
                && (north - south) < DegenerateExtentTolerance;

            return new SpatialBoundingBox(west, south, east, north, isPoint);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}