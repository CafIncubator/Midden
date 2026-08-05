using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Security;
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
        // Fail loudly on misspelled property names. Previously a typo such as "ClientSecrets"
        // deserialized to null and the data store was silently skipped as "not enough configuration".
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
    };

    /// <summary>
    /// Returns the path of the configuration file that would be loaded, or null when none exists.
    /// </summary>
    public string? FindConfigurationPath(string? configPath = null) =>
        GetCandidateConfigurationPaths(configPath).FirstOrDefault(File.Exists);

    public CliConfiguration? GetConfiguration(string? configPath = null)
    {
        var resolvedPath = FindConfigurationPath(configPath);

        if (resolvedPath is null)
        {
            return null;
        }

        CliConfiguration configuration;

        try
        {
            configuration = JsonSerializer.Deserialize<CliConfiguration>(File.ReadAllText(resolvedPath), JsonOptions)
                ?? throw new InvalidDataException($"Configuration file '{resolvedPath}' is empty or invalid.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Configuration file '{resolvedPath}' could not be parsed (line {exception.LineNumber}, position {exception.BytePositionInLine}): {exception.Message}",
                exception);
        }

        // A file with no Version predates schema versioning; treat it as the current schema so
        // existing installations keep working. An explicit but unknown version is an error.
        if (!string.IsNullOrWhiteSpace(configuration.Version) && !CliConfigurationVersions.IsSupported(configuration.Version))
        {
            throw new InvalidDataException(
                $"Configuration file '{resolvedPath}' declares schema version '{configuration.Version}', which this version of the CLI does not support. Supported version: '{CliConfigurationVersions.Current}'.");
        }

        var duplicateNames = configuration.DataStores
            .GroupBy(dataStore => dataStore.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateNames.Count > 0)
        {
            throw new InvalidDataException(
                $"Configuration file '{resolvedPath}' defines more than one data store with the same name: {string.Join(", ", duplicateNames)}. Data store names must be unique.");
        }

        return configuration;
    }

    public string CreateConfiguration(string? configPath = null)
    {
        var resolvedPath = ResolveConfigurationPath(configPath);
        var directory = Path.GetDirectoryName(resolvedPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using (var stream = new FileStream(resolvedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, CreateDefaultConfiguration(), JsonOptions);
        }

        // configuration.json can end up holding plain-text literal secrets (see D2/D3), so it
        // gets the same restrictive permissions as secrets.midden on non-Windows platforms.
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(resolvedPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        return resolvedPath;
    }

    private static CliConfiguration CreateDefaultConfiguration() => new()
    {
        Version = CliConfigurationVersions.Current,
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

    /// <summary>
    /// Returns the directory that owns the configuration file. Companion files such as the secret
    /// store and the Google token cache are kept here so they travel with the configuration.
    /// </summary>
    public string GetConfigurationDirectory(string? configPath = null)
    {
        var configurationPath = FindConfigurationPath(configPath) ?? ResolveConfigurationPath(configPath);
        var directory = Path.GetDirectoryName(configurationPath);

        return string.IsNullOrWhiteSpace(directory) ? Directory.GetCurrentDirectory() : directory;
    }

    /// <summary>
    /// Returns the path of the encrypted secret store that pairs with the configuration file.
    /// The store lives beside the configuration so the two travel together.
    /// </summary>
    public string GetSecretStorePath(string? configPath = null) =>
        SecretStore.GetDefaultPath(GetConfigurationDirectory(configPath));

    /// <summary>
    /// Returns the path of the Google OAuth token cache that pairs with the configuration file.
    /// </summary>
    public string GetGoogleTokenStorePath(string? configPath = null) =>
        GoogleCredentialFactory.GetTokenStorePath(GetConfigurationDirectory(configPath));

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