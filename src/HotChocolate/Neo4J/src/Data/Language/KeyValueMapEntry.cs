namespace HotChocolate.Data.Neo4J.Language
{
    public class KeyValueMapEntry : Expression
    {
        private readonly Expression _value;

        public KeyValueMapEntry(string key, Expression value)
        {
            Key = key;
            _value = value;
        }

        public override ClauseKind Kind => ClauseKind.KeyValueMapEntry;

        public string Key { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);

            _value.Visit(cypherVisitor);

            cypherVisitor.Leave(this);
        }
    }
}
