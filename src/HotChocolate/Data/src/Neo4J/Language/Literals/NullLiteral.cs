namespace HotChocolate.Data.Neo4J.Language
{
    public sealed class NullLiteral : Literal<string>
    {
        public static readonly NullLiteral Instance = new();

        private NullLiteral() : base("null") { }

        public override string AsString() => "NULL";
    }
}
