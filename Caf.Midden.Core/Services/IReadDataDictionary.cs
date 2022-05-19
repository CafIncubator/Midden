using Caf.Midden.Core.Models.v0_2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Services
{
    public interface IReadDataDictionary
    {
        public List<Variable> Read(string path);
        public List<Variable> Read(Stream stream);

    }
}
