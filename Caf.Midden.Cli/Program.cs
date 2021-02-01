using Caf.Midden.Cli.Actions;
using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Models;
using Caf.Midden.Cli.Services;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Caf.Midden.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            ConfigurationService configReader = new ConfigurationService();
            CliConfiguration? config = configReader.GetConfiguration();

            if(config == null)
            {
                Console.WriteLine("Unable to read 'configuration.json' file. To create a configuration file, use the 'setup' action");
            }

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
