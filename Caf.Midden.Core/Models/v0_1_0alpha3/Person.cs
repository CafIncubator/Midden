using System.Text.Json.Serialization;

namespace Caf.Midden.Core.Models.v0_1_0alpha3
{
    public class Person
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }
    }
}
