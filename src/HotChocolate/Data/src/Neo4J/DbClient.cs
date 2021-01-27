using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    public static class DbClient
    {
        public static IDriver  CreateDriver(string uri, string username, string password) =>
            GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));

        public static IDriver  CreateDriver(string uri) =>
            GraphDatabase.Driver(uri);
    }
}
