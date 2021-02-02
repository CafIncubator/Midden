using Caf.Midden.Cli.Actions;
using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Services;
using System.CommandLine;

namespace Caf.Midden.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            ConfigurationService configReader = new ConfigurationService();
            CliConfiguration? config = configReader.GetConfiguration();

            var cmd = new RootCommand
            {
                new Collate(
                    "collate",
                    "Create a Midden catalog file from one or more data stores.",
                    config),
                new Setup(
                    "setup",
                    "Creates a blank 'configuration.json' file")
            };

            return cmd.Invoke(args);
        }
    }
}
