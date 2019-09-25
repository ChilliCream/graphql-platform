using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Owin.Testing;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetClassic
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
    }
}
