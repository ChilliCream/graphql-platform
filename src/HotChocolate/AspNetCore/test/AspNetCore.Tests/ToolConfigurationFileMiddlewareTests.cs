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
            HttpResponseMessage response = await server.CreateClient().GetAsync(
                TestServerExtensions.CreateUrl("/bcp-config.json"));

            // assert
            response.MatchSnapshot();
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
            HttpResponseMessage response = await server.CreateClient().GetAsync(
                TestServerExtensions.CreateUrl("/bcp-config.json"));

            // assert
            response.MatchSnapshot();
        }
    }
}
