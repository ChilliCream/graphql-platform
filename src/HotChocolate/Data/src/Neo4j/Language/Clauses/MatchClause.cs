using HotChocolate.Data.Neo4j.Language.Clauses;

namespace HotChocolate.Data.Neo4j
{
    public class MatchClause : IVisitable
    {
        private readonly bool _optional;
        private readonly NodeClause _node;

        public MatchClause(bool optional = false, NodeClause node = null)
        {
            _optional = optional;
            _node = node;
        }

        public MatchClause(NodeClause node = null)
        {
            _node = node;
        }

        public MatchClause() { }


        public void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            if (_node is not null)
                _node.Visit(visitor);
            visitor.Leave(this);
        }

        public bool IsOptional => _optional;
    }
}
