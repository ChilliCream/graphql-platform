namespace HotChocolate.Data.Neo4J.Language
{
    public sealed class NullLiteral : Literal<string>
    {
        public readonly static NullLiteral Instance = new NullLiteral();

        private NullLiteral() : base("null") { }

        public override string AsString() => "NULL";
    }
}
