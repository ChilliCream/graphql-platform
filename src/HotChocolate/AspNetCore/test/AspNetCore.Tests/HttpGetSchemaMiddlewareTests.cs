using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using HotChocolate.AspNetCore.Utilities;
using Snapshooter.Xunit;
using Xunit;
using System.Collections.Generic;
using Snapshooter;

namespace HotChocolate.AspNetCore
{
    public class HttpGetSchemaMiddlewareTests : ServerTestBase
    {
        public HttpGetSchemaMiddlewareTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        public static TheoryData<HttpMethod> HttpVerbs = new TheoryData<HttpMethod>
        {
            HttpMethod.Get,
            HttpMethod.Head
        };

        [Theory]
        [MemberData(nameof(HttpVerbs))]
        public async Task Download_GraphQL_SDL(HttpMethod httpMethod)
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var url = TestServerExtensions.CreateUrl("/graphql?sdl");
            var request = new HttpRequestMessage(httpMethod, url);

            // act
            HttpResponseMessage response = await server.CreateClient().SendAsync(request);

            // assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadAsStringAsync();
            result.MatchSnapshot();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Download_GraphQL_SDL_Checksum_Header(bool isEnabled)
        {
            // arrange
            TestServer server = CreateStarWarsServer(
                configureConventions: e => e.WithOptions(
                    new GraphQLServerOptions
                    {
                        EnableSchemaRequestsChecksumResponseHeader = isEnabled
                    }));
            var url = TestServerExtensions.CreateUrl("/graphql?sdl");
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // act
            HttpResponseMessage response = await server.CreateClient().SendAsync(request);

            // assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            if (isEnabled)
            {
                Assert.Contains(response.Headers, h => h.Key == HttpHeaderKeys.HotChocolateSchemaChecksum);
                IEnumerable<string> checksum = response.Headers.GetValues(HttpHeaderKeys.HotChocolateSchemaChecksum);
                checksum.MatchSnapshot(new SnapshotNameExtension($"IsEnabled_{isEnabled})"));
            }
            else
            {
                Assert.DoesNotContain(response.Headers, h => h.Key == HttpHeaderKeys.HotChocolateSchemaChecksum);
            }
        }

        [Fact]
        public async Task Download_GraphQL_SDL_Disabled()
        {
            // arrange
            TestServer server = CreateStarWarsServer(
                configureConventions: e => e.WithOptions(
                    new GraphQLServerOptions
                    {
                        EnableSchemaRequests = false
                    }));
            var url = TestServerExtensions.CreateUrl("/graphql?sdl");
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // act
            HttpResponseMessage response = await server.CreateClient().SendAsync(request);

            // assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var result = await response.Content.ReadAsStringAsync();
            result.MatchSnapshot();
        }
    }
}
