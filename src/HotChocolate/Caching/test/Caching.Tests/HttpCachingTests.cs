using System.Net.Http.Headers;
using System.Text;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

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
                .UseQueryCache()
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
                .UseQueryCache()
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

    [Theory]
    [InlineData(60, 30)]
    [InlineData(30, 60)]
    [InlineData(30, 30)]
    [InlineData(30, 3000)]
    public async Task MaxAge_Multiple_Should_Cache_Shortest_Time(int time1, int time2)
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(time1);
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(time2);
                });
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field1, field2 }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task MaxAge_Multiple_Combine_Public_Private_Caches_Private()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(30);
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(60, CacheControlScope.Private);
                });
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field1, field2 }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task SharedMaxAge_Multiple_Combine_Public_Private_Caches_Private()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(sharedMaxAge: 60);
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(scope: CacheControlScope.Private, sharedMaxAge: 30);
                });
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field1, field2 }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task SharedMaxAge_MaxAge_Combine_Produces_Resolved_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(maxAge: 0, sharedMaxAge: 60);
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(maxAge: 30);
                });
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field1, field2 }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task MaxAge_SharedMaxAge_Combine_Produces_Resolved_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(maxAge: 30);
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(maxAge: 0, sharedMaxAge: 60);
                });
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field1, field2 }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Just_Defaults_Should_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Resolve(""));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task No_Applied_Defaults_Should_Not_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Resolve(""));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Default_Max_Age_Should_Apply_And_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.DefaultMaxAge = 1000)
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Resolve(""));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Default_Scope_Should_Apply_And_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.DefaultScope = CacheControlScope.Private)
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Resolve(""));
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
                .UseQueryCache()
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
                .UseQueryCache()
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

    [Fact]
    public async Task QueryError_Should_Not_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Type<StringType>()
                        .Resolve(_ => throw new Exception()));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task SharedMaxAgeAndScope_Should_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Resolve("")
                        .CacheControl(scope: CacheControlScope.Public, sharedMaxAge: 2000));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("QUERY")]
    public async Task SharedMaxAgeAndVary_Should_Cache(string method)
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .ModifyServerOptions(o => o.EnableQueryRequests = true)
                .UseQueryCache()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Resolve("")
                        .CacheControl(sharedMaxAge: 2000, vary: ["X-foo", "X-BaR"]));
        });

        var client = server.CreateClient();
        var result = await client.SendQueryAsync(new HttpMethod(method), "{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task SharedMaxAgeAndVary_Multiple_Should_Cache_And_Combine()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(sharedMaxAge: 2000, vary: ["X-foo", "X-BaR"]);
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(sharedMaxAge: 1000, vary: ["X-FAR", "X-BaR"]);
                });
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field1, field2 }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task SharedMaxAgeAndVary_Should_ListAcceptOnce_When_VaryNamesAccept()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Resolve("")
                        .CacheControl(sharedMaxAge: 2000, vary: ["Accept", "X-foo"]));
        });
        var client = server.CreateClient();

        // act
        var result = await client.PostQueryAsync("{ field }");

        // assert
        Assert.Equal(["Accept", "x-foo"], result.Headers.Vary);
    }

    [Theory]
    [InlineData(
        """{ node(id: "UHJvZHVjdDox") { ... on Product { name } } }""",
        """{"data":{"node":{"name":"Product 1"}}}""")]
    [InlineData(
        """{ nodes(ids: ["UHJvZHVjdDox"]) { ... on Product { name } } }""",
        """{"data":{"nodes":[{"name":"Product 1"}]}}""")]
    public async Task NodeFields_Should_UseDefaultMaxAge_When_ApplyDefaultsIsEnabled(
        string query,
        string expectedBody)
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .AddGlobalObjectIdentification()
                .AddQueryType<ProductQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync(query);

        // assert
        Assert.Equal(expectedBody, result.Body);
        Assert.Equal("max-age=0", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task EntitiesField_Should_UseDefaultMaxAge_When_ApplyDefaultsIsEnabled()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
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
        Assert.Equal("""{"data":{"_entities":[{"body":"Review 1"}]}}""", result.Body);
        Assert.Equal("max-age=0", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task ServiceField_Should_UseDefaultMaxAge_When_ApplyDefaultsIsEnabled()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .AddApolloFederation()
                .AddQueryType<ReviewQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ _service { sdl } }");

        // assert
        Assert.Equal("max-age=0", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task PagingField_Should_UseDefaultMaxAge_When_ResolverIsSynchronous()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .AddQueryType<CatalogQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ catalog { tags { nodes } } }");

        // assert
        Assert.Equal("""{"data":{"catalog":{"tags":{"nodes":["new"]}}}}""", result.Body);
        Assert.Equal("max-age=0", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task RootField_Should_UseDefaultMaxAge_When_CacheControlDeclaresOnlyScope()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .AddQueryType<StoreQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ name profile }");

        // assert
        Assert.Equal("""{"data":{"name":"Main","profile":"profile"}}""", result.Body);
        Assert.Equal("max-age=0, private", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task RootField_Should_UseReturnTypeMaxAge_When_CacheControlDeclaresOnlyScope()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCache()
                .AddCacheControl()
                .AddQueryType<ViewerQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ me { email } }");

        // assert
        Assert.Equal("""{"data":{"me":{"email":"alice@example.com"}}}""", result.Body);
        Assert.Equal("max-age=60, private", result.Headers.CacheControl?.ToString());
    }

    public class Catalog
    {
        [UsePaging]
        public List<string> GetTags() => ["new"];
    }

    public class CatalogQuery
    {
        [CacheControl(3600)]
        public Catalog GetCatalog() => new();
    }

    [Node]
    public class Product
    {
        public int Id { get; init; }

        [CacheControl(3600)]
        public string Name { get; init; } = null!;

        [NodeResolver]
        public static Product Get(int id) => new() { Id = id, Name = $"Product {id}" };
    }

    public class ProductQuery
    {
        public Product GetProduct(int id) => Product.Get(id);
    }

    [Key("id")]
    public class Review
    {
        public int Id { get; init; }

        [CacheControl(3600)]
        public string Body { get; init; } = null!;

        [ReferenceResolver]
        public static Review Get(int id) => new() { Id = id, Body = $"Review {id}" };
    }

    public class ReviewQuery
    {
        public Review GetReview(int id) => Review.Get(id);
    }

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
    public HttpResponseHeaders Headers { get; set; } = null!;

    public HttpContentHeaders ContentHeaders { get; set; } = null!;

    public string Body { get; set; } = null!;
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

    public static Task<GraphQLResult> PostQueryAsync(this HttpClient client, string query)
        => client.SendQueryAsync(HttpMethod.Post, query);

    public static async Task<GraphQLResult> SendQueryAsync(
        this HttpClient client,
        HttpMethod method,
        string query)
    {
        var payload = $"{{ \"query\": \"{query}\" }}";

        using var request = new HttpRequestMessage(method, "/graphql")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);

        var result = new GraphQLResult
        {
            Headers = response.Headers,
            ContentHeaders = response.Content.Headers,
            Body = await response.Content.ReadAsStringAsync()
        };

        return result;
    }
}
