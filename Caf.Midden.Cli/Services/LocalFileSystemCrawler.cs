using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Cli.Services
{
    public class LocalFileSystemCrawler
    {
        public List<string> GetFileNames(string rootDir)
        {
            string[] files = Directory.GetFiles(
                rootDir, 
                "*.midden", 
                SearchOption.AllDirectories);

            return files.ToList();
        }
    }
}
