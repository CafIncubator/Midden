using Caf.Midden.Core.Models.v0_2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Caf.Midden.Wasm.Services;

public enum CatalogSearchResultType
{
    Dataset,
    Project,
    Variable,
    Tag
}

public sealed record CatalogSearchResult(
    CatalogSearchResultType Type,
    string Title,
    string? Subtitle,
    string Url,
    int Rank);

public sealed record CatalogSearchResultGroup(
    CatalogSearchResultType Type,
    string Label,
    IReadOnlyList<CatalogSearchResult> Items,
    int TotalCount,
    string SeeAllUrl);

public sealed record CatalogSearchResponse(string Query, IReadOnlyList<CatalogSearchResultGroup> Groups)
{
    public static CatalogSearchResponse Empty { get; } = new(string.Empty, Array.Empty<CatalogSearchResultGroup>());

    public bool HasResults => Groups.Any(g => g.TotalCount > 0);
}

/// <summary>
/// In-memory, catalog-wide search over datasets, projects, variables, and tags. Backs the
/// Home page's universal search bar. All data is already resident in <see cref="StateContainer"/>,
/// so this is pure LINQ with no I/O.
/// </summary>
public sealed class CatalogSearchService
{
    private const int MaxResultsPerGroup = 5;

    public CatalogSearchResponse Search(Catalog? catalog, string? query, int maxPerGroup = MaxResultsPerGroup)
    {
        if (catalog == null || string.IsNullOrWhiteSpace(query))
        {
            return CatalogSearchResponse.Empty;
        }

        var trimmedQuery = query.Trim();

        var datasetResults = SearchDatasets(catalog, trimmedQuery);
        var projectResults = SearchProjects(catalog, trimmedQuery);
        var variableResults = SearchVariables(catalog, trimmedQuery);
        var tagResults = SearchTags(catalog, trimmedQuery);

        var groups = new List<CatalogSearchResultGroup>
        {
            BuildGroup(CatalogSearchResultType.Dataset, "Datasets", datasetResults, maxPerGroup, "catalog/datasets"),
            BuildGroup(CatalogSearchResultType.Project, "Projects", projectResults, maxPerGroup, "catalog/projects"),
            BuildGroup(CatalogSearchResultType.Variable, "Variables", variableResults, maxPerGroup, "catalog/variables"),
            BuildGroup(CatalogSearchResultType.Tag, "Tags", tagResults, maxPerGroup, "catalog/datasets"),
        };

        return new CatalogSearchResponse(trimmedQuery, groups);
    }

    private static CatalogSearchResultGroup BuildGroup(
        CatalogSearchResultType type,
        string label,
        List<CatalogSearchResult> allResults,
        int maxPerGroup,
        string seeAllUrl)
    {
        var ordered = allResults
            .OrderBy(r => r.Rank)
            .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new CatalogSearchResultGroup(
            type,
            label,
            ordered.Take(maxPerGroup).ToList(),
            ordered.Count,
            seeAllUrl);
    }

    private static List<CatalogSearchResult> SearchDatasets(Catalog catalog, string query)
    {
        var results = new List<CatalogSearchResult>();

        foreach (var metadata in catalog.Metadatas)
        {
            var dataset = metadata.Dataset;
            if (dataset == null)
            {
                continue;
            }

            var rank = RankMatch(query, dataset.Name, dataset.Tags, dataset.Description)
                ?? RankListMatch(query, dataset.Methods);

            if (rank == null)
            {
                continue;
            }

            var url = $"catalog/datasets/{dataset.Zone}/{dataset.Project}/{dataset.Name}";
            results.Add(new CatalogSearchResult(
                CatalogSearchResultType.Dataset,
                dataset.Name,
                $"{dataset.Zone} / {dataset.Project}",
                url,
                rank.Value));
        }

        return results;
    }

    private static List<CatalogSearchResult> SearchProjects(Catalog catalog, string query)
    {
        var results = new List<CatalogSearchResult>();

        foreach (var project in catalog.Projects)
        {
            var rank = RankMatch(query, project.Name, null, project.Description);
            if (rank == null)
            {
                continue;
            }

            var url = $"catalog/projects/{project.Name}";
            results.Add(new CatalogSearchResult(
                CatalogSearchResultType.Project,
                project.Name,
                project.ProjectStatus,
                url,
                rank.Value));
        }

        return results;
    }

    private static List<CatalogSearchResult> SearchVariables(Catalog catalog, string query)
    {
        var results = new List<CatalogSearchResult>();

        foreach (var metadata in catalog.Metadatas)
        {
            var dataset = metadata.Dataset;
            if (dataset?.Variables == null)
            {
                continue;
            }

            foreach (var variable in dataset.Variables)
            {
                if (string.IsNullOrWhiteSpace(variable.Name))
                {
                    continue;
                }

                var rank = RankMatch(query, variable.Name, variable.Tags, variable.Description)
                    ?? RankSingleMatch(query, variable.Units);

                if (rank == null)
                {
                    continue;
                }

                var url = $"catalog/datasets/{dataset.Zone}/{dataset.Project}/{dataset.Name}";
                results.Add(new CatalogSearchResult(
                    CatalogSearchResultType.Variable,
                    variable.Name!,
                    $"{dataset.Name}",
                    url,
                    rank.Value));
            }
        }

        return results;
    }

    private static List<CatalogSearchResult> SearchTags(Catalog catalog, string query)
    {
        var tagCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var metadata in catalog.Metadatas)
        {
            foreach (var tag in metadata.Dataset?.Tags ?? new List<string>())
            {
                IncrementTag(tagCounts, tag);
            }

            foreach (var variable in metadata.Dataset?.Variables ?? new List<Variable>())
            {
                foreach (var tag in variable.Tags ?? new List<string>())
                {
                    IncrementTag(tagCounts, tag);
                }
            }
        }

        var results = new List<CatalogSearchResult>();

        foreach (var (tag, count) in tagCounts)
        {
            var rank = RankSingleMatch(query, tag);
            if (rank == null)
            {
                continue;
            }

            results.Add(new CatalogSearchResult(
                CatalogSearchResultType.Tag,
                tag,
                $"{count} dataset{(count == 1 ? string.Empty : "s")}",
                $"catalog/datasets/tags/{tag}",
                rank.Value));
        }

        return results;
    }

    private static void IncrementTag(Dictionary<string, int> tagCounts, string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        tagCounts[tag] = tagCounts.TryGetValue(tag, out var count) ? count + 1 : 1;
    }

    /// <summary>
    /// Ranking: exact match &gt; name prefix &gt; name contains &gt; tag match &gt; description contains.
    /// Lower is better. Returns null when there is no match at all.
    /// </summary>
    private static int? RankMatch(string query, string? name, List<string>? tags, string? description)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            if (string.Equals(name, query, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (name.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }
        }

        if (tags != null && tags.Any(t => !string.IsNullOrWhiteSpace(t) && t.Contains(query, StringComparison.OrdinalIgnoreCase)))
        {
            return 3;
        }

        if (!string.IsNullOrWhiteSpace(description) && description.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        return null;
    }

    private static int? RankListMatch(string query, List<string>? values)
    {
        if (values != null && values.Any(v => !string.IsNullOrWhiteSpace(v) && v.Contains(query, StringComparison.OrdinalIgnoreCase)))
        {
            return 4;
        }

        return null;
    }

    private static int? RankSingleMatch(string query, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (string.Equals(value, query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (value.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (value.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return null;
    }
}
