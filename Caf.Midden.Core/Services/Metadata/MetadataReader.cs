using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Services.Metadata
{
    public class MetadataReader
    {
        private readonly IMetadataParser parser;

        public MetadataReader(
            IMetadataParser parser)
        {
            this.parser = parser;
        }

        public Models.v0_2.Metadata Read(
            string fileString)
        {
            if (fileString.Length == 0)
                throw new ArgumentNullException("No data in string");

            var result = this.parser.Parse(fileString);

            return result;
        }

        public async Task<Models.v0_2.Metadata> ReadAsync(
            System.IO.Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string fileString = await reader.ReadToEndAsync();

                var result = this.Read(fileString);

                return result;
            }
        }
    }
}
