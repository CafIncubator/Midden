using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caf.Midden.Core.Models.v0_2;

namespace Caf.Midden.Core.Services
{
    public class ProjectReader
    {
        private readonly IParseProjects parser;

        public ProjectReader(
            IParseProjects parser)
        {
            this.parser = parser;
        }

        public Project ReadAsync(
            Stream stream)
        {
            Project project;
            using (var sr = new StreamReader(stream, Encoding.UTF8))
            {
                project = parser.Parse(sr);
            }

            return project;
        }
    }
}
