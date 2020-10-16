namespace HotChocolate.Data.Neo4j
{
    public class Match : IVisitable
    {
        private readonly Node _node;
        private readonly bool _optional;

        public Match(Node node, bool optional = false)
        {
            _node = node;
            _optional = optional;
        }

        public void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            visitor.VisitIfNotNull(_node);
            visitor.Leave(this);
        }

        public bool IsOptional => _optional;
    }
}
