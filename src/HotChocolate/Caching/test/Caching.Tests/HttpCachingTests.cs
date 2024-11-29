using System.Net.Http.Headers;
using System.Text;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
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
                .UseQueryCachePipeline()
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
                .UseQueryCachePipeline()
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
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(sharedMaxAge:60);
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(scope:CacheControlScope.Private, sharedMaxAge:30);
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
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(maxAge: 0, sharedMaxAge:60);
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(maxAge:30);
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
                .UseQueryCachePipeline()
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
                        .CacheControl(maxAge: 0, sharedMaxAge:60);
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
                .UseQueryCachePipeline()
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
                .UseQueryCachePipeline()
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
                .UseQueryCachePipeline()
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
                .UseQueryCachePipeline()
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

    [Fact]
    public async Task QueryError_Should_Not_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
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
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Resolve("")
                        .CacheControl(sharedMaxAge: 2000, scope: CacheControlScope.Public));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task SharedMaxAgeAndVary_Should_Cache()
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
                        .CacheControl(sharedMaxAge: 2000, vary: new[] { "X-foo", "X-BaR" }));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task SharedMaxAgeAndVary_Multiple_Should_Cache_And_Combine()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(sharedMaxAge: 2000, vary: new[] {"X-foo", "X-BaR"});
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(sharedMaxAge: 1000, vary: new[] {"X-FAR", "X-BaR"});
                });
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field1, field2 }");

        result.MatchSnapshot();
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
    public static async Task<GraphQLResult> PostQueryAsync(this HttpClient client, string query)
    {
        var payload = $"{{ \"query\": \"{query}\" }}";

        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/graphql", content);

        var result = new GraphQLResult
        {
            Headers = response.Headers,
            ContentHeaders = response.Content.Headers,
            Body = await response.Content.ReadAsStringAsync(),
        };

        return result;
    }
}
