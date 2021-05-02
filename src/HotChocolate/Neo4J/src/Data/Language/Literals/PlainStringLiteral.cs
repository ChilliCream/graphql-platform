namespace HotChocolate.Data.Neo4J.Language
{
    public class PlainStringLiteral : Literal<string>
    {
        public PlainStringLiteral(string content) : base(content)
        {
        }

        public override string AsString() => GetContent();
    }
}
