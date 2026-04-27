using System.Net;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.AspNetCore;

public class HttpGetSemanticNonNullSchemaMiddlewareTests : ServerTestBase
{
    public HttpGetSemanticNonNullSchemaMiddlewareTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task Download_GraphQL_SemanticNonNull_Schema()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: sp =>
                sp.AddGraphQLServer()
                    .ConfigureSchemaServices(s =>
                        s.RemoveAll<ITimeProvider>()
                            .AddSingleton<ITimeProvider, StaticTimeProvider>()));
        var url = TestServerExtensions.CreateUrl("/graphql/semantic-non-null-schema.graphql");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Headers.Remove("ETag");
        response.Content.Headers.ContentLength = null;

        response.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_SemanticNonNull_Schema_Not_Allowed_When_FileSupport_Disabled()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyServerOptions(o => o.EnableSchemaFileSupport = false));
        var url = TestServerExtensions.CreateUrl("/graphql/semantic-non-null-schema.graphql");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Download_GraphQL_SemanticNonNull_Schema_Disabled_When_SchemaRequests_Disabled()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyServerOptions(o =>
                {
                    o.EnableSchemaRequests = false;
                    o.Tool.Enable = false;
                }));
        var url = TestServerExtensions.CreateUrl("/graphql/semantic-non-null-schema.graphql");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_To_SemanticNonNull_Schema_Endpoint_Returns_NotFound()
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql/semantic-non-null-schema.graphql");
        var request = new HttpRequestMessage(HttpMethod.Post, url);

        // act
        var response = await server.CreateClient().SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Download_GraphQL_SemanticNonNull_Schema_Explicit_Pattern()
    {
        // arrange
        var server = CreateServer(b => b.MapGraphQLSemanticNonNullSchema("/foo/bar.graphql"));
        var url = TestServerExtensions.CreateUrl("/foo/bar.graphql");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        result.MatchSnapshot();
    }

    private sealed class StaticTimeProvider : ITimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
