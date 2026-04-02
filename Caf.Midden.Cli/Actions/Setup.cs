using Caf.Midden.Cli.Services;
using System.CommandLine;

namespace Caf.Midden.Cli.Actions;

public static class SetupCommand
{
    public static Command Create(ConfigurationService configurationService)
    {
        var command = new Command("setup", "Create a blank configuration.json file in the current directory.");
        command.SetAction(_ => HandleSetup(configurationService));
        return command;
    }

    private static int HandleSetup(ConfigurationService configurationService)
    {
        try
        {
            var configurationPath = configurationService.CreateConfiguration();
            Console.WriteLine($"Created configuration template at {configurationPath}");
            return 0;
        }
        catch (IOException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }
}
