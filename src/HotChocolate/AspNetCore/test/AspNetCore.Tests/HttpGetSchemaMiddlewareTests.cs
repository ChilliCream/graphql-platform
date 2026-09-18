using System.Net;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.AspNetCore;

public class HttpGetSchemaMiddlewareTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task Download_GraphQL_SDL()
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql?sdl");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Theory]
    [InlineData("/graphql?sdl")]
    [InlineData("/graphql/schema/")]
    [InlineData("/graphql/schema.graphql")]
    [InlineData("/graphql/schema")]
    public async Task Download_GraphQL_Schema(string path)
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: sp =>
                sp.AddGraphQLServer()
                    .ConfigureSchemaServices(s =>
                            s.RemoveAll<ITimeProvider>()
                            .AddSingleton<ITimeProvider, StaticTimeProvider>()));
        var url = TestServerExtensions.CreateUrl(path);
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Headers.Remove("ETag");
        response.Content.Headers.ContentLength = null;

        response.MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData("/graphql?sdl")]
    [InlineData("/graphql/schema/")]
    [InlineData("/graphql/schema.graphql")]
    [InlineData("/graphql/schema")]
    public async Task Download_GraphQL_Schema_Slicing_Args_Enabled(string path)
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: sp =>
                sp.AddGraphQLServer()
                    .ConfigureSchemaServices(s =>
                        s.RemoveAll<ITimeProvider>()
                            .AddSingleton<ITimeProvider, StaticTimeProvider>())
                    .ModifyPagingOptions(o => o.RequirePagingBoundaries = true));
        var url = TestServerExtensions.CreateUrl(path);
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Headers.Remove("ETag");
        response.Content.Headers.ContentLength = null;

        response.MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData("/graphql/?sdl")]
    [InlineData("/graphql/schema/")]
    [InlineData("/graphql/schema.graphql")]
    [InlineData("/graphql/schema")]
    public async Task Download_GraphQL_Schema_Not_Allowed(string path)
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyServerOptions(o => o.EnableSchemaFileSupport = false));

        var url = TestServerExtensions.CreateUrl(path);
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Download_GraphQL_Types_SDL()
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql?sdl&types=Query");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_Types_SDL_Character_and_Query()
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql?sdl&types=Character,Query");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_Types_SDL_Type_Not_Found()
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql?sdl&types=Xyz");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_Types_SDL_Types_Empty()
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql?sdl&types=");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_Types_SDL_Invalid_TypeName()
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql?sdl&types=Xyz.Abc");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_SDL_Explicit_Route()
    {
        // arrange
        var server = CreateServer(b => b.MapGraphQLSchema());
        var url = TestServerExtensions.CreateUrl("/graphql/sdl");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_SDL_Explicit_Route_Explicit_Pattern()
    {
        // arrange
        var server = CreateServer(b => b.MapGraphQLSchema("/foo/bar"));
        var url = TestServerExtensions.CreateUrl("/foo/bar");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_SDL_Disabled()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyServerOptions(o =>
                {
                    o.EnableSchemaRequests = false;
                    o.Tool.Enable = false;
                }),
            configureConventions: e => e.WithOptions(o => o.Enable = false));
        var url = TestServerExtensions.CreateUrl("/graphql?sdl");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_Schema_Does_Not_Include_Internal_Directives()
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
                .UseEndpoints(endpoints => endpoints.MapGraphQLSchema()));
        var url = TestServerExtensions.CreateUrl("/graphql/sdl");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_Schema_Includes_Internal_Directives_When_DisableInternalDirectives_Is_True()
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
                .UseEndpoints(endpoints => endpoints.MapGraphQLSchema()));
        var url = TestServerExtensions.CreateUrl("/graphql/sdl");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        result.MatchSnapshot();
    }

    [Theory]
    [InlineData("/graphql/schema.graphql", "october-2021")]
    [InlineData("/graphql/schema.graphql", "2021-10")]
    [InlineData("/graphql/schema.graphql", "september-2025")]
    [InlineData("/graphql/schema.graphql", "SEPTEMBER-2025")]
    [InlineData("/graphql/schema", "october-2021")]
    [InlineData("/graphql/schema", "2021-10")]
    [InlineData("/graphql/schema", "september-2025")]
    [InlineData("/graphql/schema", "SEPTEMBER-2025")]
    [InlineData("/graphql/schema/", "october-2021")]
    [InlineData("/graphql/schema/", "2021-10")]
    [InlineData("/graphql/schema/", "september-2025")]
    [InlineData("/graphql/schema/", "SEPTEMBER-2025")]
    public async Task Download_GraphQL_Schema_Should_Remove_DirectiveDefinition_When_SpecVersionIsSpecified(
        string path,
        string specVersion)
    {
        // arrange
        var server = CreateTaggedStarWarsServer();
        var url = TestServerExtensions.CreateUrl($"{path}?spec-version={specVersion}");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(result.Contains("DIRECTIVE_DEFINITION", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Download_GraphQL_Schema_Should_ReturnVersionedSchema_When_SpecVersionIsSpecified()
    {
        // arrange
        var server = CreateTaggedStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql/schema.graphql?spec-version=october-2021");
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
    public async Task Download_GraphQL_Schema_Should_UseDifferentEtag_When_SpecVersionIsSpecified()
    {
        // arrange
        var server = CreateTaggedStarWarsServer();
        var client = server.CreateClient();
        var nativeUrl = TestServerExtensions.CreateUrl("/graphql/schema.graphql");
        var versionedUrl = TestServerExtensions.CreateUrl("/graphql/schema.graphql?spec-version=october-2021");

        // act
        var nativeResponse = await client.GetAsync(nativeUrl, TestContext.Current.CancellationToken);
        var versionedResponse = await client.GetAsync(versionedUrl, TestContext.Current.CancellationToken);
        var nativeResult = await nativeResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var versionedResult = await versionedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Contains("DIRECTIVE_DEFINITION", nativeResult, StringComparison.Ordinal);
        Assert.False(versionedResult.Contains("DIRECTIVE_DEFINITION", StringComparison.Ordinal));
        Assert.NotEqual(nativeResponse.Headers.ETag, versionedResponse.Headers.ETag);
    }

    [Theory]
    [InlineData("invalid", "unknown")]
    [InlineData("", "empty")]
    [InlineData("   ", "whitespace")]
    public async Task Download_GraphQL_Schema_Should_ReturnBadRequest_When_SpecVersionIsInvalid(
        string specVersion,
        string snapshotPostFix)
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl(
            $"/graphql/schema.graphql?spec-version={Uri.EscapeDataString(specVersion)}");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        response.MatchMarkdownSnapshot(postFix: snapshotPostFix);
    }

    [Fact]
    public async Task Download_GraphQL_Types_SDL_Should_IgnoreSpecVersion_When_TypesAreSpecified()
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql?sdl&types=Query&spec-version=bogus");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private TestServer CreateTaggedStarWarsServer()
        => CreateStarWarsServer(
            configureServices: services =>
                services
                    .AddGraphQLServer()
                    .ConfigureSchemaServices(schemaServices =>
                        schemaServices
                            .RemoveAll<ITimeProvider>()
                            .AddSingleton<ITimeProvider, StaticTimeProvider>())
                    .AddTypeExtension<TaggedQueryExtension>());

    private sealed class TaggedQueryExtension : ObjectTypeExtension
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query").Tag("schema-download");
        }
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
