using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Caf.Midden.Core.Models.v0_2
{
    public class Metadata
    {
        [JsonPropertyName("schemaVersion")]
        [Required]
        public string SchemaVersion { get; private set; }

        [JsonPropertyName("creationDate")]
        [Required]
        public DateTime CreationDate { get; set; }

        [JsonPropertyName("modifiedDate")]
        [Required]
        public DateTime ModifiedDate { get; set; }

        [JsonPropertyName("dataset")]
        [Required]
        public Dataset Dataset { get; set; } = new Dataset();

        public Metadata()
        {
            this.SchemaVersion = "v0.2";
        }
    }
}
