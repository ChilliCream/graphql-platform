using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A condition checking for the presence of labels on nodes.
    /// </summary>
    public class HasLabelCondition : Condition
    {
        public override ClauseKind Kind => ClauseKind.HasLabelCondition;
        private readonly SymbolicName _nodeName;
        private readonly List<NodeLabel> _nodeLabels;

        public static HasLabelCondition Create(SymbolicName nodeName, params string[] labels) {

            Ensure.IsNotNull(nodeName, "A symbolic name for the node is required.");
            Ensure.IsNotNull(labels, "Labels to query are required.");
            Ensure.IsNotNull(labels, "At least one label to query is required.");

            return new HasLabelCondition(nodeName,
                labels.Select(label => new NodeLabel(label)).ToList());
        }

        private HasLabelCondition(SymbolicName nodeName, List<NodeLabel> nodeLabels) {
            _nodeName = nodeName;
            _nodeLabels = nodeLabels;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _nodeName.Visit(cypherVisitor);
            _nodeLabels.ForEach(label => label.Visit(cypherVisitor));
            cypherVisitor.Leave(this);
        }
    }
}
