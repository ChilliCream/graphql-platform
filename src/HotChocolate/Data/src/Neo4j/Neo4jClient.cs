using System;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4j
{
    public class Neo4jClient
    {
        // TODO: Add encryption level
        private readonly IDriver _connection;

        public bool IsConnected => _connection != default;
        public IDriver Connection => _connection;

        public Neo4jClient()
        {
            _connection = GraphDatabase.Driver(new Uri("bolt://localhost:7687"), AuthTokens.Basic("neo4j", "password"));
        }

        public Neo4jClient(Uri uri, string? username = null, string? password = null)
        {
            _connection = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));
        }

        public IAsyncSession GetAsyncSession(string? databaseName = null)
        {
            return _connection.AsyncSession(o => o.WithDatabase(databaseName ?? "neo4j"));
        }

    }
}
