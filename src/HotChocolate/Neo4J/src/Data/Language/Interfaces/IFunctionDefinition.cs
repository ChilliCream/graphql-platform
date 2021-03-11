namespace HotChocolate.Data.Neo4J.Language
{
    public interface IFunctionDefinition
    {
        public string GetImplementationName();

        public bool IsAggregate();
    }
}
