using Caf.Midden.Core.Models.v0_2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Caf.Midden.Wasm.Services;

public sealed record KeyCount(string Key, int Count);

public sealed record ZoneCount(string Zone, int Count);

public sealed record StatusCount(string Status, int Count);

public sealed record TimelinePoint(DateTime Date, int Count);

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
    IReadOnlyList<TimelinePoint> DatasetGrowth)
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
        Array.Empty<TimelinePoint>());
}

public sealed class CatalogInsightsService
{
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

        return new CatalogInsightsSnapshot(
            TotalDatasets: metadatas.Count,
            TotalVariables: metadatas.SelectMany(metadata => metadata.Dataset?.Variables ?? new List<Variable>()).Count(),
            TotalTags: allTags.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            TotalContacts: contacts.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            TotalProjects: metadatas
                .Select(metadata => metadata.Dataset?.Project)
                .Where(project => !string.IsNullOrWhiteSpace(project))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            CatalogLastUpdate: catalog.CreationDate,
            TopDatasetTags: BuildTopCounts(datasetTags),
            TopVariableTags: BuildTopCounts(variableTags),
            TopTags: BuildTopCounts(allTags),
            TopContacts: BuildTopCounts(contacts),
            DatasetsByZone: BuildDatasetsByZone(metadatas, configuration),
            ProjectsByStatus: BuildProjectsByStatus(projects, configuration),
            DatasetGrowth: BuildDatasetGrowth(metadatas));
    }

    private static IReadOnlyList<KeyCount> BuildTopCounts(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new KeyCount(group.First(), group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();
    }

    private static IReadOnlyList<ZoneCount> BuildDatasetsByZone(IEnumerable<Metadata> metadatas, Configuration configuration)
    {
        string Normalize(string? value) => (value ?? string.Empty).Trim();

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
        string Normalize(string? value) => (value ?? string.Empty).Trim();

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

            points.Add(new TimelinePoint(month, runningTotal));
        }

        return points;
    }
}