using System.Net;
using System.Net.Http.Json;
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

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // When defer is disabled, should get a regular JSON response, not multipart
        Assert.Equal("application/graphql-response+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();

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

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // assert
        // Should reject the request since we have a deferred result but
        // the user only accepts non-streaming JSON payload
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal("application/graphql-response+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                {"errors":[{"message":"The specified operation kind is not allowed."}]}
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

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // When defer is disabled, should get a regular JSON response
        Assert.Equal("application/graphql-response+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();

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

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/jsonl", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();

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

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/jsonl", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();

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

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/jsonl", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();

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

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/jsonl", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();

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

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/jsonl", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();

        Snapshot
            .Create()
            .Add(content, "Response")
            .MatchInline(
                """
                {"data":{"hero":{"name":"R2-D2"}},"pending":[{"id":"2","path":["hero"],"label":"droid_details"}],"hasNext":true}
                {"incremental":[{"id":"2","data":{"primaryFunction":"Astromech"}}],"completed":[{"id":"2"}],"hasNext":false}

                """);
    }

    private TestServer CreateDeferServer(
        HttpTransportVersion serverTransportVersion = HttpTransportVersion.Latest)
    {
        return ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
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
    }
}
