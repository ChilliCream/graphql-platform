namespace HotChocolate.Data.Neo4J.Language
{
    public sealed class NullLiteral : Literal<string>
    {
        private NullLiteral() : base("null")
        {
        }

        public override string Print() => "NULL";

        public static NullLiteral Instance { get; } = new();
    }
}
