using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Services.Configuration
{
    public class ConfigurationReaderHttp : IReadConfiguration
    {
        private readonly HttpClient client;
        private readonly string jsonPath;
        

        public ConfigurationReaderHttp(
            HttpClient client,
            string jsonPath)
        {
            this.client = client;
            this.jsonPath = jsonPath;
        }

        public async Task<Models.v0_2.Configuration> Read()
        {
            Models.v0_2.Configuration result = 
                await client
                    .GetFromJsonAsync<Models.v0_2.Configuration>(
                        jsonPath);

            return result;
        }
    }
}
