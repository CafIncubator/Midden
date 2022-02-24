using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using Caf.Midden.Cli.Common;
using Caf.Midden.Cli.Models;
using Caf.Midden.Core.Models.v0_2;
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
    public class GoogleWorkspaceSharedDriveCrawler : ICrawl
    {
        private const string MIDDEN_FILE_EXTENSION = ".midden";
        private const string MIPPEN_FILE_EXTENSION = ".mippen";

        string[] Scopes = { DriveService.Scope.DriveReadonly };

        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string applicationName;
        private readonly DriveService service;

        private List<Google.Apis.Drive.v3.Data.TeamDrive> cachedDriveList;

        public GoogleWorkspaceSharedDriveCrawler(
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
                    if(this.cachedDriveList == null)
                    {
                        this.cachedDriveList = GetSharedDrives();
                    }

                    string driveName = cachedDriveList
                        .Where(d => d.Id == parent.DriveId)
                        .ToList()[0]
                        .Name;

                    if(!string.IsNullOrEmpty(driveName))
                        path.Insert(0, driveName);

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
            request.Fields = "id, name, parents, driveId";
            request.SupportsAllDrives = true;
            request.SupportsTeamDrives = true;
            var parent = request.Execute();
        
            return parent;
        }

        
        private List<Google.Apis.Drive.v3.Data.TeamDrive> GetSharedDrives()
        {
            var teamDriveList = service.Teamdrives.List();

            teamDriveList.Fields = "teamDrives(kind, id, name)";
            teamDriveList.PageSize = 100;

            var teamDrives = teamDriveList.Execute().TeamDrives.ToList();

            return teamDrives;
        }

        // Gets a list of Google File Ids for files in Shared Drives with the extension ".midden"
        // Limited to searching 100 shared drives and returning 100 midden files within each shared drive (TODO: Update paging to support more)
        public List<string> GetFileNames(string fileExtension = MIDDEN_FILE_EXTENSION)
        {
            List<string> names = new List<string>();

            if(this.cachedDriveList == null)
            {
                this.cachedDriveList = GetSharedDrives();
            }

            if (cachedDriveList != null && cachedDriveList.Count > 0)
            {
                foreach (var drive in cachedDriveList)
                {
                    FilesResource.ListRequest listRequest = service.Files.List();
                    listRequest.DriveId = drive.Id;
                    listRequest.PageSize = 100;
                    listRequest.Fields = "nextPageToken, files(id, name, parents)";
                    listRequest.IncludeItemsFromAllDrives = true;
                    listRequest.SupportsAllDrives = true;
                    listRequest.Corpora = "drive";
                    listRequest.Q = $"name contains '{fileExtension}'";

                    IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;

                    if (files != null && files.Count > 0)
                    {
                        foreach (var file in files)
                        {
                            if(file.Name.EndsWith(fileExtension))
                            {
                                Console.WriteLine($"  In {drive.Name} found {file.Name}");
                                names.Add(file.Id);
                            }
                        }
                    }
                }
            }
            
            Console.WriteLine($"Found a total of {names.Count} files");

            return names;
        }

        
        public List<Google.Apis.Drive.v3.Data.File> GetFiles(string fileExtension)
        {
            List<Google.Apis.Drive.v3.Data.File> files = new List<Google.Apis.Drive.v3.Data.File>();

            if (this.cachedDriveList == null)
            {
                this.cachedDriveList = GetSharedDrives();
            }

            if (cachedDriveList != null && cachedDriveList.Count > 0)
            {
                foreach (var drive in cachedDriveList)
                {
                    FilesResource.ListRequest listRequest = service.Files.List();
                    listRequest.DriveId = drive.Id;
                    listRequest.PageSize = 100;
                    listRequest.Fields = "nextPageToken, files(id, name, parents)";
                    listRequest.IncludeItemsFromAllDrives = true;
                    listRequest.SupportsAllDrives = true;
                    listRequest.Corpora = "drive";
                    listRequest.Q = $"name contains '{fileExtension}'";

                    List<Google.Apis.Drive.v3.Data.File> dirFiles = listRequest.Execute().Files.ToList();

                    if (dirFiles != null && dirFiles.Count > 0)
                    {
                        foreach (var file in dirFiles)
                        {
                            Console.WriteLine($"  In {drive.Name} found {file.Name}");

                            files.Add(file);
                        }
                    }
                }
            }

            return files;
        }

        public List<Metadata> GetMetadatas()
        {
            List<Google.Apis.Drive.v3.Data.File> files = GetFiles(MIDDEN_FILE_EXTENSION);

            List<Metadata> metadatas = new List<Metadata>();

            MetadataParser parser =
               new MetadataParser(
                   new MetadataConverter());

            foreach(var file in files)
            {
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

        public List<Project> GetProjects()
        {
            List<Google.Apis.Drive.v3.Data.File> files = GetFiles(MIPPEN_FILE_EXTENSION);

            List<Project> projects = new List<Project>();

            foreach (var file in files)
            {
                string fileString;

                using (MemoryStream ms = new MemoryStream())
                {
                    var fileRequest = service.Files.Get(file.Id);
                    fileRequest.Download(ms);
                    fileString = Encoding.UTF8.GetString(ms.ToArray());
                }

                Project project;
                try
                {
                    project = new Project()
                    {
                        Name = file.Name.Replace(MIPPEN_FILE_EXTENSION, ""),
                        Description = fileString
                    };
                }
                catch // Probably not a good idea
                {
                    continue;
                }

                projects.Add(project);
            }

            return projects;
        }
    }
}
