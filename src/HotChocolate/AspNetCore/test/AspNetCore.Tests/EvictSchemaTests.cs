using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace HotChocolate.AspNetCore
{
    public class EvictSchemaTests : ServerTestBase
    {
        public EvictSchemaTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task Evict_Default_Schema()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            ClientQueryResult time1 = await server.GetAsync(
                new ClientQueryRequest { Query = "{ time }" });

            // act
            await server.GetAsync(
                new ClientQueryRequest { Query = "{ evict }" });

            // assert
            ClientQueryResult time2 = await server.GetAsync(
                new ClientQueryRequest { Query = "{ time }" });
            Assert.False(((long)time1.Data["time"]).Equals((long)time2.Data["time"]));
        }

        [Fact]
        public async Task Evict_Named_Schema()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            ClientQueryResult time1 = await server.GetAsync(
                new ClientQueryRequest { Query = "{ time }" },
                "/evict");

            // act
            await server.GetAsync(
                new ClientQueryRequest { Query = "{ evict }" },
                "/evict");

            // assert
            ClientQueryResult time2 = await server.GetAsync(
                new ClientQueryRequest { Query = "{ time }" },
                "/evict");
            Assert.False(((long)time1.Data["time"]).Equals((long)time2.Data["time"]));
        }
    }
}
