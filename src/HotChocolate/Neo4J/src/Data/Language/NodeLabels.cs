using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Makes a list of <see cref="NodeLabel"/>.
    /// </summary>
    public class NodeLabels : Visitable
    {
        public NodeLabels(List<NodeLabel> labels)
        {
            Labels = labels;
        }

        public override ClauseKind Kind => ClauseKind.NodeLabels;

        public List<NodeLabel> Labels { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Labels.ForEach(value => value.Visit(cypherVisitor));
            cypherVisitor.Leave(this);
        }
    }
}
