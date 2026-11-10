using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
#if NET7_0_OR_GREATER
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
#endif
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Caching.Http.Tests;

public class HttpCachingTests : ServerTestBase
{
    public HttpCachingTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task MaxAge_NonZero_Should_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                    .Field("field")
                    .Resolve("")
                    .CacheControl(2000));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task MaxAge_Zero_Should_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                    .Field("field")
                    .Resolve("")
                    .CacheControl(0));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task JustScope_Should_Not_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                    .Field("field")
                    .Resolve("")
                    .CacheControl(scope: CacheControlScope.Private));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task MaxAgeAndScope_Should_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                    .Field("field")
                    .Resolve("")
                    .CacheControl(2000, CacheControlScope.Private));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Theory]
    [InlineData("""{ node(id: "UHJvZHVjdAppMQ==") { ... on Product { name } } }""")]
    [InlineData("""{ nodes(ids: ["UHJvZHVjdAppMQ=="]) { ... on Product { name } } }""")]
    public async Task NodeFields_Should_UseDefaultMaxAge_When_ApplyDefaultsIsEnabled(string query)
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddGlobalObjectIdentification()
                .AddQueryType<ProductQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync(query);

        // assert
        Assert.Equal(TimeSpan.Zero, result.Headers.CacheControl?.MaxAge);
    }

#if NET7_0_OR_GREATER
    [Fact]
    public async Task EntitiesField_Should_UseDefaultMaxAge_When_ApplyDefaultsIsEnabled()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddApolloFederation()
                .AddQueryType<ReviewQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync(
            """
            {
                _entities(representations: [{ __typename: "Review", id: 1 }]) {
                    ... on Review {
                        body
                    }
                }
            }
            """);

        // assert
        Assert.Equal(TimeSpan.Zero, result.Headers.CacheControl?.MaxAge);
    }

    [Fact]
    public async Task ServiceField_Should_UseDefaultMaxAge_When_ApplyDefaultsIsEnabled()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddApolloFederation()
                .AddQueryType<ReviewQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ _service { sdl } }");

        // assert
        Assert.Equal(TimeSpan.Zero, result.Headers.CacheControl?.MaxAge);
    }
#endif

    [Fact]
    public async Task RootField_Should_UseDefaultMaxAge_When_CacheControlDeclaresOnlyScope()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddQueryType<StoreQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ name profile }");

        // assert
        Assert.Equal("max-age=0, private", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task RootField_Should_UseReturnTypeMaxAge_When_CacheControlDeclaresOnlyScope()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddQueryType<ViewerQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ me { email } }");

        // assert
        Assert.Equal("max-age=60, private", result.Headers.CacheControl?.ToString());
    }

    [Node]
    public class Product
    {
        public int Id { get; init; }

        [CacheControl(3600)]
        public string Name { get; init; } = default!;

        [NodeResolver]
        public static Product Get(int id) => new() { Id = id, Name = $"Product {id}" };
    }

    public class ProductQuery
    {
        public Product GetProduct(int id) => Product.Get(id);
    }

#if NET7_0_OR_GREATER
    [Key("id")]
    public class Review
    {
        public int Id { get; init; }

        [CacheControl(3600)]
        public string Body { get; init; } = default!;

        [ReferenceResolver]
        public static Review Get(int id) => new() { Id = id, Body = $"Review {id}" };
    }

    public class ReviewQuery
    {
        public Review GetReview(int id) => Review.Get(id);
    }
#endif

    public class StoreQuery
    {
        [CacheControl(3600)]
        public string GetName() => "Main";

        [CacheControl(Scope = CacheControlScope.Private)]
        public string GetProfile() => "profile";
    }

    [CacheControl(60)]
    public class User
    {
        public string Email => "alice@example.com";
    }

    public class ViewerQuery
    {
        [CacheControl(Scope = CacheControlScope.Private)]
        public User? GetMe() => new();
    }
}

public class GraphQLResult
{
    public HttpResponseHeaders Headers { get; set; } = default!;

    public HttpContentHeaders ContentHeaders { get; set; } = default!;

    public string Body { get; set; } = default!;
}

internal static class TestServerExtensions
{
    public static async Task<GraphQLResult> GetQueryAsync(this HttpClient client, string query)
    {
        var response = await client.GetAsync("/graphql?query=" + Uri.EscapeDataString(query));

        return new GraphQLResult
        {
            Headers = response.Headers,
            ContentHeaders = response.Content.Headers,
            Body = await response.Content.ReadAsStringAsync()
        };
    }

    public static async Task<GraphQLResult> PostQueryAsync(this HttpClient client, string query)
    {
        var payload = $"{{ \"query\": \"{query}\" }}";

        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/graphql", content);

        var result = new GraphQLResult
        {
            Headers = response.Headers,
            ContentHeaders = response.Content.Headers,
            Body = await response.Content.ReadAsStringAsync()
        };

        return result;
    }
}
