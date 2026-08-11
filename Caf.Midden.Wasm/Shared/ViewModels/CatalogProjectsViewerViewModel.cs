using System;
using System.Collections.Generic;

namespace Caf.Midden.Wasm.Shared.ViewModels
{
    public class CatalogProjectsViewerViewModel
    {
        public List<CatalogProject> BaseCatalogProjects { get; set; } = new();
        public List<CatalogProject> FilteredCatalogProjects { get; set; } = new();
        public List<CatalogProject> PagedCatalogProjects { get; set; } = new();

        public string SearchTerm { get; set; } = string.Empty;
    }

    public class CatalogProject
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MarkdownDescription { get; set; } = string.Empty;
        public DateTime? LastModified { get; set; }
        public string ProjectStatus { get; set; } = string.Empty;
        public int DatasetCount { get; set; }
        public int VariableCount { get; set; }
        public bool CanExpandDescription { get; set; }
        public bool IsDescriptionExpanded { get; set; }
    }
}
