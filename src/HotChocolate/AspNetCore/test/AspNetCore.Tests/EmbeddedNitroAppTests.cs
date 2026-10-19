using System.Net;
using System.Net.Http.Headers;
using ChilliCream.Nitro.App;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.StarWars;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Forwarder;

namespace HotChocolate.AspNetCore;

public class EmbeddedNitroAppTests : ServerTestBase
{
    public EmbeddedNitroAppTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task MapGraphQL_WithEmbeddedNitroAssets_Serves_Nitro_And_GraphQL_On_The_Same_Path()
    {
        // arrange
        var forwarder = new RecordingForwarder();
        var server = CreateServer(forwarder, e => e.MapGraphQL().WithEmbeddedNitroAssets());

        // act
        var nitro = await GetHtmlAsync(server, "/graphql/");
        var graphql = await server.PostAsync(new ClientQueryRequest { Query = "{ __typename }" });

        // assert
        Assert.Equal(HttpStatusCode.OK, nitro.StatusCode);
        Assert.Equal("text/html", nitro.Content.Headers.ContentType?.MediaType);
        Assert.Null(forwarder.DestinationPrefix);
        Assert.Equal(HttpStatusCode.OK, graphql.StatusCode);
        Assert.Equal("Query", graphql.Data?["__typename"]);
    }

    [Fact]
    public async Task MapNitroApp_WithEmbeddedNitroAssets_Serves_Nitro_And_Config()
    {
        // arrange
        var forwarder = new RecordingForwarder();
        var server = CreateServer(
            forwarder,
            e => e.MapNitroApp().WithOptions(o => o.Title = "Embedded").WithEmbeddedNitroAssets());

        // act
        var nitro = await GetHtmlAsync(server, "/graphql/ui/");
        var config = await server.CreateClient().GetStringAsync(
            "/graphql/ui/nitro-config.json",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, nitro.StatusCode);
        Assert.Equal("text/html", nitro.Content.Headers.ContentType?.MediaType);
        Assert.Null(forwarder.DestinationPrefix);
        Assert.Contains("\"title\":\"Embedded\"", config);
    }

    [Fact]
    public async Task MapGraphQL_WithEmbeddedNitroAssets_In_MapGroup_Serves_Nitro_And_GraphQL()
    {
        // arrange
        var forwarder = new RecordingForwarder();
        var server = CreateServer(forwarder, e => e.MapGroup("/api").MapGraphQL().WithEmbeddedNitroAssets());

        // act
        var nitro = await GetHtmlAsync(server, "/api/graphql/");
        var graphql = await server.PostAsync(
            new ClientQueryRequest { Query = "{ __typename }" },
            "/api/graphql");

        // assert
        Assert.Equal(HttpStatusCode.OK, nitro.StatusCode);
        Assert.Equal("text/html", nitro.Content.Headers.ContentType?.MediaType);
        Assert.Null(forwarder.DestinationPrefix);
        Assert.Equal("Query", graphql.Data?["__typename"]);
    }

    [Fact]
    public async Task MapGroup_WithEmbeddedNitroAssets_Serves_Nitro_For_Every_Endpoint_In_The_Group()
    {
        // arrange
        var forwarder = new RecordingForwarder();
        var server = CreateServer(
            forwarder,
            e =>
            {
                var group = e.MapGroup("/dev").WithEmbeddedNitroAssets();
                group.MapGraphQL();
                group.MapNitroApp("/ui");
            });

        // act
        var graphQLNitro = await GetHtmlAsync(server, "/dev/graphql/");
        var nitroApp = await GetHtmlAsync(server, "/dev/ui/");

        // assert
        Assert.Equal(HttpStatusCode.OK, graphQLNitro.StatusCode);
        Assert.Equal(HttpStatusCode.OK, nitroApp.StatusCode);
        Assert.Null(forwarder.DestinationPrefix);
    }

    [Fact]
    public async Task MapGroup_WithEmbeddedNitroAssets_Wins_Over_Schema_Wide_ServeMode()
    {
        // arrange
        var forwarder = new RecordingForwarder();
        var server = ServerFactory.Create(
            services => services
                .AddSingleton<IHttpForwarder>(forwarder)
                .AddRouting()
                .AddGraphQLServer()
                .AddStarWarsTypes()
                .AddStarWarsRepositories()
                .ModifyServerOptions(o => o.Tool.ServeMode = ServeMode.Insider),
            app => app
                .UseRouting()
                .UseEndpoints(e => e.MapGroup("/dev").WithEmbeddedNitroAssets().MapGraphQL()));

        // act
        var nitro = await GetHtmlAsync(server, "/dev/graphql/");

        // assert
        Assert.Equal(HttpStatusCode.OK, nitro.StatusCode);
        Assert.Null(forwarder.DestinationPrefix);
    }

