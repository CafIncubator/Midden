using Caf.Midden.Cli.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Caf.Midden.Cli.Services;

public sealed class ConfigurationService
{
    public const string ConfigFileName = "configuration.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter(),
        },
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public CliConfiguration? GetConfiguration(string? configPath = null)
    {
        foreach (var candidatePath in GetCandidateConfigurationPaths(configPath))
        {
            if (!File.Exists(candidatePath))
            {
                continue;
            }

            var json = File.ReadAllText(candidatePath);
            return JsonSerializer.Deserialize<CliConfiguration>(json, JsonOptions)
                ?? throw new InvalidDataException($"Configuration file '{candidatePath}' is empty or invalid.");
        }

        return null;
    }

    public string CreateConfiguration(string? configPath = null)
    {
        var resolvedPath = ResolveConfigurationPath(configPath);
        var directory = Path.GetDirectoryName(resolvedPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = new FileStream(resolvedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, CreateDefaultConfiguration(), JsonOptions);

        return resolvedPath;
    }

    private static CliConfiguration CreateDefaultConfiguration() => new()
    {
        DataStores =
        [
            new DataStore
            {
                Name = "DataStoreName",
                Type = DataStoreTypes.LocalFileSystem,
                Path = @"C:\Path\To\Projects",
            },
        ],
    };

    private static IEnumerable<string> GetCandidateConfigurationPaths(string? configPath)
    {
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            yield return Path.GetFullPath(configPath);
            yield break;
        }

        var currentDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName);
        yield return currentDirectoryPath;

        var appBasePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
        if (!string.Equals(currentDirectoryPath, appBasePath, StringComparison.OrdinalIgnoreCase))
        {
            yield return appBasePath;
        }
    }

    private static string ResolveConfigurationPath(string? configPath) =>
        Path.GetFullPath(string.IsNullOrWhiteSpace(configPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName)
            : configPath);
}