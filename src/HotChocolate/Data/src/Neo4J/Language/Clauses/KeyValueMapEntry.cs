namespace HotChocolate.Data.Neo4J.Language
{
    public class KeyValueMapEntry : Expression
    {
        public override ClauseKind Kind => ClauseKind.KeyValueMapEntry;

        public KeyValueMapEntry(string key, ILiteral value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }

        public ILiteral Value { get; }

        // public new void Visit(CypherVisitor visitor)
        // {
        //     visitor.Enter(this);
        //     Value.Visit(visitor);
        //     visitor.Leave(this);
        // }
    }
}
