using System.Net;
using System.Net.Http.Headers;
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
        var result = await GetNitroConfigAsync(server);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Tool_Config_Without_Options_Explicit_Route()
    {
        // arrange
        var options = new GraphQLToolOptions { ServeMode = GraphQLToolServeMode.Embedded, };
        var server = CreateServer(builder => builder.MapNitroApp().WithOptions(options));

        // act
        var result = await GetNitroConfigAsync(server, "/graphql/ui");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Tool_Config_Without_Options_Explicit_Route_Combined()
    {
        // arrange
        var options = new GraphQLToolOptions { ServeMode = GraphQLToolServeMode.Embedded, };
        var server = CreateServer(builder =>
        {
            builder.MapGraphQLHttp();
            builder.MapNitroApp().WithOptions(options);
        });

        // act
        var result = await GetNitroConfigAsync(server, "/graphql/ui");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Tool_Config_Without_Options_Explicit_Route_Explicit_Path()
    {
        // arrange
        var options = new GraphQLToolOptions { ServeMode = GraphQLToolServeMode.Embedded, };
        var server = CreateServer(b => b.MapNitroApp("/foo/bar").WithOptions(options));

        // act
        var result = await GetNitroConfigAsync(server, "/foo/bar");

        // assert
        result.MatchSnapshot();
    }

    [Theory]
    [InlineData("embedded")]
    [InlineData("latest")]
    [InlineData("insider")]
    [InlineData("1.0.0")]
    public async Task Fetch_Tool_When_Disabled(string version)
    {
        // arrange
        var options = new GraphQLServerOptions
        {
            Tool = { ServeMode = GraphQLToolServeMode.Version(version), Enable = false, },
        };
        var server = CreateStarWarsServer(configureConventions: e => e.WithOptions(options));

        // act
        var result = await GetAsync(server, "/graphql/index.html");

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
                ServeMode = GraphQLToolServeMode.Embedded,
                Document = "# foo",
                IncludeCookies = true,
                HttpHeaders =
                    new HeaderDictionary { { "Content-Type", "application/json" }, },
                HttpMethod = DefaultHttpMethod.Get,
                Enable = true,
                Title = "Hello",
                GaTrackingId = "GA-FOO",
                GraphQLEndpoint = "/foo/bar",
                UseBrowserUrlAsGraphQLEndpoint = true,
                DisableTelemetry = true,
            },
        };

        var server = CreateStarWarsServer("/graphql", configureConventions: builder => builder.WithOptions(options));

        // act
        var result = await GetNitroConfigAsync(server);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_MapNitroApp_Tool_Config()
    {
        // arrange
        var options = new GraphQLToolOptions { ServeMode = GraphQLToolServeMode.Embedded, };
        var server = CreateServer(endpoint => endpoint.MapNitroApp().WithOptions(options));

        // act
        var result = await GetNitroConfigAsync(server, "/graphql/ui");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_MapNitroApp_Tool_FromCdn()
    {
        // arrange
        var options = new GraphQLToolOptions
        {
            ServeMode = GraphQLToolServeMode.Version("5.0.8"),
        };
        var server = CreateServer(endpoint => endpoint.MapNitroApp().WithOptions(options));

        // act
        var result = await GetAsync(server, "/graphql/ui/index.html");

        // assert
        Assert.Contains("static/js/main.98391269.js", result.Content);
    }

    private Task<Result> GetNitroConfigAsync(TestServer server, string url = "/graphql")
    {
        return GetAsync(server, $"{url}/nitro-config.json");
    }

    private async Task<Result> GetAsync(TestServer server, string url = "/graphql")
    {
        var response = await server.CreateClient()
            .GetAsync(TestServerExtensions.CreateUrl(url));
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
