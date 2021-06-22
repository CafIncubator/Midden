using Caf.Midden.Core.Models.v0_1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Services
{
    public interface IReadCatalog
    {
        Task<Catalog> Read(string path, bool noCache);
    }
}
