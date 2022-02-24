using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Models.v0_2
{
    public class Dataset
    {
        [JsonPropertyName("zone")]
        [Required]
        public string Zone { get; set; }

        [JsonPropertyName("project")]
        [Required]
        public string Project { get; set; }

        [JsonPropertyName("name")]
        [Required]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        [Required]
        public string? Description { get; set; }

        [JsonPropertyName("datasetPath")]
        public string? DatasetPath { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("filePathTemplate")]
        public string? FilePathTemplate { get; set; }

        [JsonPropertyName("filePathDescriptor")]
        public string? FilePathDescriptor { get; set; }

        [JsonPropertyName("structure")]
        public string? Structure { get; set; }

        [JsonPropertyName("lastUpdate")]
        public DateTime? LastUpdate { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonPropertyName("contacts")]
        public List<Person> Contacts { get; set; } = new List<Person>();

        /// <summary>
        /// "geometry" value of a geojson document; should include "type" and "coordinates"
        /// </summary>
        [JsonPropertyName("geometry")]
        public string? Geometry { get; set; }

        [JsonPropertyName("methods")]
        public List<string> Methods { get; set; } = new List<string>();

        [JsonPropertyName("temporalResolution")]
        public string? TemporalResolution { get; set; }

        /// <summary>
        /// String in form of {startDate}/{endDate}, e.g. 2011-01-01/2019-10-30
        /// Note that dates/times are in ISO 8601 standard
        /// </summary>
        [JsonPropertyName("temporalExtent")]
        public string? TemporalExtent { get; set; }

        [JsonPropertyName("spatialRepeats")]
        public int? SpatialRepeats { get; set; }

        [JsonPropertyName("variables")]
        public List<Variable> Variables { get; set; } = new List<Variable>();

        [JsonPropertyName("derivedWorks")]
        public List<string>? DerivedWorks { get; set; } = new List<string>();

        [JsonPropertyName("parentDatasets")]
        public List<string>? ParentDatasets { get; set; } = new List<string>();
    }
}
