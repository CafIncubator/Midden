using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Security;
using Caf.Midden.Cli.Services;
using System.CommandLine;

namespace Caf.Midden.Cli.Actions;

/// <summary>
/// Manages the encrypted local secret store that holds data store credentials, so that
/// 'configuration.json' can stay plain text and free of secrets.
/// </summary>
public static class SecretCommand
{
    public static Command Create(ConfigurationService configurationService)
    {
        var command = new Command("secret", "Manage credentials for data stores in an encrypted local store.");

        command.Add(CreateSetCommand(configurationService));
        command.Add(CreateListCommand(configurationService));
        command.Add(CreateRemoveCommand(configurationService));

        return command;
    }

    private static Option<string?> CreateConfigOption() => new("--config", ["-c"])
    {
        Description = "Path to the configuration file. The secret store is kept beside it.",
    };

    private static Option<bool> CreatePasswordOption() => new("--password")
    {
        Description = "Protect a new store with a password instead of the Windows Data Protection API. "
            + "Use this when the store must be readable on another machine or operating system.",
    };

    private static Command CreateSetCommand(ConfigurationService configurationService)
    {
        var nameArgument = new Argument<string>("name")
        {
            Description = "Name of the secret, referenced from configuration.json as 'secret:name'.",
        };

        var configOption = CreateConfigOption();
        var passwordOption = CreatePasswordOption();

        var command = new Command("set", "Add or replace a secret. The value is prompted for and never echoed.");
        command.Add(nameArgument);
        command.Add(configOption);
        command.Add(passwordOption);

        command.SetAction(parseResult => HandleSet(
            configurationService,
            parseResult.GetValue(nameArgument)!,
            parseResult.GetValue(configOption),
            parseResult.GetValue(passwordOption)));

        return command;
    }

    private static Command CreateListCommand(ConfigurationService configurationService)
    {
        var configOption = CreateConfigOption();

        var command = new Command("list", "List the names of stored secrets. Values are never displayed.");
        command.Add(configOption);
        command.SetAction(parseResult => HandleList(configurationService, parseResult.GetValue(configOption)));

        return command;
    }

    private static Command CreateRemoveCommand(ConfigurationService configurationService)
    {
        var nameArgument = new Argument<string>("name")
        {
            Description = "Name of the secret to remove.",
        };

        var configOption = CreateConfigOption();

        var command = new Command("remove", "Remove a secret from the store.");
        command.Add(nameArgument);
        command.Add(configOption);

        command.SetAction(parseResult => HandleRemove(
            configurationService,
            parseResult.GetValue(nameArgument)!,
            parseResult.GetValue(configOption)));

        return command;
    }

    private static int HandleSet(
        ConfigurationService configurationService,
        string name,
        string? configPath,
        bool usePassword)
    {
        var storePath = configurationService.GetSecretStorePath(configPath);
        var isNewStore = !SecretStore.Exists(storePath);
        var provider = usePassword ? SecretProtectionProvider.Password : SecretStore.DefaultProvider;

        if (isNewStore && provider == SecretProtectionProvider.Password)
        {
            Console.WriteLine("Creating a new password protected secret store.");
            Console.WriteLine("There is no way to recover this password. If it is lost, the secrets must be set again.");
        }

        Func<string> passwordProvider = isNewStore && provider == SecretProtectionProvider.Password
            ? CreateNewPasswordProvider()
            : () => ConsolePrompt.ReadStorePassword();

        try
        {
            using var store = SecretStore.Open(storePath, passwordProvider, provider);

            var value = ConsolePrompt.ReadHidden($"Value for '{name}': ");

            if (string.IsNullOrWhiteSpace(value))
            {
                Console.Error.WriteLine("No value entered. Nothing was changed.");
                return 1;
            }

            var isReplacement = store.TryGet(name, out _);
            store.Set(name, value);
            store.Save();

            Console.WriteLine($"{(isReplacement ? "Updated" : "Stored")} secret '{name}' in {storePath}");

            if (isNewStore)
            {
                Console.WriteLine(store.Provider == SecretProtectionProvider.Dpapi
                    ? "The store is protected with the Windows Data Protection API. Only your Windows account on this machine can read it."
                    : "The store is protected with your password.");
            }

            Console.WriteLine($"Reference it from configuration.json as \"{SecretResolver.SecretReferencePrefix}{name}\".");
            Console.WriteLine($"For unattended runs, set the {SecretResolver.GetEnvironmentVariableName(name)} environment variable instead.");

            return 0;
        }
        catch (Exception exception) when (exception is InvalidDataException or PlatformNotSupportedException or OperationCanceledException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int HandleList(ConfigurationService configurationService, string? configPath)
    {
        var storePath = configurationService.GetSecretStorePath(configPath);

        if (!SecretStore.Exists(storePath))
        {
            Console.WriteLine($"No secret store exists at {storePath}. Create one with 'midden secret set <name>'.");
            return 0;
        }

        try
        {
            using var store = SecretStore.Open(storePath, () => ConsolePrompt.ReadStorePassword());

            Console.WriteLine($"Secret store: {storePath}");
            Console.WriteLine($"Protection:   {store.Provider}");

            if (store.Names.Count == 0)
            {
                Console.WriteLine("The store contains no secrets.");
                return 0;
            }

            Console.WriteLine($"Secrets ({store.Names.Count}):");

            foreach (var name in store.Names.Order(StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  {name}");
            }

            return 0;
        }
        catch (Exception exception) when (exception is InvalidDataException or PlatformNotSupportedException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int HandleRemove(ConfigurationService configurationService, string name, string? configPath)
    {
        var storePath = configurationService.GetSecretStorePath(configPath);

        if (!SecretStore.Exists(storePath))
        {
            Console.Error.WriteLine($"No secret store exists at {storePath}.");
            return 1;
        }

        try
        {
            using var store = SecretStore.Open(storePath, () => ConsolePrompt.ReadStorePassword());

            if (!store.Remove(name))
            {
                Console.Error.WriteLine($"No secret named '{name}' exists in {storePath}.");
                return 1;
            }

            store.Save();
            Console.WriteLine($"Removed secret '{name}'.");

            return 0;
        }
        catch (Exception exception) when (exception is InvalidDataException or PlatformNotSupportedException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static Func<string> CreateNewPasswordProvider() => () =>
        ConsolePrompt.ReadNewPassword() ?? throw new OperationCanceledException("The secret store was not created.");
}
