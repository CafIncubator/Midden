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
            var cmd = new RootCommand
            {
                new Collate(
                    "collate",
                    "Create a Midden catalog file from one or more data stores.",
                    new Configuration())
            };

            return cmd.Invoke(args);
        }
    }
}
