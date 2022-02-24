using Caf.Midden.Cli.Common;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Cli.Services
{
    public class LocalFileSystemCrawler: ICrawl
    {
        private const string MIDDEN_FILE_EXTENSION = ".midden";
        private const string MIPPEN_FILE_EXTENSION = ".mippen";

        private readonly string rootDirectory;
        public LocalFileSystemCrawler(string rootDirectory)
        {
            if (string.IsNullOrEmpty(rootDirectory))
                throw new ArgumentNullException("Directory not specified");

            if(!Directory.Exists(rootDirectory))
                throw new ArgumentNullException("Directory does not exist");

            this.rootDirectory = rootDirectory;
        }
        public List<string> GetFileNames(string fileExtension)
        {
            string[] files = Directory.GetFiles(
                rootDirectory, 
                $"*{fileExtension}", 
                SearchOption.AllDirectories);

            Console.WriteLine($"Found a total of {files.Length} files");

            return files.ToList();
        }


        public List<Metadata> GetMetadatas()
        {
            var files = GetFileNames(MIDDEN_FILE_EXTENSION);

            List<Metadata> metadatas = new List<Metadata>();

            MetadataParser parser =
                new MetadataParser(
                    new MetadataConverter());

            foreach (var file in files)
            {
                string json = File.ReadAllText(file);

                Metadata metadata = parser.Parse(json);

                string relativePath = Path.GetRelativePath(this.rootDirectory, file);

                metadata.Dataset.DatasetPath = relativePath.Replace(MIDDEN_FILE_EXTENSION, "");

                metadatas.Add(metadata);
            }

            return metadatas;
        }

        public List<Project> GetProjects()
        {
            var files = GetFileNames(MIPPEN_FILE_EXTENSION);

            List<Project> projects = new List<Project>();

            foreach (var file in files)
            {
                string fileString = File.ReadAllText(file);

                Project project = new Project()
                {
                    Name = Path.GetFileName(file).Replace(MIPPEN_FILE_EXTENSION, ""),
                    Description = fileString
                };

                projects.Add(project);
            }

            return projects;
        }
    }
}
