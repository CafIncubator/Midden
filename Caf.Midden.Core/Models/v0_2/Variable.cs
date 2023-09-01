using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Models.v0_2
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
        public List<string>? Tags { get; set; } = new List<string>();

        [JsonPropertyName("methods")]
        public List<string>? Methods { get; set; } = new List<string>();

        [JsonPropertyName("temporalResolution")]
        public string? TemporalResolution { get; set; }

        [JsonPropertyName("temporalExtent")]
        public string? TemporalExtent { get; set; }

        [JsonPropertyName("spatialRepeats")]
        public int? SpatialRepeats { get; set; }

        [JsonPropertyName("isQCSpecified")]
        public bool? IsQCSpecified { get; set; }

        [JsonPropertyName("qcApplied")]
        public List<string>? QCApplied { get; set; } = new List<string>();

        [JsonPropertyName("processingLevel")]
        public string? ProcessingLevel { get; set; }

        [JsonPropertyName("variableType")]
        public string? VariableType { get; set; }

        public Variable ShallowCopy()
        {
            return (Variable)this.MemberwiseClone();
        }

        public Variable DeepCopy()
        {
            Variable other = (Variable)this.MemberwiseClone();

            if(this.Tags != null)
                other.Tags = new List<string>(this.Tags);

            if (this.Methods != null)
                other.Methods = new List<string>(this.Methods);

            if (this.QCApplied != null)
                other.QCApplied = new List<string>(this.QCApplied);

            return other;
        }
    }
}
