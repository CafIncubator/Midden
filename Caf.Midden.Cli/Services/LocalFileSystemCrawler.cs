using Caf.Midden.Cli.Common;
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
        private readonly string rootDirectory;
        public LocalFileSystemCrawler(string rootDirectory)
        {
            if (string.IsNullOrEmpty(rootDirectory))
                throw new ArgumentNullException("Directory not specified");

            if(!Directory.Exists(rootDirectory))
                throw new ArgumentNullException("Directory does nto exist");

            this.rootDirectory = rootDirectory;
        }
        public List<string> GetFileNames()
        {
            string[] files = Directory.GetFiles(
                rootDirectory, 
                "*.midden", 
                SearchOption.AllDirectories);

            return files.ToList();
        }
    }
}