    [Fact]
    public async Task MapGraphQL_Middleware_Does_Not_Proxy_Nitro_For_Other_Endpoints_With_A_Slug()
    {
        // arrange
        var forwarder = new RecordingForwarder();
        var server = ServerFactory.Create(
            services => services
                .AddSingleton<IHttpForwarder>(forwarder)
                .AddRouting()
                .AddGraphQLServer()
                .AddStarWarsTypes()
                .AddStarWarsRepositories(),
            app => app
                .UseRouting()
                .MapGraphQL("/graphql", ISchemaDefinition.DefaultName)
                .UseEndpoints(e => e.MapGet("/posts/{slug}", () => "post")));

        // act
        await GetHtmlAsync(server, "/posts/hello");
        var postDestination = forwarder.DestinationPrefix;
        var nitro = await GetHtmlAsync(server, "/graphql/");

        // assert
        Assert.Null(postDestination);
        Assert.Equal(HttpStatusCode.OK, nitro.StatusCode);
        Assert.Equal("https://cdn.chillicream.com/web/latest/", forwarder.DestinationPrefix);
    }

    [Fact]
    public async Task MapGraphQL_With_ServeMode_Embedded_Still_Serves_Embedded_Nitro()
    {
        // arrange
        var forwarder = new RecordingForwarder();
        var server = CreateServer(
            forwarder,
            e => e.MapGraphQL().WithOptions(o => o.Tool.ServeMode = ServeMode.Embedded));

        // act
        var nitro = await GetHtmlAsync(server, "/graphql/");

        // assert
        Assert.Equal(HttpStatusCode.OK, nitro.StatusCode);
        Assert.Equal("text/html", nitro.Content.Headers.ContentType?.MediaType);
        Assert.Null(forwarder.DestinationPrefix);
    }

    [Fact]
    public async Task MapGraphQL_Without_Embedded_Proxies_Nitro_From_The_Cdn()
    {
        // arrange
        var forwarder = new RecordingForwarder();
        var server = CreateServer(forwarder, e => e.MapGraphQL());

        // act
        var nitro = await GetHtmlAsync(server, "/graphql/");

        // assert
        Assert.Equal(HttpStatusCode.OK, nitro.StatusCode);
        Assert.Equal("https://cdn.chillicream.com/web/latest/", forwarder.DestinationPrefix);
    }

    [Fact]
    public async Task WithOptions_Version_After_WithEmbeddedNitroAssets_Proxies_That_Version()
    {
        // arrange
        var forwarder = new RecordingForwarder();
        var server = CreateServer(
            forwarder,
            e => e.MapNitroApp()
                .WithEmbeddedNitroAssets()
                .WithOptions(o => o.ServeMode = ServeMode.Version("5.0.8")));

        // act
        var nitro = await GetHtmlAsync(server, "/graphql/ui/");

        // assert
        Assert.Equal(HttpStatusCode.OK, nitro.StatusCode);
        Assert.Equal("https://cdn.chillicream.com/web/5.0.8/", forwarder.DestinationPrefix);
    }

    private TestServer CreateServer(RecordingForwarder forwarder, Action<IEndpointRouteBuilder> configure)
        => ServerFactory.Create(
            services => services
                .AddSingleton<IHttpForwarder>(forwarder)
                .AddRouting()
                .AddGraphQLServer()
                .AddStarWarsTypes()
                .AddStarWarsRepositories(),
            app => app
                .UseRouting()
                .UseEndpoints(configure));

    private static Task<HttpResponseMessage> GetHtmlAsync(TestServer server, string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        return server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);
    }

    private sealed class RecordingForwarder : IHttpForwarder
    {
        public string? DestinationPrefix { get; private set; }

        public ValueTask<ForwarderError> SendAsync(
            HttpContext context,
            string destinationPrefix,
            HttpMessageInvoker httpClient,
            ForwarderRequestConfig requestConfig,
            HttpTransformer transformer,
            CancellationToken cancellationToken)
        {
            DestinationPrefix = destinationPrefix;
            context.Response.StatusCode = StatusCodes.Status200OK;
            return ValueTask.FromResult(ForwarderError.None);
        }

        public ValueTask<ForwarderError> SendAsync(
            HttpContext context,
            string destinationPrefix,
            HttpMessageInvoker httpClient,
            ForwarderRequestConfig requestConfig,
            HttpTransformer transformer)
            => SendAsync(context, destinationPrefix, httpClient, requestConfig, transformer, default);
    }
}
