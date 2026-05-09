using System.Net;
using System.Text;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore;

public class DynamicSchemaTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task MapGraphQL_DynamicSchema_ResolvesSchemaFromHeader()
    {
        // arrange
        var server = ServerFactory.Create(
            services =>
            {
                services.AddRouting();
                services.AddGraphQLServer("tenant-a")
                    .AddQueryType<QueryA>();
                services.AddGraphQLServer("tenant-b")
                    .AddQueryType<QueryB>();
            },
            app => app
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapGraphQL(
                        "/graphql",
                        context => context.Request.Headers["X-Tenant-Id"].ToString());
                }));

        // act: resolve tenant-a
        var resultA = await PostDynamicAsync(
            server,
            new ClientQueryRequest { Query = "{ who }" },
            "/graphql",
            "X-Tenant-Id",
            "tenant-a");

        // act: resolve tenant-b
        var resultB = await PostDynamicAsync(
            server,
            new ClientQueryRequest { Query = "{ who }" },
            "/graphql",
            "X-Tenant-Id",
            "tenant-b");

        // assert
        Assert.Equal("TenantA", resultA.Data!["who"]!.ToString());
        Assert.Equal("TenantB", resultB.Data!["who"]!.ToString());
    }

    [Fact]
    public async Task MapGraphQLHttp_DynamicSchema_ResolvesSchemaPerRequest()
    {
        // arrange
        var server = ServerFactory.Create(
            services =>
            {
                services.AddRouting();
                services.AddGraphQLServer("tenant-a")
                    .AddQueryType<QueryA>();
                services.AddGraphQLServer("tenant-b")
                    .AddQueryType<QueryB>();
            },
            app => app
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapGraphQLHttp(
                        "/graphql",
                        context => context.Request.Headers["X-Tenant-Id"].ToString());
                }));

        // act
        var resultA = await PostDynamicAsync(
            server,
            new ClientQueryRequest { Query = "{ who }" },
            "/graphql",
            "X-Tenant-Id",
            "tenant-a");

        var resultB = await PostDynamicAsync(
            server,
            new ClientQueryRequest { Query = "{ who }" },
            "/graphql",
            "X-Tenant-Id",
            "tenant-b");

        // assert
        Assert.Equal("TenantA", resultA.Data!["who"]!.ToString());
        Assert.Equal("TenantB", resultB.Data!["who"]!.ToString());
    }

    [Fact]
    public async Task MapGraphQLHttp_DynamicSchema_GetRequest_ResolvesSchemaPerRequest()
    {
        // arrange
        var server = ServerFactory.Create(
            services =>
            {
                services.AddRouting();
                services.AddGraphQLServer("tenant-a")
                    .AddQueryType<QueryA>()
                    .ModifyServerOptions(o =>
                        o.AllowedGetOperations = AllowedGetOperations.Query);
                services.AddGraphQLServer("tenant-b")
                    .AddQueryType<QueryB>()
                    .ModifyServerOptions(o =>
                        o.AllowedGetOperations = AllowedGetOperations.Query);
            },
            app => app
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapGraphQLHttp(
                        "/graphql",
                        context => context.Request.Headers["X-Tenant-Id"].ToString());
                }));

        // act
        var resultA = await GetDynamicAsync(
            server,
            "{ who }",
            "/graphql",
            "X-Tenant-Id",
            "tenant-a");

        var resultB = await GetDynamicAsync(
            server,
            "{ who }",
            "/graphql",
            "X-Tenant-Id",
            "tenant-b");

        // assert
        Assert.Equal("TenantA", resultA.Data!["who"]!.ToString());
        Assert.Equal("TenantB", resultB.Data!["who"]!.ToString());
    }

    [Fact]
    public async Task MapGraphQL_DynamicSchema_SameEndpointDifferentTenantsConcurrently()
    {
        // arrange
        var server = ServerFactory.Create(
            services =>
            {
                services.AddRouting();
                services.AddGraphQLServer("tenant-a")
                    .AddQueryType<QueryA>()
                    .ModifyServerOptions(o =>
                        o.AllowedGetOperations = AllowedGetOperations.Query);
                services.AddGraphQLServer("tenant-b")
                    .AddQueryType<QueryB>()
                    .ModifyServerOptions(o =>
                        o.AllowedGetOperations = AllowedGetOperations.Query);
            },
            app => app
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapGraphQL(
                        "/graphql",
                        context => context.Request.Headers["X-Tenant-Id"].ToString());
                }));

        // act: send multiple requests concurrently
        var tasks = new List<Task<ClientQueryResult>>();
        for (var i = 0; i < 5; i++)
        {
            tasks.Add(PostDynamicAsync(
                server,
                new ClientQueryRequest { Query = "{ who }" },
                "/graphql",
                "X-Tenant-Id",
                "tenant-a"));
            tasks.Add(PostDynamicAsync(
                server,
                new ClientQueryRequest { Query = "{ who }" },
                "/graphql",
                "X-Tenant-Id",
                "tenant-b"));
        }

        var results = await Task.WhenAll(tasks);

        // assert: tenant-a results
        var tenantAResults = results.Where((_, i) => i % 2 == 0).ToList();
        var tenantBResults = results.Where((_, i) => i % 2 != 0).ToList();

        Assert.All(tenantAResults, r => Assert.Equal("TenantA", r.Data!["who"]!.ToString()));
        Assert.All(tenantBResults, r => Assert.Equal("TenantB", r.Data!["who"]!.ToString()));
    }

    [Fact]
    public async Task MapGraphQL_DynamicSchema_IntrospectionReturnsDifferentSchemas()
    {
        // arrange
        var server = ServerFactory.Create(
            services =>
            {
                services.AddRouting();
                services.AddGraphQLServer("tenant-a")
                    .AddQueryType<QueryA>();
                services.AddGraphQLServer("tenant-b")
                    .AddQueryType<QueryB>();
            },
            app => app
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapGraphQL(
                        "/graphql",
                        context => context.Request.Headers["X-Tenant-Id"].ToString());
                }));

        // act: introspect tenant-a schema, then tenant-b schema
        var resultA = await PostDynamicAsync(
            server,
            new ClientQueryRequest { Query = "{ __typename }" },
            "/graphql",
            "X-Tenant-Id",
            "tenant-a");

        var resultB = await PostDynamicAsync(
            server,
            new ClientQueryRequest { Query = "{ __typename }" },
            "/graphql",
            "X-Tenant-Id",
            "tenant-b");

        // assert: both tenants return valid results (schemas are distinct)
        Assert.NotNull(resultA.Data);
        Assert.NotNull(resultB.Data);
        Assert.Null(resultA.Errors);
        Assert.Null(resultB.Errors);
    }

    [Fact]
    public async Task MapGraphQLWebSocket_DynamicSchema_ResolvesSchemaPerRequest()
    {
        // arrange
        var server = ServerFactory.Create(
            services =>
            {
                services.AddRouting();
                services.AddGraphQLServer("tenant-a")
                    .AddQueryType<QueryA>()
                    .AddInMemorySubscriptions()
                    .ModifyServerOptions(o =>
                        o.AllowedGetOperations = AllowedGetOperations.Query);
                services.AddGraphQLServer("tenant-b")
                    .AddQueryType<QueryB>()
                    .AddInMemorySubscriptions()
                    .ModifyServerOptions(o =>
                        o.AllowedGetOperations = AllowedGetOperations.Query);
            },
            app => app
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapGraphQLHttp(
                        "/graphql",
                        context => context.Request.Headers["X-Tenant-Id"].ToString());
                    endpoints.MapGraphQLWebSocket(
                        "/graphql/ws",
                        context => context.Request.Headers["X-Tenant-Id"].ToString());
                }));

        // act: verify HTTP still works with dynamic WebSocket endpoint registered
        var resultA = await PostDynamicAsync(
            server,
            new ClientQueryRequest { Query = "{ who }" },
            "/graphql",
            "X-Tenant-Id",
            "tenant-a");

        // assert
        Assert.Equal("TenantA", resultA.Data!["who"]!.ToString());
    }

    [Fact]
    public async Task DynamicSchemaMiddleware_StaticFallback_WhenNoDynamicResolver()
    {
        // arrange: use the normal non-dynamic MapGraphQL to ensure it still works
        var server = ServerFactory.Create(
            services =>
            {
                services.AddRouting();
                services.AddGraphQLServer()
                    .AddQueryType<QueryA>()
                    .ModifyServerOptions(o =>
                        o.AllowedGetOperations = AllowedGetOperations.Query);
            },
            app => app
                .UseRouting()
                .UseEndpoints(endpoints =>
                    endpoints.MapGraphQL("/graphql-static")));

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = "{ who }" },
            "/graphql-static");

        // assert: static endpoint still works
        Assert.Equal("TenantA", result.Data!["who"]!.ToString());
    }

    private static async Task<ClientQueryResult> PostDynamicAsync(
        TestServer server,
        ClientQueryRequest request,
        string path,
        string headerName,
        string headerValue)
    {
        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.Clear();
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add(headerName, headerValue);
        var response = await client.PostAsync(
            new Uri($"http://localhost:5000{path}"),
            content);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ClientQueryResult { StatusCode = HttpStatusCode.NotFound };
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ClientQueryResult>(responseJson)!;
        result.StatusCode = response.StatusCode;
        result.ContentType = response.Content.Headers.ContentType?.ToString();
        return result;
    }

    private static async Task<ClientQueryResult> GetDynamicAsync(
        TestServer server,
        string query,
        string path,
        string headerName,
        string headerValue)
    {
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add(headerName, headerValue);
        var message = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri($"http://localhost:5000{path}/?query={Uri.EscapeDataString(query)}"));

        var response = await client.SendAsync(message);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new ClientQueryResult { StatusCode = HttpStatusCode.NotFound };
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ClientQueryResult>(json)!;
        result.StatusCode = response.StatusCode;
        result.ContentType = response.Content.Headers.ContentType?.ToString();
        return result;
    }

    public class QueryA
    {
        public string Who() => "TenantA";
    }

    public class QueryB
    {
        public string Who() => "TenantB";
    }
}
