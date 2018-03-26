using System.Collections.Generic;

namespace Prometheus.Language
{
    public class OperationDefinitionNode
        : IExecutableDefinitionNode
    {
        public NodeKind Kind { get; } = NodeKind.OperationDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public OperationTypeNode Operation { get; }
        public IReadOnlyCollection<VariableDefinitionNode> VariableDefinitions { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public SelectionSetNode SelectionSet { get; }
    }
}