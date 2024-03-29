﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Models.v0_1_0alpha4
{
    public class Catalog
    {
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; private set; }

        [JsonPropertyName("creationDate")]
        public DateTime CreationDate { get; set; }

        [JsonPropertyName("metadatas")]
        public List<Metadata> Metadatas { get; set; } = new List<Metadata>();

        public Catalog()
        {
            this.SchemaVersion = "v0.1.0-alpha4";
        }
    }
}
