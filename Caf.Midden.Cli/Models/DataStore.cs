﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Cli.Models
{
    public class DataStore
    {
        public string Name { get; set; }
        public DataStoreTypes Type { get; set; }
        public string? LocalPath { get; set; }
    }
}