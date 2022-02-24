using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Models.v0_2
{
    public class Geometry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("geojson")]
        public string GeoJson { get; set; }
    }
}
