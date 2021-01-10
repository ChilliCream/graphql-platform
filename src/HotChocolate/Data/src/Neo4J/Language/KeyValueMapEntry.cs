namespace HotChocolate.Data.Neo4J.Language
{
    public class KeyValueMapEntry : Expression
    {
        public override ClauseKind Kind => ClauseKind.KeyValueMapEntry;
        private readonly string _key;
        private readonly Expression _value;

        public KeyValueMapEntry(string key, Expression value)
        {
            _key = key;
            _value = value;
        }

        public string GetKey() => _key;

        public new void Visit(CypherVisitor visitor)
        {
             visitor.Enter(this);
             _value.Visit(visitor);
             visitor.Leave(this);
        }
    }
}
