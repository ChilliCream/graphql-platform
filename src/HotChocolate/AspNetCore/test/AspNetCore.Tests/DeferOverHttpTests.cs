using System.Net;
using System.Net.Http.Json;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

public class DeferOverHttpTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task Simple_Defer_Multipart()
    {
        // arrange
        using var server = CreateDeferServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = JsonContent.Create(new
        {
            query = """
                {
                    product {
                        name
                        ... @defer {
                            description
                        }
                    }
                }
                """
        });
        request.Headers.Add("Accept", "multipart/mixed");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("multipart/mixed", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """

                ---
                Content-Type: application/json; charset=utf-8

                {"data":{"product":{"name":"Abc","description":null}},"pending":[{"id":2,"path":["product"]}],"hasNext":true}
                ---
                Content-Type: application/json; charset=utf-8

                {"incremental":[{"id":2,"data":{"description":"Abc desc"}}],"completed":[{"id":2}],"hasNext":false}
                -----

                """);
    }

    [Fact]
    public async Task Simple_Defer_EventStream()
    {
        // arrange
        using var server = CreateDeferServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = JsonContent.Create(new
        {
            query = """
                {
                    product {
                        name
                        ... @defer {
                            description
                        }
                    }
                }
                """
        });
        request.Headers.Add("Accept", "text/event-stream");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                Response:
                -------------------------->
                TODO: Add expected snapshot
                """);
    }

    [Fact]
    public async Task Defer_List_Multipart()
    {
        // arrange
        using var server = CreateDeferServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = JsonContent.Create(new
        {
            query = """
                {
                    products {
                        name
                        ... @defer(label: "desc") {
                            description
                        }
                    }
                }
                """
        });
        request.Headers.Add("Accept", "multipart/mixed");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                Response:
                -------------------------->
                TODO: Add expected snapshot
                """);
    }

    [Fact]
    public async Task Defer_With_Label_Multipart()
    {
        // arrange
        using var server = CreateDeferServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = JsonContent.Create(new
        {
            query = """
                {
                    product {
                        name
                        ... @defer(label: "productDescription") {
                            description
                        }
                    }
                }
                """
        });
        request.Headers.Add("Accept", "multipart/mixed");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // assert
        var content = await response.Content.ReadAsStringAsync();

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                Response:
                -------------------------->
                TODO: Add expected snapshot
                """);
    }

    [Fact]
    public async Task Defer_Disabled_By_Variable()
    {
        // arrange
        using var server = CreateDeferServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = JsonContent.Create(new
        {
            query = """
                query($shouldDefer: Boolean!) {
                    product {
                        name
                        ... @defer(if: $shouldDefer) {
                            description
                        }
                    }
                }
                """,
            variables = new { shouldDefer = false }
        });
        request.Headers.Add("Accept", "multipart/mixed");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // When defer is disabled, should get a regular JSON response, not multipart
        var content = await response.Content.ReadAsStringAsync();

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                Response:
                -------------------------->
                TODO: Add expected snapshot
                """);
    }

    private TestServer CreateDeferServer(HttpTransportVersion serverTransportVersion = HttpTransportVersion.Latest)
    {
        return ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .AddTypeExtension<Query>()
                .AddDefaultBatchDispatcher()
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = serverTransportVersion
                    })
                .ModifyOptions(o =>
                    {
                        o.EnableDefer = true;
                        o.EnableStream = true;
                    }),
            app => app
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
    }

    public sealed class Query
    {
        public Product GetProduct()
            => new("Abc");

        public IEnumerable<Product> GetProducts()
        {
            yield return new Product("Abc");
            yield return new Product("Def");
            yield return new Product("Ghi");
        }
    }

    public sealed record Product(string Name)
    {
        public async Task<string> GetDescriptionAsync()
        {
            await Task.Delay(1000);
            return Name + " desc";
        }
    }
}
