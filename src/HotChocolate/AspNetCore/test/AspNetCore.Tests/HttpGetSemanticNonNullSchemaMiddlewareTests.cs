using System.Net;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
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

    [Theory]
    [InlineData("october-2021")]
    [InlineData("2021-10")]
    [InlineData("september-2025")]
    [InlineData("SEPTEMBER-2025")]
    public async Task Download_GraphQL_SemanticNonNull_Schema_Should_Remove_DirectiveDefinition_When_SpecVersionIsSpecified(
        string specVersion)
    {
        // arrange
        var server = CreateTaggedStarWarsServer();
        var url = TestServerExtensions.CreateUrl(
            $"/graphql/semantic-non-null-schema.graphql?spec-version={specVersion}");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("@semanticNonNull", result, StringComparison.Ordinal);
        Assert.False(result.Contains("DIRECTIVE_DEFINITION", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Download_GraphQL_SemanticNonNull_Schema_Should_ReturnVersionedSchema_When_SpecVersionIsSpecified()
    {
        // arrange
        var server = CreateTaggedStarWarsServer();
        var url = TestServerExtensions.CreateUrl(
            "/graphql/semantic-non-null-schema.graphql?spec-version=october-2021");
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
    public async Task Download_GraphQL_SemanticNonNull_Schema_Should_UseDifferentEtag_When_SpecVersionIsSpecified()
    {
        // arrange
        var server = CreateTaggedStarWarsServer();
        var client = server.CreateClient();
        var nativeUrl = TestServerExtensions.CreateUrl("/graphql/semantic-non-null-schema.graphql");
        var versionedUrl = TestServerExtensions.CreateUrl(
            "/graphql/semantic-non-null-schema.graphql?spec-version=october-2021");

        // act
        var nativeResponse = await client.GetAsync(nativeUrl, TestContext.Current.CancellationToken);
        var versionedResponse = await client.GetAsync(versionedUrl, TestContext.Current.CancellationToken);

        // assert
        Assert.NotEqual(nativeResponse.Headers.ETag, versionedResponse.Headers.ETag);
    }

    [Theory]
    [InlineData("invalid", "unknown")]
    [InlineData("", "empty")]
    [InlineData("   ", "whitespace")]
    public async Task Download_GraphQL_SemanticNonNull_Schema_Should_ReturnBadRequest_When_SpecVersionIsInvalid(
        string specVersion,
        string snapshotPostFix)
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl(
            $"/graphql/semantic-non-null-schema.graphql?spec-version={Uri.EscapeDataString(specVersion)}");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        response.MatchMarkdownSnapshot(postFix: snapshotPostFix);
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
