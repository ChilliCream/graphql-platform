using System.Collections.Generic;

namespace Prometheus.Language
{
    public class SchemaDefinitionNode
      : ITypeSystemDefinitionNode
    {
        public NodeKind Kind { get; } = NodeKind.SchemaDefinition;
        public Location Location { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<OperationTypeDefinitionNode> OperationTypes { get; }
    }
}