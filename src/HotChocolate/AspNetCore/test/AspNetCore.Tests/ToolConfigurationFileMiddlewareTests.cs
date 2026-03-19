using System.Net;
using System.Net.Http.Headers;
using ChilliCream.Nitro.App;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

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
        var server = CreateServer(builder => builder.MapNitroApp()
            .WithOptions(o => o.ServeMode = ServeMode.Embedded));

        // act
        var result = await GetNitroConfigAsync(server, "/graphql/ui");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Tool_Config_Without_Options_Explicit_Route_Combined()
    {
        // arrange
        var server = CreateServer(builder =>
        {
            builder.MapGraphQLHttp();
            builder.MapNitroApp()
                .WithOptions(o => o.ServeMode = ServeMode.Embedded);
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
        var server = CreateServer(b => b.MapNitroApp("/foo/bar")
            .WithOptions(o => o.ServeMode = ServeMode.Embedded));

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
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyServerOptions(o => o.Tool.Enable = false),
            configureConventions: e => e.WithOptions(o =>
                {
                    o.ServeMode = ServeMode.Version(version);
                    o.Enable = false;
                }));

        // act
        var result = await GetAsync(server, "/graphql/index.html");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Tool_When_Disabled_With_ServerOptions_Override()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureConventions: e => e.WithOptions(o => o.Tool.Enable = false));

        // act
        var result = await GetAsync(server, "/graphql/index.html");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Tool_Config_With_ServerOptions_Override_Using_Global_Tool_Options()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyServerOptions(o => o.Tool.Title = "Global"),
            configureConventions: e => e.WithOptions(o => o.Tool.Title += " Local"));

        // act
        var result = await GetNitroConfigAsync(server);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Tool_Config_With_Options()
    {
        // arrange
        var server = CreateStarWarsServer("/graphql",
            configureConventions: builder => builder.WithOptions(o =>
            {
                o.ServeMode = ServeMode.Embedded;
                o.Document = "# foo";
                o.IncludeCookies = true;
                o.HttpHeaders =
                    new HeaderDictionary { { "Content-Type", "application/json" } };
                o.UseGet = true;
                o.Enable = true;
                o.Title = "Hello";
                o.GaTrackingId = "GA-FOO";
                o.GraphQLEndpoint = "/foo/bar";
                o.UseBrowserUrlAsGraphQLEndpoint = true;
                o.DisableTelemetry = true;
            }));

        // act
        var result = await GetNitroConfigAsync(server);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_MapNitroApp_Tool_Config()
    {
        // arrange
        var server = CreateServer(endpoint => endpoint.MapNitroApp()
            .WithOptions(o => o.ServeMode = ServeMode.Embedded));

        // act
        var result = await GetNitroConfigAsync(server, "/graphql/ui");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_MapNitroApp_Tool_FromCdn()
    {
        // arrange
        var server = CreateServer(endpoint => endpoint.MapNitroApp()
            .WithOptions(o => o.ServeMode = ServeMode.Version("5.0.8")));

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
            StatusCode = response.StatusCode
        };
    }

    private sealed class Result
    {
        public string Content { get; set; } = null!;

        public MediaTypeHeaderValue ContentType { get; set; } = null!;

        public HttpStatusCode StatusCode { get; set; }
    }
}
