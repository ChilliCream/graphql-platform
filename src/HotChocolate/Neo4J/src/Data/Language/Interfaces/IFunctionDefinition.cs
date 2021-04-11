namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Defines metadata for a function.
    /// </summary>
    public interface IFunctionDefinition
    {
        public string GetImplementationName();

        public bool IsAggregate();
    }
}
