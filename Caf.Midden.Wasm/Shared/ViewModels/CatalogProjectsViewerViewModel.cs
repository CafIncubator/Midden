using System;
using System.Collections.Generic;

namespace Caf.Midden.Wasm.Shared.ViewModels
{
    public class CatalogProjectsViewerViewModel
    {
        public List<CatalogProject> BaseCatalogProjects { get; set; } = new List<CatalogProject>();
        public List<CatalogProject> FilteredCatalogProjects { get; set; } = new List<CatalogProject>();

        public string SearchTerm { get; set; }
    }

    public class CatalogProject
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string MarkdownDescription { get; set; }
        public DateTime? LastModified { get; set; }
        public string ProjectStatus { get; set; }
        public int DatasetCount { get; set; }
        public int VariableCount { get; set; }
    }
}
