using Caf.Midden.Cli.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Caf.Midden.Cli.Services
{
    public class ConfigurationService
    {
        const string CONFIG_FILE = "configuration.json";
        JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter()
            },
            WriteIndented = true
        };
        
        public Configuration? GetConfiguration()
        {
            if (!File.Exists(CONFIG_FILE))
                return null;

            string json = File.ReadAllText(CONFIG_FILE);
            Configuration? config = 
                JsonSerializer.Deserialize<Configuration>(json, jsonOptions);

            return config;
        }

        public void CreateConfiguration()
        {
            Configuration config = new Configuration()
            {
                DataStores = new List<DataStore>()
                {
                    new DataStore()
                    {
                        Name = "DataStoreName",
                        Type = DataStoreTypes.LocalFileSystem,
                        LocalPath = @"C:\Path\To\Projects"
                    }
                }
            };

            File.WriteAllText(
                CONFIG_FILE,
                JsonSerializer.Serialize(config, jsonOptions));
        }
    }
}
