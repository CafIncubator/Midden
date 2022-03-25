using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Caf.Midden.Cli.Common;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Caf.Midden.Cli.Services
{
    public class AzureFileShareCrawler : ICrawl
    {
        private const string MIDDEN_FILE_EXTENSION = ".midden";
        private const string MIPPEN_FILE_SEARCH_TERM = "DESCRIPTION.md";

        private readonly string uri;
        private readonly string path;
        private readonly AzureSasCredential sasCred;

        private readonly ShareClient shareClient;

        public AzureFileShareCrawler(
            string uri,
            string path,
            string sharedAccessSignature)
        {
            this.uri = uri;   
            this.path = path;

            this.sasCred = new AzureSasCredential(sharedAccessSignature);
            this.shareClient = InitializeClient();
        }

        private ShareClient InitializeClient()
        {
            ShareClient shareClient = new ShareClient(
                new Uri(this.uri),
                this.sasCred);

            return shareClient;
        }

        // Gets a list of midden file names
        public List<string> GetFileNames(string fileExtension)
        {
            var names = new List<string>();

            try
            {
                var remaining = new Queue<ShareDirectoryClient>();

                remaining.Enqueue(shareClient.GetDirectoryClient(this.path));
                while (remaining.Count > 0)
                {
                    ShareDirectoryClient dir = remaining.Dequeue();
                    foreach (ShareFileItem item in dir.GetFilesAndDirectories())
                    {
                        if (item.IsDirectory)
                        {
                            remaining.Enqueue(dir.GetSubdirectoryClient(item.Name));
                        }
                        else if (item.Name.Contains(fileExtension))
                        {
                            Console.WriteLine($"  In {dir.Name} found {item.Name}");

                            names.Add(item.Name);
                        }
                    }
                }

                Console.WriteLine($"Found a total of {names.Count} files");
            }
            catch(Exception e)
            {
                Console.WriteLine($"An error ocurred: {e}");
            }

            return names;
        }

        public List<Metadata> GetMetadatas(
            IMetadataParser parser)
        {
            List<Metadata> metadatas = new List<Metadata>();

            try
            {
                var remaining = new Queue<ShareDirectoryClient>();

                remaining.Enqueue(shareClient.GetDirectoryClient(this.path));
                while (remaining.Count > 0)
                {
                    ShareDirectoryClient dir = remaining.Dequeue();
                    foreach (ShareFileItem item in dir.GetFilesAndDirectories())
                    {
                        if (item.IsDirectory)
                        {
                            remaining.Enqueue(dir.GetSubdirectoryClient(item.Name));
                        }
                        else if (item.Name.Contains(MIDDEN_FILE_EXTENSION))
                        {
                            Console.WriteLine($"  In {dir.Uri.AbsolutePath} found {item.Name}");

                            ShareFileClient file = dir.GetFileClient(item.Name);

                            ShareFileDownloadInfo fileContents = file.Download();
                            string json;
                            using (MemoryStream ms = new MemoryStream())
                            {
                                fileContents.Content.CopyTo(ms);
                                json = Encoding.UTF8.GetString(ms.ToArray());
                            }

                            // Parse json string
                            Metadata metadata = parser.Parse(json);

                            // Sets the dataset path relative to the Uri and Path specified in constructor
                            var filePath = 
                                Path.GetRelativePath(this.path, file.Path)
                                .Replace(MIDDEN_FILE_EXTENSION, "");

                            metadata.Dataset.DatasetPath = filePath;

                            metadatas.Add(metadata);
                        }
                    }
                }

                Console.WriteLine($"Found a total of {metadatas.Count} files");
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error ocurred: {e}");
            }

            return metadatas;
        }

        public List<Project> GetProjects(
            ProjectReader reader)
        {
            List<Project> projects = new List<Project>();

            try
            {
                var remaining = new Queue<ShareDirectoryClient>();

                remaining.Enqueue(shareClient.GetDirectoryClient(this.path));
                while (remaining.Count > 0)
                {
                    ShareDirectoryClient dir = remaining.Dequeue();
                    foreach (ShareFileItem item in dir.GetFilesAndDirectories())
                    {
                        if (item.IsDirectory)
                        {
                            remaining.Enqueue(dir.GetSubdirectoryClient(item.Name));
                        }
                        else if (item.Name.Contains(MIPPEN_FILE_SEARCH_TERM))
                        {
                            Console.WriteLine($"  In {dir.Uri.AbsolutePath} found {item.Name}");

                            ShareFileClient file = dir.GetFileClient(item.Name);

                            ShareFileDownloadInfo fileContents = file.Download();
                            Project project;
                            using (var stream = fileContents.Content)
                            {
                                project = reader.Read(stream);
                            }

                            if(project is not null)
                                projects.Add(project);
                            //string fileString;
                            //using (MemoryStream ms = new MemoryStream())
                            //{
                            //    fileContents.Content.CopyTo(ms);
                            //    fileString = Encoding.UTF8.GetString(ms.ToArray());
                            //}
                            //
                            //Project project = new Project()
                            //{
                            //    Name = file.Name.Replace(MIPPEN_FILE_EXTENSION, ""),
                            //    Description = fileString
                            //};
                        }
                    }
                }

                Console.WriteLine($"Found a total of {projects.Count} files");
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error ocurred: {e}");
            }

            return projects;
        }
    }
}
