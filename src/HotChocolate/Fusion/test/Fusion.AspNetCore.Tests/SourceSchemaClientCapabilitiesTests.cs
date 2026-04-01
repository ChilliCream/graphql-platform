using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

// Note: We do not currently handle the case that Variable Batching is supported, but Request Batching isn't.
public class SourceSchemaClientCapabilitiesTests : FusionTestBase
{
    #region Singular Request

    [Fact]
    public async Task SingularRequest_CustomAcceptHeader()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>(),
            defaultAcceptHeaderValues: [new MediaTypeWithQualityHeaderValue("application/json")]);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              books {
                title
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    #endregion

    #region Batch Request

    [Fact]
    public async Task BatchRequest_CustomAcceptHeader()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            batchingAcceptHeaderValues: [new MediaTypeWithQualityHeaderValue("application/jsonl")]);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              books {
                rating
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task BatchRequest_OnlyVariableBatching()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            capabilities: SourceSchemaClientCapabilities.VariableBatching);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              books {
                rating
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task BatchRequest_OnlyRequestBatching()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            capabilities: SourceSchemaClientCapabilities.RequestBatching);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              books {
                rating
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    // This tests the response of HotChocolate < 15 servers
    [Fact]
    public async Task BatchRequest_OnlyRequestBatching_ServerReturnsNoRequestIndices()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            capabilities: SourceSchemaClientCapabilities.RequestBatching,
            mockHttpResponse: async request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        event: next
                        data: {"data":{"bookById":{"rating":"1"}}}

                        event: next
                        data: {"data":{"bookById":{"rating":"2"}}}

                        event: complete
                        """,
                        Encoding.UTF8,
                        "text/event-stream")
                };

                return await ReturnHttpResponse(
                    request,
                    "application/jsonl; charset=utf-8, text/event-stream; charset=utf-8, application/graphql-response+json; charset=utf-8, application/json; charset=utf-8",
                    """
                    [{"query":"query Op_490e9345_2(\n  $__fusion_1_id: ID!\n) {\n  bookById(id: $__fusion_1_id) {\n    rating\n  }\n}","variables":{"__fusion_1_id":"1"}},{"query":"query Op_490e9345_2(\n  $__fusion_1_id: ID!\n) {\n  bookById(id: $__fusion_1_id) {\n    rating\n  }\n}","variables":{"__fusion_1_id":"2"}}]
                    """,
                    response
                );
            });

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              books {
                rating
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    // This tests the response of non-HotChocolate servers
    [Fact]
    public async Task BatchRequest_OnlyRequestBatching_ServerReturnsJsonArray()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            capabilities: SourceSchemaClientCapabilities.RequestBatching,
            mockHttpResponse: async request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        [
                          {"data":{"bookById":{"rating":"1"}}},
                          {"data":{"bookById":{"rating":"2"}}}
                        ]
                        """,
                        Encoding.UTF8,
                        "application/json")
                };

                return await ReturnHttpResponse(
                    request,
                    "application/jsonl; charset=utf-8, text/event-stream; charset=utf-8, application/graphql-response+json; charset=utf-8, application/json; charset=utf-8",
                    """
                    [{"query":"query Op_490e9345_2(\n  $__fusion_1_id: ID!\n) {\n  bookById(id: $__fusion_1_id) {\n    rating\n  }\n}","variables":{"__fusion_1_id":"1"}},{"query":"query Op_490e9345_2(\n  $__fusion_1_id: ID!\n) {\n  bookById(id: $__fusion_1_id) {\n    rating\n  }\n}","variables":{"__fusion_1_id":"2"}}]
                    """,
                    response
                );
            });

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              books {
                rating
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    #endregion

    #region OperationBatch Batch Request

    [Fact]
    public async Task OperationBatch_BatchRequest_CustomAcceptHeader()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            batchingAcceptHeaderValues: [new MediaTypeWithQualityHeaderValue("application/jsonl")]);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              books {
                a: author {
                  a: name
                }
                b: author {
                  b: name(postFix: "2")
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task OperationBatch_BatchRequest_OnlyVariableBatching()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            capabilities: SourceSchemaClientCapabilities.VariableBatching);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              books {
                a: author {
                  a: name
                }
                b: author {
                  b: name(postFix: "2")
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task OperationBatch_BatchRequest_OnlyRequestBatching()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            capabilities: SourceSchemaClientCapabilities.RequestBatching);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              books {
                a: author {
                  a: name
                }
                b: author {
                  b: name(postFix: "2")
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    // This tests the response of HotChocolate < 15 servers
    [Fact]
    public async Task OperationBatch_BatchRequest_OnlyRequestBatching_ServerReturnsNoRequestIndices()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            capabilities: SourceSchemaClientCapabilities.RequestBatching,
            mockHttpResponse: async request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        event: next
                        data: {"data":{"authorById":{"b":"Author 1 - 2"}}}

                        event: next
                        data: {"data":{"authorById":{"b":"Author 2 - 2"}}}

                        event: next
                        data: {"data":{"authorById":{"a":"Author 1"}}}

                        event: next
                        data: {"data":{"authorById":{"a":"Author 2"}}}

                        event: complete
                        """,
                        Encoding.UTF8,
                        "text/event-stream")
                };

                return await ReturnHttpResponse(
                    request,
                    "application/jsonl; charset=utf-8, text/event-stream; charset=utf-8, application/graphql-response+json; charset=utf-8, application/json; charset=utf-8",
                    """
                    [{"query":"query Op_1b3419da_2(\n  $__fusion_1_id: ID!\n) {\n  authorById(id: $__fusion_1_id) {\n    b: name(postFix: \"2\")\n  }\n}","variables":{"__fusion_1_id":"1"}},{"query":"query Op_1b3419da_2(\n  $__fusion_1_id: ID!\n) {\n  authorById(id: $__fusion_1_id) {\n    b: name(postFix: \"2\")\n  }\n}","variables":{"__fusion_1_id":"2"}},{"query":"query Op_1b3419da_3(\n  $__fusion_2_id: ID!\n) {\n  authorById(id: $__fusion_2_id) {\n    a: name\n  }\n}","variables":{"__fusion_2_id":"1"}},{"query":"query Op_1b3419da_3(\n  $__fusion_2_id: ID!\n) {\n  authorById(id: $__fusion_2_id) {\n    a: name\n  }\n}","variables":{"__fusion_2_id":"2"}}]
                    """,
                    response);
            });

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              books {
                a: author {
                  a: name
                }
                b: author {
                  b: name(postFix: "2")
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    // This tests the response of non-HotChocolate servers
    [Fact]
    public async Task OperationBatch_BatchRequest_OnlyRequestBatching_ServerReturnsJsonArray()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            capabilities: SourceSchemaClientCapabilities.RequestBatching,
            mockHttpResponse: async request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        [
                          {"data":{"authorById":{"b":"Author 1 - 2"}}},
                          {"data":{"authorById":{"b":"Author 2 - 2"}}},
                          {"data":{"authorById":{"a":"Author 1"}}},
                          {"data":{"authorById":{"a":"Author 2"}}}
                        ]
                        """,
                        Encoding.UTF8,
                        "application/json")
                };

                return await ReturnHttpResponse(
                    request,
                    "application/jsonl; charset=utf-8, text/event-stream; charset=utf-8, application/graphql-response+json; charset=utf-8, application/json; charset=utf-8",
                    """
                    [{"query":"query Op_1b3419da_2(\n  $__fusion_1_id: ID!\n) {\n  authorById(id: $__fusion_1_id) {\n    b: name(postFix: \"2\")\n  }\n}","variables":{"__fusion_1_id":"1"}},{"query":"query Op_1b3419da_2(\n  $__fusion_1_id: ID!\n) {\n  authorById(id: $__fusion_1_id) {\n    b: name(postFix: \"2\")\n  }\n}","variables":{"__fusion_1_id":"2"}},{"query":"query Op_1b3419da_3(\n  $__fusion_2_id: ID!\n) {\n  authorById(id: $__fusion_2_id) {\n    a: name\n  }\n}","variables":{"__fusion_2_id":"1"}},{"query":"query Op_1b3419da_3(\n  $__fusion_2_id: ID!\n) {\n  authorById(id: $__fusion_2_id) {\n    a: name\n  }\n}","variables":{"__fusion_2_id":"2"}}]
                    """,
                    response);
            });

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              books {
                a: author {
                  a: name
                }
                b: author {
                  b: name(postFix: "2")
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    #endregion

    private static string GenerateRandomString(int kiloBytes)
    {
        var targetBytes = kiloBytes * 1024;
        var charsNeeded = targetBytes / 2;
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        var random = new Random(0);
        var stringBuilder = new StringBuilder(charsNeeded);

        for (var i = 0; i < charsNeeded; i++)
        {
            stringBuilder.Append(chars[random.Next(chars.Length)]);
        }

        return stringBuilder.ToString();
    }

    public static class SourceSchema1
    {
        public class Query
        {
            private readonly OrderedDictionary<int, Book> _books =
                new() { [1] = new Book(1, "C# in Depth"), [2] = new Book(2, "The Lord of the Rings") };

            public IEnumerable<Book> GetBooks()
                => _books.Values;

            [Lookup, Internal]
            public Book? GetBookById([ID] int id) => _books.GetValueOrDefault(id);

            [Lookup, Internal]
            public Author? GetAuthorById([ID] int id) => null;
        }

        public record Book([property: ID] int Id, string Title)
        {
            public Author? GetAuthor() => new Author(Id);
        };

        public record Author([property: ID] int Id);
    }

    public static class SourceSchema2
    {
        public class Query
        {
            [Lookup, Internal]
            public Book? GetBookById([ID] int id) => new(id);

            [Lookup, Internal]
            public Author? GetAuthorById([ID] int id) => new(id);
        }

        public record Book([property: ID] int Id)
        {
            public string GetRating() => Id.ToString();
        }

        public record Author([property: ID] int Id)
        {
            public string GetName(string? postFix = null)
            {
                var name = "Author " + Id;

                if (string.IsNullOrEmpty(postFix))
                {
                    return name;
                }

                return name + " - " + postFix;
            }
        }
    }

    private static async Task<HttpResponseMessage> ReturnHttpResponse(
        HttpRequestMessage request,
        string expectedAcceptHeader,
        string expectedBody,
        HttpResponseMessage response)
    {
        var accept = request.Headers.Accept.ToString();

        if (accept != expectedAcceptHeader || request.Content is null)
        {
            return ErrorHttpResponseMessage();
        }

        var body = await request.Content.ReadAsStringAsync();

        if (body != expectedBody)
        {
            return ErrorHttpResponseMessage();
        }

        return response;
    }

    private static HttpResponseMessage ErrorHttpResponseMessage()
        => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Received an unexpected request.")
        };
}
