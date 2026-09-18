using System.Net;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.AspNetCore;

public class HttpGetSemanticNonNullSchemaMiddlewareTests(TestServerFactory serverFactory)
    : ServerTestBase(serverFactory)
{
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
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

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
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

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
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

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
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

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
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_SemanticNonNull_Schema_Does_Not_Include_Internal_Directives()
    {
        // arrange
        var server = ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .AddDirectiveType<InternalDirectiveType>()
                .AddQueryType<DirectiveQueryType>(),
            app => app
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQLSemanticNonNullSchema()));
        var url = TestServerExtensions.CreateUrl("/graphql/semantic-non-null-schema.graphql");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_SemanticNonNull_Schema_Includes_Internal_Directives_When_DisableInternalDirectives_Is_True()
    {
        // arrange
        var server = ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .ModifyOptions(o => o.DisableInternalDirectives = true)
                .AddDirectiveType<InternalDirectiveType>()
                .AddQueryType<DirectiveQueryType>(),
            app => app
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQLSemanticNonNullSchema()));
        var url = TestServerExtensions.CreateUrl("/graphql/semantic-non-null-schema.graphql");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        result.MatchSnapshot();
    }

    // Content suppression for HEAD is the HTTP server's responsibility and TestServer,
    // unlike Kestrel, does not emulate it, so only the status and headers are compared.
    [Fact]
    public async Task Head_Should_AnswerAsGet_When_SchemaIsRequested()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: sp =>
                sp.AddGraphQLServer()
                    .ConfigureSchemaServices(s =>
                        s.RemoveAll<ITimeProvider>()
                            .AddSingleton<ITimeProvider, StaticTimeProvider>()));
        var url = TestServerExtensions.CreateUrl("/graphql/semantic-non-null-schema.graphql");
        var client = server.CreateClient();

        // act
        using var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
        using var getResponse = await client.SendAsync(getRequest, TestContext.Current.CancellationToken);

        using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
        using var headResponse = await client.SendAsync(headRequest, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(getResponse.StatusCode, headResponse.StatusCode);
        Assert.Equal(
            getResponse.Content.Headers.ContentType?.ToString(),
            headResponse.Content.Headers.ContentType?.ToString());
    }

    private sealed class StaticTimeProvider : ITimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    public class DirectiveQueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");
            descriptor.Field("secret").Type<NonNullType<StringType>>().Resolve("secret").Directive("internal");
            descriptor.Field("public").Type<NonNullType<StringType>>().Resolve("public");
        }
    }

    public class InternalDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("internal");
            descriptor.Location(DirectiveLocation.FieldDefinition);
            descriptor.Internal();
        }
    }
}
