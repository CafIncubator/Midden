using Caf.Midden.Core.Models.v0_2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Caf.Midden.Wasm.Shared.ViewModels
{
    public class MetadataLineageDiagramViewModel(Dataset dataset, Catalog catalog)
    {
        private readonly Dataset _Dataset = dataset;
        private readonly Catalog _Catalog = catalog;

        private const string MIDDEN_DATASET_FLAG = "[Midden]";
        private const int MAX_NUMBER_CONNECTIONS = 20;

        // Tracks the number of connections (NOT number of parent). This is to prevent infinite loops.
        private int _NumberConnections = 0;
        private DatasetNode _Node;
        public DatasetNode GetDatasetNode()
        {
            if (_Node == null)
            {
                _NumberConnections = 0;
                _Node = InitializeDatasetNode(_Dataset);
            }

            return _Node;
        }
        private DatasetNode InitializeDatasetNode(Dataset dataset)
        {
            var node = new DatasetNode(
                dataset.Name, 
                dataset.Project,
                dataset.Zone,
                GetRelativeUrlForDataset(dataset));

            foreach (var parentDatasetString in dataset.ParentDatasets)
            {
                var parentName = "";
                var parentUrl = "";
                var parentZone = "";
                var parentProject = "";

                if (parentDatasetString.StartsWith(MIDDEN_DATASET_FLAG))
                {
                    string potentialUrl = parentDatasetString.Replace(MIDDEN_DATASET_FLAG, "");
                    if (Uri.IsWellFormedUriString(potentialUrl, UriKind.Absolute))
                    {
                        // It's a Midden dataset with a valid url, so set URL and look up dataset in catalog
                        parentUrl = potentialUrl;

                        var parentDataset = GetDatasetFromCatalogByUrl(parentUrl);
                        if (parentDataset != null)
                        {
                            _NumberConnections += 1;

                            // Check if we've already reached max connections
                            // If so, stop recursive
                            if (_NumberConnections > MAX_NUMBER_CONNECTIONS)
                            {
                                return node;
                            }

                            // Recursive time! Call Initialize on this one
                            var parentNode = InitializeDatasetNode(parentDataset);

                            node.AddParent(parentNode);
                        }
                        else
                        {
                            // Failed to find the dataset in the catalog, so just set name as url
                            parentName = parentUrl;

                            var parentNode = new DatasetNode(parentName, parentProject, parentZone, parentUrl);
                            _NumberConnections += 1;
                            node.AddParent(parentNode);     
                        }
  
                    }
                    else
                    {
                        // It's not a valid url, so set the name to it and don't link anything
                        parentName = potentialUrl;

                        var parentNode = new DatasetNode(parentName, parentProject, parentZone, parentUrl);
                        _NumberConnections += 1;
                        node.AddParent(parentNode);
                    }
                }
                else
                {
                    parentName = parentDatasetString;

                    if (Uri.IsWellFormedUriString(parentDatasetString, UriKind.Absolute))
                    {
                        parentUrl = parentDatasetString;
                    }

                    var parentNode = new DatasetNode(parentName, parentProject, parentZone, parentUrl);
                    _NumberConnections += 1;
                    node.AddParent(parentNode);
                }
            }

            return node;
        }

        private string GetRelativeUrlForDataset(Dataset dataset)
        {
            var result = $"/catalog/datasets/{dataset.Zone}/{dataset.Project}/{dataset.Name}";

            return result;
        }
        private Dataset GetDatasetFromCatalogByUrl(string url)
        {
            // Parse the url, assume: {base url}/catalog/dataset/{zone}/{project}/{dataset}
            // NOTE: Url structure may change, so this is on weak footing

            Uri uri = new Uri(url);
            string pathOnly = uri.AbsolutePath;
            string[] segments = pathOnly.Split('/');

            if(segments.Length != 6)
            {
                // Not in Midden format
                return null;
            }

            string zone = segments[3];
            string project = segments[4];
            string name = segments[5];

            Dataset dataset = _Catalog.Metadatas.Where(
                m => m.Dataset.Zone == zone &&
                m.Dataset.Project == project &&
                m.Dataset.Name == name).FirstOrDefault().Dataset;

            return dataset;        
        }
    }

    public class DatasetNode
    {
        private List<DatasetNode> _Parents = new List<DatasetNode>();
        //private List<Node> _Children = new List<Node>();

       
        public string Zone { get; set; }
        public string Project { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }

        public DatasetNode(string name, string project, string zone, string url)
        {
            this.Name = name;
            this.Url = url;
            this.Zone = zone;
            this.Project = project;
        }

        public IReadOnlyCollection<DatasetNode> Parents
        {
            get { return _Parents.AsReadOnly(); }
        }

        public void AddParent(DatasetNode node)
        {
            _Parents.Add(node);
        }
    }
}
