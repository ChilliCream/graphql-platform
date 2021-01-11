namespace HotChocolate.Data.Neo4J.Language
{
    public class KeyValueSeparator : Visitable
    {
        public override ClauseKind Kind => ClauseKind.KeyValueSeparator;
        public static KeyValueSeparator Instance => new();
    }
}
