using System.Net.Http.Headers;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
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
            TestServer server = CreateStarWarsServer("/graphql");

            // act
            Result result = await GetAsync(server);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Fetch_Tool_Config_With_Options()
        {
            // arrange
            ToolOptions options = new ToolOptions
            {
                DefaultDocument = "# foo"
            };
            TestServer server = CreateStarWarsServer("/graphql",
                builder => builder.WithToolOptions(options));

            // act
            Result result = await GetAsync(server);

            // assert
            result.MatchSnapshot();
        }

        private async Task<Result> GetAsync(TestServer server)
        {
            HttpResponseMessage response = await server.CreateClient().GetAsync(
                TestServerExtensions.CreateUrl("/graphql/bcp-config.json"));
            string content = await response.Content.ReadAsStringAsync();

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
