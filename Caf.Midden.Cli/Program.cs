using Caf.Midden.Cli.Actions;
using Caf.Midden.Cli.Services;
using System.CommandLine;

var configurationService = new ConfigurationService();

var rootCommand = new RootCommand("Create Midden catalogs from one or more supported data stores.");
rootCommand.Add(CollateCommand.Create(configurationService));
rootCommand.Add(ValidateCommand.Create());
rootCommand.Add(SetupCommand.Create(configurationService));
rootCommand.Add(SecretCommand.Create(configurationService));
rootCommand.Add(LoginCommand.Create(configurationService));

return await rootCommand.Parse(args).InvokeAsync();
