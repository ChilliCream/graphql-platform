namespace HotChocolate.Data.Neo4J.Language
{
    public interface ILiteral : IExpression
    {
        public abstract string AsString();
    }
}
