using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Models.v0_1_0alpha4
{
    public class Variable
    {
        [JsonPropertyName("name")]
        [Required]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        [Required]
        public string? Description { get; set; }

        [JsonPropertyName("units")]
        [Required]
        public string? Units { get; set; }

        [JsonPropertyName("height")]
        public double? Height { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("methods")]
        public List<string>? Methods { get; set; }

        [JsonPropertyName("temporalResolution")]
        public string? TemporalResolution { get; set; }

        [JsonPropertyName("temporalExtent")]
        public string? TemporalExtent { get; set; }

        [JsonPropertyName("spatialRepeats")]
        public int? SpatialRepeats { get; set; }

        [JsonPropertyName("isQCSpecified")]
        public bool IsQCSpecified { get; set; }

        [JsonPropertyName("qcApplied")]
        public List<string>? QCApplied { get; set; }

        [JsonPropertyName("processingLevel")]
        public string? ProcessingLevel { get; set; }
    }
}
