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

        public Project Read(
            Stream stream)
        {
            Project project;
            using (var sr = new StreamReader(stream, Encoding.UTF8))
            {
                project = parser.Parse(sr);
            }

            return project;
        }

        public Project Read(
            string fileString)
        {
            Project project;
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(fileString));
            using(var sr = new StreamReader(ms, Encoding.UTF8))
            {
                project = parser.Parse(sr);
            }
            

            return project;
        }
    }
}
