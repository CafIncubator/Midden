using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Services;

namespace Caf.Midden.Cli.LiveTests;

internal static class LiveTestConfiguration
{
    public static DataStore GetDataStoreOrSkip(string configPath)
    {
        Assert.SkipUnless(
            File.Exists(configPath),
            $"Live test configuration file '{configPath}' is not present.");

        var configuration = new ConfigurationService().GetConfiguration(configPath);
        var dataStore = configuration?.DataStores.FirstOrDefault();

        Assert.SkipUnless(
            dataStore is not null,
            $"Live test configuration file '{configPath}' does not define a data store.");

        return dataStore;
    }

    public static void RequireFileOrSkip(string? path, string description)
    {
        Assert.SkipUnless(
            !string.IsNullOrWhiteSpace(path) && File.Exists(path),
            $"The {description} file '{path}' is not present.");
    }
}