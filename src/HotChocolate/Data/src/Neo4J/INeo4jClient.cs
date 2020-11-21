namespace HotChocolate.Data.Neo4J
{
    public interface INeo4jClient
    {
        bool IsConnected { get; }
    }
}