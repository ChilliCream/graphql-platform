namespace HotChocolate.Data.Neo4J.Language
{
    public interface IStatementBuilder
    {
        public string Build();
        public IStatementBuilder GetDefaultBuilder();
    }
}
