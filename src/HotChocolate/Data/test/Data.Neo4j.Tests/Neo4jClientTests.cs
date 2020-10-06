using Xunit;
using Neo4j.Driver;
using Squadron;
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4j.Tests
{
    public class Neo4jClientTests : IClassFixture<Neo4jResource>
    {
        private Neo4jResource _neo4JResource { get; }

        public Neo4jClientTests(Neo4jResource neo4jResource)
        {
            _neo4JResource = neo4jResource;
        }

        [Fact]
        public async void TestConnection()
        {
            var client = new Neo4jClient();

            IAsyncSession session = _neo4JResource.GetAsyncSession();

            try
            {
                IResultCursor cursor = await session.RunAsync("Match () Return 1 Limit 1");
                List<string> list = await cursor.ToListAsync(record => record.Keys.As<string>());
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}
