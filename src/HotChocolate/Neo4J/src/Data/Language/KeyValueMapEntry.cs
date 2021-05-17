namespace HotChocolate.Data.Neo4J.Language
{
    public class KeyValueMapEntry : Expression
    {
        private readonly string _key;
        private readonly Expression _value;

        public KeyValueMapEntry(string key, Expression value)
        {
            _key = key;
            _value = value;
        }

        public override ClauseKind Kind => ClauseKind.KeyValueMapEntry;

        public string GetKey() => _key;

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);

            _value.Visit(cypherVisitor);

            cypherVisitor.Leave(this);
        }
    }
}
