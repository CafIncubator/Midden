using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Models;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Caf.Midden.Cli.Services
{
    public class GoogleDriveCrawler : ICrawl
    {
        private const string MIDDEN_FILE_EXTENSION = ".midden";
        private const string MIPPEN_FILE_SEARCH_TERM = "DESCRIPTION.md";

        string[] Scopes = { DriveService.Scope.DriveReadonly };

        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string applicationName;

        private readonly DriveService service;

        public GoogleDriveCrawler(
            string clientId,
            string clientSecret,
            string applicationName)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.applicationName = applicationName;

            UserCredential credential;
            ClientSecrets clientSecrets = new ClientSecrets()
            {
                ClientId = this.clientId,
                ClientSecret = this.clientSecret
            };

            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets, 
                Scopes, 
                "user", 
                CancellationToken.None, 
                new FileDataStore("token.json", true)).Result;

            this.service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = this.applicationName
            });
        }

        // Stolen from: https://stackoverflow.com/a/38750409/1621156
        private string AbsPath(Google.Apis.Drive.v3.Data.File file)
        {
            var name = file.Name;
        
            if (file.Parents.Count() == 0)
            {
                return name;
            }
        
            var path = new List<string>();
        
            while (true)
            {
                var parent = GetFile(file.Parents[0]);

                // Stop when we find the root dir
                if (parent.Parents == null || parent.Parents.Count() == 0)
                {
                    break;
                }

                path.Insert(0, parent.Name);
                file = parent;
            }
            path.Add(name);
            return path.Aggregate((current, next) => Path.Combine(current, next));
        }

        // Stolen from: https://stackoverflow.com/a/38750409/1621156
        private Google.Apis.Drive.v3.Data.File GetFile(string id)
        {        
            // Fetch file from drive
            FilesResource.GetRequest request = service.Files.Get(id);
            request.Fields = "id, name, parents, driveId, trashed";
            request.SupportsAllDrives = true;
            request.SupportsTeamDrives = true;
            var parent = request.Execute();
        
            return parent;
        }

        // Gets a list of Google File Ids for files in drive with the extension ".midden"
        public List<string> GetFileNames(
            string fileNameContains = MIDDEN_FILE_EXTENSION)
        {
            
            List<string> names = new List<string>();

            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 100;
            listRequest.Fields = "nextPageToken, files(id, name, parents, trashed)";
            listRequest.Q = $"name contains '{fileNameContains}'";

            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;
            
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    if (file.Trashed == true)
                        continue;

                    if (file.Name.EndsWith(fileNameContains))
                    {
                        Console.WriteLine($" Found {file.Name}");
                        names.Add(file.Id);
                    }
                }
            }

            Console.WriteLine($"Found a total of {names.Count} files");

            return names;
        }
        
        public List<Google.Apis.Drive.v3.Data.File> GetFiles(
            string fileNameContains = MIDDEN_FILE_EXTENSION,
            bool fileNameContainsIsExactMatch = false,
            string? fileNameEndsWith = null)
        {
            List<Google.Apis.Drive.v3.Data.File> files = new List<Google.Apis.Drive.v3.Data.File>();
            
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 100;
            listRequest.Fields = "nextPageToken, files(id, name, parents, trashed)";

            string searchQuery;
            if (fileNameContainsIsExactMatch)
                searchQuery = $"name = '{fileNameContains}'";
            else
                searchQuery = $"name contains '{fileNameContains}'";

            listRequest.Q = searchQuery;

            List<Google.Apis.Drive.v3.Data.File> dirFiles = listRequest.Execute().Files.ToList();

            if (dirFiles != null && dirFiles.Count > 0)
            {
                if (string.IsNullOrEmpty(fileNameEndsWith))
                {
                    foreach (var file in dirFiles)
                    {
                        if (file.Trashed == true)
                            continue;

                        Console.WriteLine($"  Found {file.Name}");

                        files.Add(file);
                    }
                }
                else
                {
                    foreach (var file in dirFiles)
                    {
                        if (file.Trashed == true)
                            continue;

                        Console.WriteLine($"  Found {file.Name}");

                        files.Add(file);
                    }
                }
            }

            return files;
        }

        public List<Metadata> GetMetadatas(
            IMetadataParser parser)
        {
            List<Google.Apis.Drive.v3.Data.File> files = GetFiles(
                MIDDEN_FILE_EXTENSION,
                false,
                MIDDEN_FILE_EXTENSION);

            List<Metadata> metadatas = new List<Metadata>();

            foreach(var file in files)
            {
                if (file.Trashed == true)
                    continue;

                string json;
                
                using (MemoryStream ms = new MemoryStream())
                {
                    var fileRequest = service.Files.Get(file.Id);
                    fileRequest.Download(ms);
                    json = Encoding.UTF8.GetString(ms.ToArray());
                }
                
                Metadata metadata;
                try
                {
                    metadata = parser.Parse(json);
                }
                catch // Probably not a good idea
                {
                    continue;
                }

                // Try to set the path
                try
                {
                    var filePath = this.AbsPath(file);

                    metadata.Dataset.DatasetPath = 
                        filePath.Replace(MIDDEN_FILE_EXTENSION, "");
                }
                catch { } // Silent fail -- code smell

                metadatas.Add(metadata);
            }

            return metadatas;
        }

        public List<Project> GetProjects(
            ProjectReader reader)
        {
            List<Google.Apis.Drive.v3.Data.File> files = GetFiles(
                MIPPEN_FILE_SEARCH_TERM,
                true,
                ".md");

            List<Project> projects = new List<Project>();

            foreach (var file in files)
            {
                if (file.Trashed == true)
                    continue;

                string fileString;

                //Option 1
                Project project;
                using (MemoryStream ms = new MemoryStream())
                {
                    var fileRequest = service.Files.Get(file.Id);
                    fileRequest.Download(ms);
                    fileString = Encoding.UTF8.GetString(ms.ToArray());

                    // Google's API threw an error when passing the stream directly to Read() (also used ExecuteAsStream())
                    byte[] byteArray = Encoding.UTF8.GetBytes(fileString);
                    MemoryStream stream = new MemoryStream(byteArray);
                    project = reader.Read(stream);
                }

                if (project is not null)
                    projects.Add(project);
            }

            return projects;
        }
    }
}
