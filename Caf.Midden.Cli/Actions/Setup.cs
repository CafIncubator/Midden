using Caf.Midden.Cli.Services;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Cli.Actions
{
    public class Setup : Command
    {
        public Setup(
            string name,
            string description)
            : base(name, description)
        {
            Handler = CommandHandler.Create(HandleSetup);
        }

        public void HandleSetup()
        {
            ConfigurationService configService = new ConfigurationService();
            configService.CreateConfiguration();
        }
    }
}
