using System.Net;
using System.Text;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class LookupTests : FusionTestBase
{
    [Fact]
    public async Task Fetch_From_Nested_Internal_Lookup()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<NestedLookups.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<NestedLookups.SourceSchema2.Query>());

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
                author {
                  name
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
    public async Task Fetch_OneOf_Lookup_With_Name()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<OneOfLookups.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<OneOfLookups.SourceSchema2.Query>());

        using var server3 = CreateSourceSchema(
            "c",
            b => b.AddQueryType<OneOfLookups.SourceSchema3.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2),
            ("c", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              topAuthor {
                id
                name
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
    public async Task Fetch_OneOf_Lookup_With_Id()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<OneOfLookups.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<OneOfLookups.SourceSchema2.Query>());

        using var server3 = CreateSourceSchema(
            "c",
            b => b.AddQueryType<OneOfLookups.SourceSchema3.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2),
            ("c", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              books {
                author {
                  id
                  name
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
    public async Task Fetch_With_Request_Batching_JsonLines()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<OneOfLookups.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<OneOfLookups.SourceSchema4.Query>(),
            batchingMode: SourceSchemaHttpClientBatchingMode.ApolloRequestBatching,
            batchingAcceptHeaderValues: [new("application/jsonl") { CharSet = "utf-8" }]);

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
                author {
                  id
                  name
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
    public async Task Fetch_With_Request_Batching_JsonLines_Large_Response()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<OneOfLookups.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<OneOfLookups.SourceSchema4.Query>(),
            batchingMode: SourceSchemaHttpClientBatchingMode.ApolloRequestBatching,
            batchingAcceptHeaderValues: [new("application/jsonl") { CharSet = "utf-8" }]);

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
                author {
                  id
                  name(large: true)
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
    public async Task Fetch_With_Request_Batching_SSE()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<OneOfLookups.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<OneOfLookups.SourceSchema4.Query>(),
            batchingMode: SourceSchemaHttpClientBatchingMode.ApolloRequestBatching,
            batchingAcceptHeaderValues: [new("text/event-stream") { CharSet = "utf-8" }]);

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
                author {
                  id
                  name
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
    public async Task Fetch_With_Request_Batching_SSE_Large_Response()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<OneOfLookups.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<OneOfLookups.SourceSchema4.Query>(),
            batchingMode: SourceSchemaHttpClientBatchingMode.ApolloRequestBatching,
            batchingAcceptHeaderValues: [new("text/event-stream") { CharSet = "utf-8" }]);

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
                author {
                  id
                  name(large: true)
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
    public async Task Fetch_With_Request_Batching_JsonArray()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<OneOfLookups.SourceSchema1.Query>());

        const string jsonArrayResponse =
            """
            [
              {
                "data": {
                  "authorById": {
                    "name": "Author 1"
                  }
                }
              },
              {
                "data": {
                  "authorById": {
                    "name": "Author 2"
                  }
                }
              },
              {
                "data": {
                  "authorById": {
                    "name": "Author 2"
                  }
                }
              },
              {
                "data": {
                  "authorById": {
                    "name": "Author 2"
                  }
                }
              }
            ]
            """;

        using var server2 = CreateSourceSchema(
            "b",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author {
              id: ID!
              name: String!
            }
            """,
            batchingMode: SourceSchemaHttpClientBatchingMode.ApolloRequestBatching,
            httpClient: new HttpClient(new MockHttpMessageHandler(jsonArrayResponse)));

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
                author {
                  id
                  name
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
    public async Task Fetch_With_Request_Batching_JsonArray_Large_Response()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<OneOfLookups.SourceSchema1.Query>());

        var jsonArrayResponse =
            $$"""
            [
              {
                "data": {
                  "authorById": {
                    "name": "Author 1 {{GenerateRandomString(128)}}"
                  }
                }
              },
              {
                "data": {
                  "authorById": {
                    "name": "Author 2 {{GenerateRandomString(128)}}"
                  }
                }
              },
              {
                "data": {
                  "authorById": {
                    "name": "Author 2 {{GenerateRandomString(128)}}"
                  }
                }
              },
              {
                "data": {
                  "authorById": {
                    "name": "Author 2 {{GenerateRandomString(128)}}"
                  }
                }
              }
            ]
            """;

        using var server2 = CreateSourceSchema(
            "b",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author {
              id: ID!
              name: String!
            }
            """,
            batchingMode: SourceSchemaHttpClientBatchingMode.ApolloRequestBatching,
            httpClient: new HttpClient(new MockHttpMessageHandler(jsonArrayResponse)));

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
                author {
                  id
                  name
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

    [Fact(Skip = "The Gateway needs to produce errors for this")]
    public async Task Fetch_With_Request_Batching_JsonArray_Returns_Wrong_Number_Of_Items()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<OneOfLookups.SourceSchema1.Query>());

        // this contains just 2 entries, while it should contain 4.
        const string jsonArrayResponse =
            """
            [
              {
                "data": {
                  "authorById": {
                    "name": "Author 1"
                  }
                }
              },
              {
                "data": {
                  "authorById": {
                    "name": "Author 2"
                  }
                }
              }
            ]
            """;

        using var server2 = CreateSourceSchema(
            "b",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author {
              id: ID!
              name: String!
            }
            """,
            batchingMode: SourceSchemaHttpClientBatchingMode.ApolloRequestBatching,
            httpClient: new HttpClient(new MockHttpMessageHandler(jsonArrayResponse)));

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
                author {
                  id
                  name
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

    [Fact(Skip = "The Gateway needs to produce errors for this")]
    public async Task Fetch_With_Request_Batching_JsonArray_Returns_Singular_Response()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<OneOfLookups.SourceSchema1.Query>());

        const string jsonResponse =
            """
            {
              "data": {
                "authorById": {
                  "name": "Author 1"
                }
              }
            }
            """;

        using var server2 = CreateSourceSchema(
            "b",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author {
              id: ID!
              name: String!
            }
            """,
            batchingMode: SourceSchemaHttpClientBatchingMode.ApolloRequestBatching,
            httpClient: new HttpClient(new MockHttpMessageHandler(jsonResponse)));

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
                author {
                  id
                  name
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

    private sealed class MockHttpMessageHandler(string responseContent) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    public static class NestedLookups
    {
        public static class SourceSchema1
        {
            public record Book([property: ID] int Id, string Title, [property: Shareable] Author Author);

            [EntityKey("id")]
            public record Author([property: ID] int Id);

            public class Query
            {
                private readonly OrderedDictionary<int, Book> _books =
                    new()
                    {
                        [1] = new Book(1, "C# in Depth", new Author(1)),
                        [2] = new Book(2, "The Lord of the Rings", new Author(2)),
                        [3] = new Book(3, "The Hobbit", new Author(2)),
                        [4] = new Book(4, "The Silmarillion", new Author(2))
                    };

                public IEnumerable<Book> GetBooks()
                    => _books.Values;
            }
        }

        public static class SourceSchema2
        {
            public record Author([property: ID] int Id, string Name);

            public class Query
            {
                [Internal]
                public InternalLookups Lookups { get; } = new();
            }

            [Internal]
            public class InternalLookups
            {
                private readonly OrderedDictionary<int, Author> _authors = new()
                {
                    [1] = new Author(1, "Jon Skeet"),
                    [2] = new Author(2, "JRR Tolkien")
                };

                [Lookup]
                public Author GetAuthorById([ID] int id)
                    => _authors[id];
            }

            public record Book([property: ID] int Id, [property: Shareable] Author Author)
            {
                public string IdAndTitle([Require] string title)
                    => $"{Id} - {title}";
            }
        }
    }

    public static class OneOfLookups
    {
        public static class SourceSchema1
        {
            public record Book([property: ID] int Id, string Title, [property: Shareable] Author Author);

            [EntityKey("id")]
            public record Author([property: ID] int Id);

            public class Query
            {
                private readonly OrderedDictionary<int, Book> _books =
                    new()
                    {
                        [1] = new Book(1, "C# in Depth", new Author(1)),
                        [2] = new Book(2, "The Lord of the Rings", new Author(2)),
                        [3] = new Book(3, "The Hobbit", new Author(2)),
                        [4] = new Book(4, "The Silmarillion", new Author(2))
                    };

                public IEnumerable<Book> GetBooks()
                    => _books.Values;
            }
        }

        public static class SourceSchema2
        {
            public record Author([property: ID] int Id, string Name);

            public class Query
            {
                [Internal]
                public InternalLookups Lookups { get; } = new();
            }

            [Internal]
            public class InternalLookups
            {
                private readonly OrderedDictionary<int, Author> _authors = new()
                {
                    [1] = new Author(1, "Jon Skeet"),
                    [2] = new Author(2, "JRR Tolkien")
                };

                [Lookup, Internal]
                public Author GetAuthor([Is("{ id } | { name }")] AuthorByInput by)
                {
                    if (by.Id is not null)
                    {
                        return _authors[by.Id.Value];
                    }

                    return _authors.Values.First(a => a.Name == by.Name);
                }
            }

            public record Book([property: ID] int Id, [property: Shareable] Author Author)
            {
                public string IdAndTitle([Require] string title)
                    => $"{Id} - {title}";
            }

            [OneOf]
            public record AuthorByInput([property: ID] int? Id, string? Name);
        }

        public static class SourceSchema3
        {
            public class Query
            {
                public Author GetTopAuthor()
                    => new("Jon Skeet");
            }

            public record Author([property: Shareable] string Name);
        }

        public static class SourceSchema4
        {
            public class Query
            {
                [Lookup, Internal]
                public Author? GetAuthorById([ID] int id)
                    => new(id);
            }

            public record Author([property: ID] int Id)
            {
                public string GetName(bool large = false)
                {
                    var name = "Author " + Id;

                    if (large)
                    {
                        return name + " " + GenerateRandomString(128);
                    }

                    return name;
                }
            }
        }
    }
}
