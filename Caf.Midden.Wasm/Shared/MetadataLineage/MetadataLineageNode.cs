using Blazor.Diagrams.Core.Models;

namespace Caf.Midden.Wasm.Shared.MetadataLineage
{
    public class MetadataLineageNode : NodeModel
    {
        public MetadataLineageNode(Blazor.Diagrams.Core.Geometry.Point? position = null): base(position) { }

        public string Name { get; set; }
        public string Project { get; set; }
        public string Zone { get; set; }
        public string Url { get; set; }
    }
}
