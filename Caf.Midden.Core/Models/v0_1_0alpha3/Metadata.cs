using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Caf.Midden.Core.Models.v0_1_0alpha3
{
    public class Metadata
    {
        [JsonPropertyName("file")]
        [Required]
        public File File { get; set; }
        
        [JsonPropertyName("dataset")]
        [Required]
        public Dataset Dataset { get; set; }
    }
}
