namespace HotChocolate.Data.Neo4j
{
    public class Property : IVisitable
    {
        private readonly string _key;
        private readonly string? _parameter;
        private readonly string _clauseOperator;

        public Property(string key, string parameter, string? clauseOperator = ClauseOperator.Equal)
        {
            _key = key;
            _parameter = parameter;
            _clauseOperator = clauseOperator;
        }

        public string Key => _key;
        public string Operator => _clauseOperator;
        public string? Parameter => _parameter;

        public void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            visitor.Leave(this);
        }
    }
}
