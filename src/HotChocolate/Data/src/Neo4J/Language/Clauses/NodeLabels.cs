using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Makes a list of NodeLabel.
    /// </summary>
    public class NodeLabels : Visitable
    {
        public new ClauseKind Kind { get; } = ClauseKind.NodeLabels;

        private readonly List<NodeLabel> _labels;

        public NodeLabels(List<NodeLabel> labels)
        {
            _labels = labels;
        }

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _labels.ForEach(value => value.Visit(visitor));
            visitor.Leave(this);
        }
    }
}
