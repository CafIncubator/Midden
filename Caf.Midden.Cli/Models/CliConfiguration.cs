namespace Caf.Midden.Cli.Models;

public sealed class CliConfiguration
{
    /// <summary>
    /// Schema version of the configuration file. Validated on load so that a file written for a
    /// different version fails loudly instead of silently deserializing with missing properties.
    /// </summary>
    public string Version { get; init; } = CliConfigurationVersions.Current;

    public List<DataStore> DataStores { get; init; } = [];
}

public static class CliConfigurationVersions
{
    public const string Current = "1.0.0";

    private static readonly string[] Supported = [Current];

    public static bool IsSupported(string? version) =>
        version is not null && Supported.Contains(version, StringComparer.OrdinalIgnoreCase);
}
