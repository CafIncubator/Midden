using Caf.Midden.Core.Models.v0_2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Services
{
    public class ProjectParser : IParseProjects
    {
        private const string PROJECT_VAR_NAME = "project";
        private const string PROJECT_VAR_LAST_MODIFIED = "lastModified";
        private const string PROEJCT_VAR_STATUS = "status";

        public Models.v0_2.Project Parse(string contents)
        {
            throw new NotImplementedException();
        }

        public Models.v0_2.Project Parse(StreamReader sr)
        {
            // Return null if not front-matter
            if (sr.ReadLine() != "---")
                return null;

            // Read through front-matter and try to find project name
            string projectName = "";
            DateTime? lastModified = DateTime.MinValue;
            string projectStatus = "";

            string? line;
            while((line = sr.ReadLine()) != null)
            {
                if (line == "---") break;
                if (line.StartsWith(PROJECT_VAR_NAME + ":"))
                {
                    projectName = ParseFrontMatter(line);
                }
                if (line.StartsWith(PROJECT_VAR_LAST_MODIFIED + ":"))
                {
                    string modifiedDateTime = ParseFrontMatter(line);
                    lastModified = DateTime.Parse(modifiedDateTime);
                }
                if (line.StartsWith(PROEJCT_VAR_STATUS + ":"))
                {
                    projectStatus = ParseFrontMatter(line);
                }
            }

            // Return null if failed to find project name
            if (string.IsNullOrWhiteSpace(projectName))
                return null;

            // Found a project name, so create a project and get the contents
            Models.v0_2.Project project = new Models.v0_2.Project();
            project.Name = projectName;
            if(lastModified != DateTime.MinValue) project.LastModified = lastModified;
            project.ProjectStatus = projectStatus;
            project.Description = sr.ReadToEnd();

            return project;
        }

        private string ParseFrontMatter(string line)
        {
            Regex regex = new Regex("\"(.*?)\"");

            var matches = regex.Matches(line);

            if(matches.Count > 0)
            {
                return matches[0].Groups[1].Value.Trim('"');
            }
            else { return null; }
        }
    }
}
