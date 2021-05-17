namespace HotChocolate.Data.Neo4J.Language
{
    public sealed class NullLiteral : Literal<string>
    {
        private NullLiteral() : base("null")
        {
        }

        public override string AsString() => "NULL";

        public static readonly NullLiteral Instance = new();
    }
}
