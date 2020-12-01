using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Caf.Midden.Core.Models.v0_1_0alpha3
{
    public class QCApplied : ICloneable
    {
        [JsonPropertyName("assurance")]
        public bool Assurance { get; set; }
        
        [JsonPropertyName("point")]
        public bool Point { get; set; }

        [JsonPropertyName("observation")]
        public bool Observation { get; set; }

        [JsonPropertyName("dataset")]
        public bool Dataset { get; set; }

        [JsonPropertyName("external")]
        public bool External { get; set; }

        [JsonPropertyName("review")]
        public bool Review { get; set; }

        public override string ToString()
        {
            List<string> s = new List<string>();

            if (Assurance) s.Add("Assurance");
            if (Point) s.Add("Point");
            if (Observation) s.Add("Observation");
            if (Dataset) s.Add("Dataset");
            if (External) s.Add("External");
            if (Review) s.Add("Review");

            if (s.Count == 0) s.Add("None");

            return string.Join(", ", s.ToArray());
        }

        public object Clone()
        {
            QCApplied qcApplied = new QCApplied()
            {
                Assurance = this.Assurance,
                Point = this.Point,
                Observation = this.Observation,
                Dataset = this.Dataset,
                External = this.External,
                Review = this.Review
            };

            return qcApplied;
        }
    }
}
