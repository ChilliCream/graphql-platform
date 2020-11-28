using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    public interface IDbClient
    {
        public IDriver Driver { get; }
        bool IsConnected { get; }
    }
}
