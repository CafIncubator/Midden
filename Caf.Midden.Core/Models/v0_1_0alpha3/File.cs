using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Caf.Midden.Core.Models.v0_1_0alpha3
{
    public class File
    {
        [JsonPropertyName("schema-version")]
        [Required]
        public string SchemaVersion { get; set; }

        [JsonPropertyName("creation-date")]
        [Required]
        public string CreationDate { get; set; }
    }
}
