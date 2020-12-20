using System;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    public class DbClient
    {
        public bool IsConnected => Driver != null;

        public DbClient(Uri uri, string username, string password)
        {
            Driver = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));
        }

        public IDriver Driver { get; }
    }
}
