using System.Net;
using System.Text;
#if !NET11_0_OR_GREATER
using System.Net.Http.Json;
#endif
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

public class DeferOverHttpTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Theory]
    [InlineData(null)]
    [InlineData("*/*")]
    [InlineData("multipart/mixed")]
    [InlineData("multipart/*")]
    [InlineData("application/graphql-response+json, multipart/mixed")]
    [InlineData("text/event-stream, multipart/mixed")]
    public async Task Simple_Defer_Multipart(string? acceptHeader)
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

        if (acceptHeader is not null)
        {
            request.Headers.Add("Accept", acceptHeader);
        }

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("multipart/mixed", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """

                ---
                Content-Type: application/json; charset=utf-8

                {"data":{"product":{"name":"Abc"}},"pending":[{"id":"2","path":["product"]}],"hasNext":true}
                ---
                Content-Type: application/json; charset=utf-8

                {"incremental":[{"id":"2","data":{"description":"Abc desc"}}],"completed":[{"id":"2"}],"hasNext":false}
                -----

                """);
    }

    [Theory]
    [InlineData("multipart/mixed; incrementalSpec=v0.1; charset=utf-8")]
    [InlineData("application/graphql-response+json, multipart/mixed; incrementalSpec=v0.1")]
    public async Task Simple_Defer_Multipart_IncrementalSpec_V0_1(string acceptHeader)
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
        request.Headers.Add("Accept", acceptHeader);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("multipart/mixed", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """

                ---
                Content-Type: application/json; charset=utf-8

                {"data":{"product":{"name":"Abc"}},"hasNext":true}
                ---
                Content-Type: application/json; charset=utf-8

                {"incremental":[{"data":{"description":"Abc desc"},"path":["product"]}],"hasNext":false}
                -----

                """);
    }

    [Theory]
    [InlineData("multipart/mixed; incrementalSpec=v0.2; charset=utf-8")]
    [InlineData("multipart/mixed; incrementalSpec=\"v0.2\"; charset=utf-8")]
    public async Task Simple_Defer_Multipart_IncrementalSpec_V0_2(string acceptHeader)
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
        request.Headers.Add("Accept", acceptHeader);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("multipart/mixed", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """

                ---
                Content-Type: application/json; charset=utf-8

                {"data":{"product":{"name":"Abc"}},"pending":[{"id":"2","path":["product"]}],"hasNext":true}
                ---
                Content-Type: application/json; charset=utf-8

                {"incremental":[{"id":"2","data":{"description":"Abc desc"}}],"completed":[{"id":"2"}],"hasNext":false}
                -----

                """);
    }

    [Theory]
    [InlineData("text/event-stream")]
    [InlineData("application/graphql-response+json, text/event-stream")]
    public async Task Simple_Defer_EventStream(string acceptHeader)
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
        request.Headers.Add("Accept", acceptHeader);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                event: next
                data: {"data":{"product":{"name":"Abc"}},"pending":[{"id":"2","path":["product"]}],"hasNext":true}

                event: next
                data: {"incremental":[{"id":"2","data":{"description":"Abc desc"}}],"completed":[{"id":"2"}],"hasNext":false}

                event: complete


                """);
    }

    [Theory]
    [InlineData("multipart/mixed; incrementalSpec=v0.1")]
    [InlineData("application/graphql-response+json, multipart/mixed; incrementalSpec=v0.1")]
    [InlineData("text/event-stream, multipart/mixed; incrementalSpec=v0.1")]
    public async Task Simple_Defer_Multipart_Legacy_Format(string acceptHeader)
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
        request.Headers.Add("Accept", acceptHeader);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("multipart/mixed", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """

                ---
                Content-Type: application/json; charset=utf-8

                {"data":{"product":{"name":"Abc"}},"hasNext":true}
                ---
                Content-Type: application/json; charset=utf-8

                {"incremental":[{"data":{"description":"Abc desc"},"path":["product"]}],"hasNext":false}
                -----

                """);
    }

    [Theory]
    [InlineData("text/event-stream; incrementalSpec=v0.1")]
    [InlineData("application/graphql-response+json, text/event-stream; incrementalSpec=v0.1")]
    public async Task Simple_Defer_EventStream_Legacy_Format(string acceptHeader)
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
        request.Headers.Add("Accept", acceptHeader);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                event: next
                data: {"data":{"product":{"name":"Abc"}},"hasNext":true}

                event: next
                data: {"incremental":[{"data":{"description":"Abc desc"},"path":["product"]}],"hasNext":false}

                event: complete


                """);
    }

    [Fact]
    public async Task Defer_With_Label_EventStream_Legacy_Format()
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
        request.Headers.Add("Accept", "text/event-stream; incrementalSpec=v0.1");

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                event: next
                data: {"data":{"product":{"name":"Abc"}},"hasNext":true}

                event: next
                data: {"incremental":[{"data":{"description":"Abc desc"},"path":["product"],"label":"productDescription"}],"hasNext":false}

                event: complete


                """);
    }

    [Theory]
    [InlineData("text/event-stream")]
    [InlineData("application/graphql-response+json, text/event-stream")]
    public async Task Defer_With_Label_EventStream(string acceptHeader)
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
        request.Headers.Add("Accept", acceptHeader);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                event: next
                data: {"data":{"product":{"name":"Abc"}},"pending":[{"id":"2","path":["product"],"label":"productDescription"}],"hasNext":true}

                event: next
                data: {"incremental":[{"id":"2","data":{"description":"Abc desc"}}],"completed":[{"id":"2"}],"hasNext":false}

                event: complete


                """);
    }

    [Theory]
    [InlineData("text/event-stream")]
    [InlineData("application/graphql-response+json, text/event-stream")]
    public async Task Defer_TypeCondition_EventStream(string acceptHeader)
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
                    hero {
                        name
                        ... on Droid @defer(label: "droid_details") {
                            primaryFunction
                        }
                    }
                }
                """
        });
        request.Headers.Add("Accept", acceptHeader);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                event: next
                data: {"data":{"hero":{"name":"R2-D2"}},"pending":[{"id":"2","path":["hero"],"label":"droid_details"}],"hasNext":true}

                event: next
                data: {"incremental":[{"id":"2","data":{"primaryFunction":"Astromech"}}],"completed":[{"id":"2"}],"hasNext":false}

                event: complete


                """);
    }

    [Fact]
    public async Task Defer_With_Label_Multipart_Legacy_Format()
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
        request.Headers.Add("Accept", "multipart/mixed; incrementalSpec=v0.1");

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("multipart/mixed", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """

                ---
                Content-Type: application/json; charset=utf-8

                {"data":{"product":{"name":"Abc"}},"hasNext":true}
                ---
                Content-Type: application/json; charset=utf-8

                {"incremental":[{"data":{"description":"Abc desc"},"path":["product"],"label":"productDescription"}],"hasNext":false}
                -----

                """);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("*/*")]
    [InlineData("multipart/mixed")]
    [InlineData("multipart/*")]
    [InlineData("application/graphql-response+json, multipart/mixed")]
    [InlineData("text/event-stream, multipart/mixed")]
    public async Task Defer_With_Label_Multipart(string? acceptHeader)
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

        if (acceptHeader is not null)
        {
            request.Headers.Add("Accept", acceptHeader);
        }

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("multipart/mixed", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """

                ---
                Content-Type: application/json; charset=utf-8

                {"data":{"product":{"name":"Abc"}},"pending":[{"id":"2","path":["product"],"label":"productDescription"}],"hasNext":true}
                ---
                Content-Type: application/json; charset=utf-8

                {"incremental":[{"id":"2","data":{"description":"Abc desc"}}],"completed":[{"id":"2"}],"hasNext":false}
                -----

                """);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("*/*")]
    [InlineData("application/graphql-response+json, multipart/mixed")]
    [InlineData("application/graphql-response+json, text/event-stream, multipart/mixed")]
    public async Task Defer_Disabled_By_Variable(string? acceptHeader)
    {
        // arrange
        using var server = CreateDeferServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = JsonContent.Create(new
        {
            query =
                """
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

        if (acceptHeader is not null)
        {
            request.Headers.Add("Accept", acceptHeader);
        }

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // When defer is disabled, should get a regular JSON response, not multipart
        Assert.Equal("application/graphql-response+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                {"data":{"product":{"name":"Abc","description":"Abc desc"}}}
                """);
    }

    [Fact]
    public async Task Defer_NoStreamableAcceptHeader()
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
        request.Headers.Add("Accept", "application/graphql-response+json");

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        // Should reject the request since we have a deferred result but
        // the user only accepts non-streaming JSON payload
        Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
        Assert.Equal("application/graphql-response+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Empty(response.Content.Headers.Allow);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                {"errors":[{"message":"The client does not accept a response content type that supports incremental delivery."}]}
                """);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("*/*")]
    [InlineData("multipart/mixed")]
    [InlineData("multipart/*")]
    [InlineData("application/graphql-response+json, multipart/mixed")]
    [InlineData("text/event-stream, multipart/mixed")]
    public async Task Defer_TypeCondition_Multipart(string? acceptHeader)
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
                    hero {
                        name
                        ... on Droid @defer(label: "droid_details") {
                            primaryFunction
                        }
                    }
                }
                """
        });

        if (acceptHeader is not null)
        {
            request.Headers.Add("Accept", acceptHeader);
        }

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("multipart/mixed", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """

                ---
                Content-Type: application/json; charset=utf-8

                {"data":{"hero":{"name":"R2-D2"}},"pending":[{"id":"2","path":["hero"],"label":"droid_details"}],"hasNext":true}
                ---
                Content-Type: application/json; charset=utf-8

                {"incremental":[{"id":"2","data":{"primaryFunction":"Astromech"}}],"completed":[{"id":"2"}],"hasNext":false}
                -----

                """);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("*/*")]
    [InlineData("multipart/mixed")]
    [InlineData("multipart/*")]
    [InlineData("application/graphql-response+json, multipart/mixed")]
    [InlineData("text/event-stream, multipart/mixed")]
    public async Task Defer_TypeCondition_If_True(string? acceptHeader)
    {
        // arrange
        using var server = CreateDeferServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = JsonContent.Create(new
        {
            query = """
                query($if: Boolean!) {
                    hero {
                        name
                        ... on Droid @defer(label: "droid_details", if: $if) {
                            primaryFunction
                        }
                    }
                }
                """,
            variables = new { @if = true }
        });

        if (acceptHeader is not null)
        {
            request.Headers.Add("Accept", acceptHeader);
        }

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("multipart/mixed", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """

                ---
                Content-Type: application/json; charset=utf-8

                {"data":{"hero":{"name":"R2-D2"}},"pending":[{"id":"2","path":["hero"],"label":"droid_details"}],"hasNext":true}
                ---
                Content-Type: application/json; charset=utf-8

                {"incremental":[{"id":"2","data":{"primaryFunction":"Astromech"}}],"completed":[{"id":"2"}],"hasNext":false}
                -----

                """);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("*/*")]
    [InlineData("application/graphql-response+json, multipart/mixed")]
    [InlineData("application/graphql-response+json, text/event-stream, multipart/mixed")]
    public async Task Defer_TypeCondition_If_False(string? acceptHeader)
    {
        // arrange
        using var server = CreateDeferServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = JsonContent.Create(new
        {
            query = """
                query($if: Boolean!) {
                    hero {
                        name
                        ... on Droid @defer(label: "droid_details", if: $if) {
                            primaryFunction
                        }
                    }
                }
                """,
            variables = new { @if = false }
        });

        if (acceptHeader is not null)
        {
            request.Headers.Add("Accept", acceptHeader);
        }

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // When defer is disabled, should get a regular JSON response
        Assert.Equal("application/graphql-response+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                {"data":{"hero":{"name":"R2-D2","primaryFunction":"Astromech"}}}
                """);
    }

    [Theory]
    [InlineData("application/jsonl")]
    [InlineData("application/graphql-response+json, application/jsonl")]
    public async Task Simple_Defer_JsonLines(string acceptHeader)
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
        request.Headers.Add("Accept", acceptHeader);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/jsonl", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                {"data":{"product":{"name":"Abc"}},"pending":[{"id":"2","path":["product"]}],"hasNext":true}
                {"incremental":[{"id":"2","data":{"description":"Abc desc"}}],"completed":[{"id":"2"}],"hasNext":false}

                """);
    }

    [Theory]
    [InlineData("application/jsonl; incrementalSpec=v0.1")]
    [InlineData("application/graphql-response+json, application/jsonl; incrementalSpec=v0.1")]
    public async Task Simple_Defer_JsonLines_Legacy_Format(string acceptHeader)
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
        request.Headers.Add("Accept", acceptHeader);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/jsonl", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                {"data":{"product":{"name":"Abc"}},"hasNext":true}
                {"incremental":[{"data":{"description":"Abc desc"},"path":["product"]}],"hasNext":false}

                """);
    }

    [Fact]
    public async Task Defer_With_Label_JsonLines_Legacy_Format()
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
        request.Headers.Add("Accept", "application/jsonl; incrementalSpec=v0.1");

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/jsonl", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                {"data":{"product":{"name":"Abc"}},"hasNext":true}
                {"incremental":[{"data":{"description":"Abc desc"},"path":["product"],"label":"productDescription"}],"hasNext":false}

                """);
    }

    [Theory]
    [InlineData("application/jsonl")]
    [InlineData("application/graphql-response+json, application/jsonl")]
    public async Task Defer_With_Label_JsonLines(string acceptHeader)
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
        request.Headers.Add("Accept", acceptHeader);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/jsonl", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                {"data":{"product":{"name":"Abc"}},"pending":[{"id":"2","path":["product"],"label":"productDescription"}],"hasNext":true}
                {"incremental":[{"id":"2","data":{"description":"Abc desc"}}],"completed":[{"id":"2"}],"hasNext":false}

                """);
    }

    [Theory]
    [InlineData("application/jsonl")]
    [InlineData("application/graphql-response+json, application/jsonl")]
    public async Task Defer_TypeCondition_JsonLines(string acceptHeader)
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
                    hero {
                        name
                        ... on Droid @defer(label: "droid_details") {
                            primaryFunction
                        }
                    }
                }
                """
        });
        request.Headers.Add("Accept", acceptHeader);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/jsonl", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                {"data":{"hero":{"name":"R2-D2"}},"pending":[{"id":"2","path":["hero"],"label":"droid_details"}],"hasNext":true}
                {"incremental":[{"id":"2","data":{"primaryFunction":"Astromech"}}],"completed":[{"id":"2"}],"hasNext":false}

                """);
    }

    [Fact]
    public async Task Defer_Overlap_Multipart_Legacy_Format()
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
                    }
                    ... @defer(label: "foo") {
                        product {
                            name
                            description
                            reviews {
                                rating
                            }
                        }
                    }
                }
                """
        });
        request.Headers.Add("Accept", "multipart/mixed; incrementalSpec=v0.1");

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("multipart/mixed", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """

                ---
                Content-Type: application/json; charset=utf-8

                {"data":{"product":{"name":"Abc"}},"hasNext":true}
                ---
                Content-Type: application/json; charset=utf-8

                {"incremental":[{"data":{"product":{"name":"Abc","description":"Abc desc","reviews":[{"rating":5}]}},"path":[],"label":"foo"}],"hasNext":false}
                -----

                """);
    }

    [Fact]
    public async Task Defer_Overlap_EventStream_Legacy_Format()
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
                    }
                    ... @defer(label: "foo") {
                        product {
                            name
                            description
                            reviews {
                                rating
                            }
                        }
                    }
                }
                """
        });
        request.Headers.Add("Accept", "text/event-stream; incrementalSpec=v0.1");

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains(
            "\"data\":{\"product\":{\"name\":\"Abc\"}},\"hasNext\":true",
            content,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"incremental\":[{\"data\":{\"product\":{\"name\":\"Abc\",\"description\":\"Abc desc\",\"reviews\":[{\"rating\":5}]}},\"path\":[],\"label\":\"foo\"}]",
            content,
            StringComparison.Ordinal);
        Assert.Contains("event: complete", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Defer_Overlap_JsonLines_Legacy_Format()
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
                    }
                    ... @defer(label: "foo") {
                        product {
                            name
                            description
                            reviews {
                                rating
                            }
                        }
                    }
                }
                """
        });
        request.Headers.Add("Accept", "application/jsonl; incrementalSpec=v0.1");

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/jsonl", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                {"data":{"product":{"name":"Abc"}},"hasNext":true}
                {"incremental":[{"data":{"product":{"name":"Abc","description":"Abc desc","reviews":[{"rating":5}]}},"path":[],"label":"foo"}],"hasNext":false}

                """);
    }

    [Fact]
    public async Task Defer_Two_Labels_Shared_Field_Multipart_Legacy_Format()
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
                    ... @defer(label: "a") {
                        product {
                            name
                        }
                    }
                    ... @defer(label: "b") {
                        product {
                            name
                        }
                    }
                }
                """
        });
        request.Headers.Add("Accept", "multipart/mixed; incrementalSpec=v0.1");

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("multipart/mixed", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"label\":\"a\"", content, StringComparison.Ordinal);
        Assert.Contains("\"label\":\"b\"", content, StringComparison.Ordinal);
        Assert.Contains("\"data\":{\"product\":{\"name\":\"Abc\"}}", content, StringComparison.Ordinal);
        Assert.True(
            content.Split("\"name\":\"Abc\"", StringSplitOptions.None).Length - 1 >= 2,
            "Expected both labeled deferred payloads to include product.name.");
    }

    [Fact]
    public async Task Defer_List_Item_Fragment_Multipart_Legacy_Format_Complete_In_Order()
    {
        // arrange
        var gates = new ItemGates(3);

        // act
        var content = await ExecuteGatedDeferListAsync(gates, [0, 1, 2], DeferredItemFragmentQuery);

        // assert
        content.MatchInlineSnapshot(
            """

            ---
            Content-Type: application/json; charset=utf-8

            {"data":{"products":{"nodes":[{"id":0},{"id":1},{"id":2}]}},"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"description":"Description 0"},"path":["products","nodes",0],"label":"details"}],"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"description":"Description 1"},"path":["products","nodes",1],"label":"details"}],"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"description":"Description 2"},"path":["products","nodes",2],"label":"details"}],"hasNext":false}
            -----

            """);
    }

    [Fact]
    public async Task Defer_List_Item_Fragment_Multipart_Legacy_Format_Complete_In_Reverse_Order()
    {
        // arrange
        var gates = new ItemGates(3);

        // act
        var content = await ExecuteGatedDeferListAsync(gates, [2, 1, 0], DeferredItemFragmentQuery);

        // assert
        content.MatchInlineSnapshot(
            """

            ---
            Content-Type: application/json; charset=utf-8

            {"data":{"products":{"nodes":[{"id":0},{"id":1},{"id":2}]}},"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"description":"Description 2"},"path":["products","nodes",2],"label":"details"}],"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"description":"Description 1"},"path":["products","nodes",1],"label":"details"}],"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"description":"Description 0"},"path":["products","nodes",0],"label":"details"}],"hasNext":false}
            -----

            """);
    }

    [Fact]
    public async Task Defer_List_Item_Fragment_Multipart_Legacy_Format_Complete_In_Mixed_Order()
    {
        // arrange
        var gates = new ItemGates(3);

        // act
        var content = await ExecuteGatedDeferListAsync(gates, [1, 2, 0], DeferredItemFragmentQuery);

        // assert
        content.MatchInlineSnapshot(
            """

            ---
            Content-Type: application/json; charset=utf-8

            {"data":{"products":{"nodes":[{"id":0},{"id":1},{"id":2}]}},"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"description":"Description 1"},"path":["products","nodes",1],"label":"details"}],"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"description":"Description 2"},"path":["products","nodes",2],"label":"details"}],"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"description":"Description 0"},"path":["products","nodes",0],"label":"details"}],"hasNext":false}
            -----

            """);
    }

    [Fact]
    public async Task Defer_List_Item_Overlap_Multipart_Legacy_Format_Complete_In_Mixed_Order()
    {
        // arrange
        var gates = new ItemGates(3);

        // act
        var content = await ExecuteGatedDeferListAsync(
            gates,
            [1, 2, 0],
            """
            {
                products {
                    nodes {
                        id
                        ... @defer {
                            id
                            description
                        }
                    }
                }
            }
            """);

        // assert
        content.MatchInlineSnapshot(
            """

            ---
            Content-Type: application/json; charset=utf-8

            {"data":{"products":{"nodes":[{"id":0},{"id":1},{"id":2}]}},"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"id":1,"description":"Description 1"},"path":["products","nodes",1]}],"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"id":2,"description":"Description 2"},"path":["products","nodes",2]}],"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"id":0,"description":"Description 0"},"path":["products","nodes",0]}],"hasNext":false}
            -----

            """);
    }

    [Fact]
    public async Task Defer_List_Item_Nested_Defers_Multipart_Legacy_Format_Complete_In_Mixed_Order()
    {
        // arrange
        var gates = new ItemGates(3);

        // act
        var content = await ExecuteGatedDeferListAsync(
            gates,
            [1, 2, 0],
            """
            {
                products {
                    nodes {
                        ... @defer {
                            ...ProductFragment
                            ... @defer {
                                ...ProductFragment
                                ... @defer {
                                    ...ProductFragment
                                }
                            }
                        }
                    }
                }
            }

            fragment ProductFragment on GatedProduct {
                id
                description
            }
            """);

        // assert
        content.MatchInlineSnapshot(
            """

            ---
            Content-Type: application/json; charset=utf-8

            {"data":{"products":{"nodes":[{},{},{}]}},"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"id":1,"description":"Description 1"},"path":["products","nodes",1]}],"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"id":2,"description":"Description 2"},"path":["products","nodes",2]}],"hasNext":true}
            ---
            Content-Type: application/json; charset=utf-8

            {"incremental":[{"data":{"id":0,"description":"Description 0"},"path":["products","nodes",0]}],"hasNext":false}
            -----

            """);
    }

    private async Task<string> ExecuteGatedDeferListAsync(
        ItemGates gates,
        int[] releaseOrder,
        string query)
    {
        using var server = ServerFactory.Create(
            services => services
                .AddRouting()
                .AddSingleton(gates)
                .AddGraphQLServer()
                .ModifyCostOptions(o => o.DefaultListSize = 1)
                .AddQueryType<GatedQuery>()
                .AddDefaultBatchDispatcher()
                .AddHttpResponseFormatter()
                .ModifyOptions(o => o.EnableDefer = true),
            app => app
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        var client = server.CreateClient();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = JsonContent.Create(new
        {
            query
        });
        request.Headers.Add("Accept", "multipart/mixed; incrementalSpec=v0.1");

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream);
        var content = new StringBuilder();
        var buffer = new char[1024];

        async Task ReadPayloadsAsync(int count)
        {
            while (CountPayloads(content) < count)
            {
                var read = await reader.ReadAsync(buffer.AsMemory(), timeout.Token);

                if (read == 0)
                {
                    return;
                }

                content.Append(buffer, 0, read);
            }
        }

        await ReadPayloadsAsync(1);

        for (var i = 0; i < releaseOrder.Length; i++)
        {
            gates.Release(releaseOrder[i]);
            await ReadPayloadsAsync(i + 2);
        }

        content.Append(await reader.ReadToEndAsync(timeout.Token));
        return content.ToString();

        static int CountPayloads(StringBuilder builder)
        {
            var text = builder.ToString();
            var count = 0;
            var index = 0;

            while ((index = text.IndexOf("\"hasNext\"", index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index++;
            }

            return count;
        }
    }

    private const string DeferredItemFragmentQuery =
        """
        {
            products {
                nodes {
                    id
                    ...ProductDetails @defer(label: "details")
                }
            }
        }

        fragment ProductDetails on GatedProduct {
            description
        }
        """;

    private TestServer CreateDeferServer(
        HttpTransportVersion serverTransportVersion = HttpTransportVersion.Latest)
    {
        return ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .ModifyCostOptions(o => o.DefaultListSize = 1)
                .AddQueryType<Query>()
                .AddType<Droid>()
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

        public ICharacter GetHero()
            => new Droid { Name = "R2-D2" };
    }

    [InterfaceType("Character")]
    public interface ICharacter
    {
        string Name { get; }
    }

    public sealed class Droid : ICharacter
    {
        public string Name { get; init; } = default!;

        public async Task<string> GetPrimaryFunctionAsync()
        {
            await Task.Delay(1000);
            return "Astromech";
        }
    }

    public sealed record Product(string Name)
    {
        public async Task<string> GetDescriptionAsync()
        {
            await Task.Delay(1000);
            return Name + " desc";
        }

        public async Task<IReadOnlyList<Review>> GetReviewsAsync()
        {
            await Task.Delay(1000);
            return new[] { new Review(5) };
        }
    }

    public sealed record Review(int Rating);

    public sealed class GatedQuery
    {
        public GatedProductPage GetProducts()
            => new();
    }

    public sealed class GatedProductPage
    {
        public IReadOnlyList<GatedProduct> GetNodes()
            => [new(0), new(1), new(2)];
    }

    public sealed record GatedProduct(int Id)
    {
        public async Task<string> GetDescriptionAsync(
            [Service] ItemGates gates,
            CancellationToken cancellationToken)
        {
            await gates.WaitAsync(Id, cancellationToken);
            return "Description " + Id;
        }
    }

    public sealed class ItemGates
    {
        private readonly TaskCompletionSource[] _gates;

        public ItemGates(int count)
        {
            _gates = new TaskCompletionSource[count];

            for (var i = 0; i < count; i++)
            {
                _gates[i] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        public Task WaitAsync(int index, CancellationToken cancellationToken)
            => _gates[index].Task.WaitAsync(cancellationToken);

        public void Release(int index)
            => _gates[index].SetResult();
    }
}
