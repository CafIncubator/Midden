using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Services.Configuration
{
    public interface IReadConfiguration
    {
        Task<Models.v0_2.Configuration> Read();
    }
}
