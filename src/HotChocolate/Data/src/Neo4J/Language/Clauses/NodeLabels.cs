using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language.Clauses
{
    /// <summary>
    /// Makes a list of NodeLabel.
    /// </summary>
    public class NodeLabels : Visitable
    {
        private readonly List<NodeLabel> _values;

        public NodeLabels(List<NodeLabel> values)
        {
            _values = values;
        }

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _values.ForEach(value => value.Visit(visitor));
            visitor.Leave(this);
        }
    }
}
