namespace HotChocolate.Data.Neo4J.Language
{
    public class KeyValueMapEntry : Expression
    {
        public KeyValueMapEntry(string key, Expression value)
        {
            Key = key;
            Value = value;
        }

        public override ClauseKind Kind => ClauseKind.KeyValueMapEntry;

        public string Key { get; }

        public Expression Value { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);

            Value.Visit(cypherVisitor);

            cypherVisitor.Leave(this);
        }
    }
}
