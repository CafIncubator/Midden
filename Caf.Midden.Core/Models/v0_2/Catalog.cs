using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Models.v0_2
{
    public class Catalog
    {
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; private set; }

        [JsonPropertyName("creationDate")]
        public DateTime CreationDate { get; set; }

        [JsonPropertyName("projects")]
        public List<Project> Projects { get; set; } = new List<Project>();

        [JsonPropertyName("metadatas")]
        public List<Metadata> Metadatas { get; set; } = new List<Metadata>();

        public Catalog()
        {
            this.SchemaVersion = "v0.2";
        }
    }
}
