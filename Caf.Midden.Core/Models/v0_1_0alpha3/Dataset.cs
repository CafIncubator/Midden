using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Models.v0_1_0alpha3
{
    public class Dataset
    {
        [JsonPropertyName("zone")]
        [Required]
        public Zones Zone { get; set; }

        [JsonPropertyName("project")]
        [Required]
        public string? Project { get; set; }

        [JsonPropertyName("name")]
        [Required]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        [Required]
        public string? Description { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("filePathTemplate")]
        public string? FilePathTemplate { get; set; }

        // TODO: Change to list of key/value pairs?
        [JsonPropertyName("filePathDescriptor")]
        public string? FilePathDescriptor { get; set; }

        // TODO: Change to enum?
        [JsonPropertyName("structure")]
        public string? Structure { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        [JsonPropertyName("contacts")]
        public List<Person> Contacts { get; set; }

        [JsonPropertyName("areaOfInterest")]
        public AreaOfInterests AreaOfInterest { get; set; }

        /// <summary>
        /// "geometry" value of a geojson document; should include "type" and "coordinates"
        /// </summary>
        [JsonPropertyName("geometry")]
        public string? Geometry { get; set; }

        [JsonPropertyName("methods")]
        public List<string> Methods { get; set; }

        [JsonPropertyName("temporalResolution")]
        public string? TemporalResolution { get; set; }

        [JsonPropertyName("startDate")]
        public string? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public string? EndDate { get; set; }

        [JsonPropertyName("spatialRepeats")]
        public int? SpatialRepeats { get; set; }

        [JsonPropertyName("variables")]
        public List<Variable> Variables { get; set; }

        public Dataset()
        {
            this.Tags = new List<string>();
            this.Contacts = new List<Person>();
            this.Methods = new List<string>();
            this.Variables = new List<Variable>();
        }
    }
}
