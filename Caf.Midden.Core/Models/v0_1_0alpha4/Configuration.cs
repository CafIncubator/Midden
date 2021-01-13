using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Models.v0_1_0alpha4
{
    public class Configuration
    {
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; set; }

        [JsonPropertyName("isConfigured")]
        public bool IsConfigured { get; set; }

        [JsonPropertyName("organizationName")]
        public string OrganizationName { get; set; }

        [JsonPropertyName("toolName")]
        public string ToolName { get; set; }

        [JsonPropertyName("zones")]
        public List<string> Zones { get; set; }

        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; }

        [JsonPropertyName("processingLevels")]
        public List<string> ProcessingLevels { get; set; }

        [JsonPropertyName("geometries")]
        public List<Geometry> Geometries { get; set; }
    }
}
