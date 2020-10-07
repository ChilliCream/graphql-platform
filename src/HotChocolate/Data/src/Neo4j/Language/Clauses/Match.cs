namespace HotChocolate.Data.Neo4j
{
    public class Match : IVisitable
    {
        private readonly bool _optional;
        private readonly Node? _node;

        public Match(bool optional = false, Node node = null)
        {
            _optional = optional;
            _node = node;
        }

        public Match(Node node = null)
        {
            _node = node;
        }

        public Match() : base() { }

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            visitor.VisitIfNotNull(_node);
            visitor.Leave(this);
        }

        public bool IsOptional => _optional;
    }
}
