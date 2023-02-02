using System.Net;
using System.Net.Http.Headers;
using CookieCrumble;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace HotChocolate.AspNetCore;

public class ToolConfigurationFileMiddlewareTests : ServerTestBase
{
    public ToolConfigurationFileMiddlewareTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task Fetch_Tool_Config_Without_Options()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result = await GetAsync(server);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Tool_Config_Without_Options_Explicit_Route()
    {
        // arrange
        var server = CreateServer(b => b.MapBananaCakePop());

        // act
        var result = await GetAsync(server, "/graphql/ui");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Tool_Config_Without_Options_Explicit_Route_Combined()
    {
        // arrange
        var server = CreateServer(b =>
        {
            b.MapGraphQLHttp();
            b.MapBananaCakePop();
        });

        // act
        var result = await GetAsync(server, "/graphql/ui");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Tool_Config_Without_Options_Explicit_Route_Explicit_Path()
    {
        // arrange
        var server = CreateServer(b => b.MapBananaCakePop("/foo/bar"));

        // act
        var result = await GetAsync(server, "/foo/bar");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Tool_When_Disabled()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureConventions: e => e.WithOptions(
                new GraphQLServerOptions
                {
                    Tool = { Enable = false }
                }));

        // act
        var result = await GetAsync(server);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Tool_Config_With_Options()
    {
        // arrange
        var options = new GraphQLServerOptions
        {
            Tool = {
                Document = "# foo",
                IncludeCookies = true,
                HttpHeaders = new HeaderDictionary
                {
                    { "Content-Type", "application/json" }
                },
                HttpMethod = DefaultHttpMethod.Get,
                Enable = true,
                Title = "Hello",
                GaTrackingId = "GA-FOO",
                GraphQLEndpoint = "/foo/bar",
                UseBrowserUrlAsGraphQLEndpoint = true,
                DisableTelemetry = true
            }
        };
        var server = CreateStarWarsServer("/graphql",
            configureConventions: builder => builder.WithOptions(options));

        // act
        var result = await GetAsync(server);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_MapBananaCakePop_Tool_Config()
    {
        // arrange
        var server = CreateServer(endpoint => endpoint.MapBananaCakePop());

        // act
        var result = await GetAsync(server, "/graphql/ui");

        // assert
        result.MatchSnapshot();
    }

    private async Task<Result> GetAsync(TestServer server, string url = "/graphql")
    {
        var response = await server.CreateClient().GetAsync(
            TestServerExtensions.CreateUrl($"{url}/bcp-config.json"));
        var content = await response.Content.ReadAsStringAsync();

        return new Result
        {
            Content = content,
            ContentType = response.Content.Headers.ContentType!,
            StatusCode = response.StatusCode,
        };
    }

    private sealed class Result
    {
        public string Content { get; set; } = default!;

        public MediaTypeHeaderValue ContentType { get; set; } = default!;

        public HttpStatusCode StatusCode { get; set; }
    }
}
