namespace HotChocolate.Data.Neo4J
{
    public interface IDbClient
    {
        bool IsConnected { get; }
    }
}
