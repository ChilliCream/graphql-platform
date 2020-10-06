using HotChocolate.Data.Neo4j.Language.Clauses;

namespace HotChocolate.Data.Neo4j
{
    public class CypherVisitor : IVisitor
    {
        private readonly CypherWriter _writer = new CypherWriter();

        public void Enter(MatchClause match)
        {
            if (match.IsOptional)
            {
                _writer.Append("OPTIONAL ");
            }
            _writer.Append("MATCH ");
        }

        public void Leave(MatchClause match)
        {
            _writer.Append(" ");
        }

        public void Enter(NodeClause node)
        {
            _writer.Append($"({node.SymbolicName}:{node.Label}");
        }

        public void Leave(NodeClause match)
        {
            _writer.Append(")");
        }

        public override string ToString()
        {
            return _writer.Print();
        }
    }
}
