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
        [JsonPropertyName("organizationName")]
        public string OrganizationName { get; set; }

        [JsonPropertyName("zones")]
        public List<string> Zones { get; set; }

        [JsonPropertyName("processingLevels")]
        public List<string> ProcessingLevels { get; set; }
    }
}
