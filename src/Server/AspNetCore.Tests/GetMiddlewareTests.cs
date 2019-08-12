using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.TestHost;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore
{
    public class GetMiddlewareTests
       : ServerTestBase
    {
        public GetMiddlewareTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task HttpGet_QueryOnly()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = "{ hero { name } }";

            // act
            HttpResponseMessage message =
                await server.SendGetRequestAsync(request);

            // assert
            ClientQueryResult result = await DeserializeAsync(message);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task HttpGet_ContentType()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var request = "{ hero { name } }";

            // act
            HttpResponseMessage message =
                await server.SendGetRequestAsync(request);

            // assert
            Assert.Collection(
                message.Content.Headers.GetValues("Content-Type"),
                t => Assert.Equal("application/json", t));
        }
    }
}
