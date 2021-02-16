using Caf.Midden.Core.Models.v0_1_0alpha4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Services
{
    public class CatalogReaderHttp : IReadCatalog
    {
        private readonly HttpClient client;        

        public CatalogReaderHttp(
            HttpClient client)
        {
            this.client = client;
        }

        public async Task<Catalog> Read(string path)
        {
            Catalog result = 
                await client
                    .GetFromJsonAsync<Catalog>(
                        path);

            return result;
        }
    }
}
