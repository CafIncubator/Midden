using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Security;
using Caf.Midden.Cli.Services;
using System.CommandLine;

namespace Caf.Midden.Cli.Actions;

/// <summary>
/// Performs Google sign in up front and caches the resulting token, so that 'collate' never has to
/// open a browser part way through a crawl.
/// </summary>
public static class LoginCommand
{
    public static Command Create(ConfigurationService configurationService)
    {
        var datastoreArgument = new Argument<string>("datastore")
        {
            Description = "Name of the Google data store in configuration.json to sign in for.",
        };

        var configOption = new Option<string?>("--config", ["-c"])
        {
            Description = "Path to the configuration file. The token cache is kept beside it.",
        };

        var googleCommand = new Command("google", "Sign in to Google Drive and cache the credentials.");
        googleCommand.Add(datastoreArgument);
        googleCommand.Add(configOption);
        googleCommand.SetAction(parseResult => HandleGoogleLogin(
            configurationService,
            parseResult.GetValue(datastoreArgument)!,
            parseResult.GetValue(configOption)));

        var command = new Command("login", "Sign in to a data store provider and cache the credentials.");
        command.Add(googleCommand);

        return command;
    }

    private static int HandleGoogleLogin(
        ConfigurationService configurationService,
        string datastoreName,
        string? configPath)
    {
        CliConfiguration? configuration;

        try
        {
            configuration = configurationService.GetConfiguration(configPath);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Unable to read '{ConfigurationService.ConfigFileName}': {exception.Message}");
            return 1;
        }

        if (configuration is null)
        {
            Console.Error.WriteLine("Unable to find 'configuration.json'. Run the 'setup' command to create one in the current directory.");
            return 1;
        }

        var dataStore = configuration.DataStores.FirstOrDefault(
            store => string.Equals(store.Name, datastoreName, StringComparison.OrdinalIgnoreCase));

        if (dataStore is null)
        {
            Console.Error.WriteLine($"No data store with name '{datastoreName}' exists in the configuration file.");
            return 1;
        }

        if (dataStore.Type is not (DataStoreTypes.GoogleDrive or DataStoreTypes.GoogleWorkspaceSharedDrive))
        {
            Console.Error.WriteLine($"Data store '{dataStore.Name}' is of type '{dataStore.Type}', which does not use Google sign in.");
            return 1;
        }

        if (!string.IsNullOrWhiteSpace(dataStore.AuthFilePath))
        {
            Console.WriteLine(
                $"Data store '{dataStore.Name}' uses a service account key, which needs no interactive sign in.");
            return 0;
        }

        var tokenStorePath = configurationService.GetGoogleTokenStorePath(configPath);

        try
        {
            using var secretResolver = new SecretResolver(
                configurationService.GetSecretStorePath(configPath),
                () => ConsolePrompt.ReadStorePassword());

            var clientSecret = secretResolver.Resolve(dataStore.ClientSecret).Value;

            if (string.IsNullOrWhiteSpace(dataStore.ClientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                Console.Error.WriteLine(
                    $"Data store '{dataStore.Name}' needs both 'ClientId' and 'ClientSecret' to sign in to Google.");
                return 1;
            }

            Console.WriteLine("Opening a browser to sign in to Google...");

            GoogleCredentialFactory.Authorize(
                dataStore.ClientId,
                clientSecret,
                tokenStorePath,
                allowInteractive: true);

            Console.WriteLine($"Signed in. Credentials cached in {tokenStorePath}");
            Console.WriteLine("'collate' will now reuse these credentials without prompting.");

            return 0;
        }
        catch (Exception exception) when (exception is InvalidOperationException or InvalidDataException or PlatformNotSupportedException)
        {
            Console.Error.WriteLine($"Google sign in failed: {exception.Message}");
            return 1;
        }
    }
}
