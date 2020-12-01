using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Models.v0_1_0alpha4
{
    public class Metadata
    {
        [JsonPropertyName("schemaVersion")]
        [Required]
        public string SchemaVersion { get; set; }

        [JsonPropertyName("creationDate")]
        [Required]
        public DateTime CreationDate { get; set; }

        [JsonPropertyName("modifiedDate")]
        [Required]
        public DateTime ModifiedDate { get; set; }

        [JsonPropertyName("dataset")]
        [Required]
        public Dataset Dataset { get; set; }
    }
}
