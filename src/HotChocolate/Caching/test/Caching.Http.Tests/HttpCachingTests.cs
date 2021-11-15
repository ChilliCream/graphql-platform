using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
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
    public async Task Test()
    {
        TestServer server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryResultCachePipeline()
                .AddHttpQueryCache()
                .AddQueryType<Query>();
        });

        HttpClient client = server.CreateClient();

        GraphQLResult result = await client.PostQueryAsync("{ field1 }");

        result.MatchSnapshot();
    }

    private class Query
    {
        [CacheControl(2000)]
        public string Field1() => "Test";
    }
}

public class GraphQLResult
{
    public HttpResponseHeaders Headers { get; set; }

    public HttpContentHeaders ContentHeaders { get; set; }

    public string Body { get; set; }
}

internal static class TestServerExtensions
{
    public static async Task<GraphQLResult> PostQueryAsync(this HttpClient client, string query)
    {
        var payload = $"{{ \"query\": \"{query}\" }}";

        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("/graphql", content);

        var result = new GraphQLResult
        {
            Headers = response.Headers,
            ContentHeaders = response.Content.Headers,
            Body = await response.Content.ReadAsStringAsync()
        };

        return result;
    }
}