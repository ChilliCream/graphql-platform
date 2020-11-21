namespace HotChocolate.Data.Neo4J.Language
{
    public class IntegerLiteral : Literal<int>
    {
        public IntegerLiteral(int content) : base(content) { }
        public override string AsString() => GetContent().ToString();
    }
}