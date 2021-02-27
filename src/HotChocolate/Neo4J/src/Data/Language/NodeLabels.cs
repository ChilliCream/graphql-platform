using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Makes a list of NodeLabel.
    /// </summary>
    public class NodeLabels : Visitable
    {
        public override ClauseKind Kind => ClauseKind.NodeLabels;
        private readonly List<NodeLabel> _labels;

        public NodeLabels(List<NodeLabel> labels)
        {
            _labels = labels;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _labels.ForEach(value => value.Visit(cypherVisitor));
            cypherVisitor.Leave(this);
        }
    }
}
