using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using HotChocolate.AspNetCore.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore
{
    public class ToolConfigurationFileMiddlewareTests
        : ServerTestBase
    {
        public ToolConfigurationFileMiddlewareTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task Fetch_Tool_Config_Without_Options()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            Result result = await GetAsync(server);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Fetch_Tool_When_Disabled()
        {
            // arrange
            TestServer server = CreateStarWarsServer(
                configureConventions: e => e.WithOptions(
                    new GraphQLServerOptions
                    {
                        Tool = { Enable = false }
                    }));

            // act
            Result result = await GetAsync(server);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Fetch_Tool_Config_With_Options()
        {
            // arrange
            var options = new GraphQLServerOptions
            {
                Tool =
                {
                    Document = "# foo",
                    Credentials = DefaultCredentials.SameOrigin,
                    HttpHeaders = new HeaderDictionary
                    {
                        { "Content-Type", "application/json" }
                    },
                    HttpMethod = DefaultHttpMethod.Get
                }
            };
            TestServer server = CreateStarWarsServer("/graphql",
                configureConventions: builder => builder.WithOptions(options));

            // act
            Result result = await GetAsync(server);

            // assert
            result.MatchSnapshot();
        }

        private async Task<Result> GetAsync(TestServer server)
        {
            HttpResponseMessage response = await server.CreateClient().GetAsync(
                TestServerExtensions.CreateUrl("/graphql/bcp-config.json"));
            var content = await response.Content.ReadAsStringAsync();

            return new Result
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType,
                StatusCode = response.StatusCode,
            };
        }

        private class Result
        {
            public string Content { get; set; }

            public MediaTypeHeaderValue ContentType { get; set; }

            public HttpStatusCode StatusCode { get; set; }
        }
    }
}
