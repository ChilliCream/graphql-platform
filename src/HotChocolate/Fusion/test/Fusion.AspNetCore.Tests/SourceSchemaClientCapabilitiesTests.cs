using System.Net;
using System.Text;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

// TODO: OperationBatchRequest_OnlyRequestBatching_NoRequestIndexInServerPayloads this does still do variable batching
public class SourceSchemaClientCapabilitiesTests : FusionTestBase
{
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

    [Fact]
    public async Task OperationBatchRequest_OnlyRequestBatching()
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
                  b: name
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
    public async Task BatchRequest_OnlyRequestBatching_NoRequestIndexInServerPayloads()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            capabilities: SourceSchemaClientCapabilities.RequestBatching,
            mockHttpResponse: request =>
            {
                // TODO: Assert incoming request. mockHttpResponse should be async
                var accept = request.Headers.Accept.ToString();
                if (!accept.Contains("text/event-stream"))
                {
                    throw new InvalidOperationException("");
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        event: next
                        data: {"data":{"bookById":{"rating":"1"}}}

                        event: next
                        data: {"data":{"bookById":{"rating":"2"}}}

                        event: next
                        data: {"data":{"bookById":{"rating":"3"}}}

                        event: next
                        data: {"data":{"bookById":{"rating":"4"}}}

                        event: complete
                        """,
                        Encoding.UTF8,
                        "text/event-stream")
                };
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

    // This tests the response of HotChocolate < 15 servers
    [Fact]
    public async Task OperationBatchRequest_OnlyRequestBatching_NoRequestIndexInServerPayloads()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<SourceSchema2.Query>(),
            capabilities: SourceSchemaClientCapabilities.RequestBatching,
            mockHttpResponse: request =>
            {
                // TODO: Assert incoming request. mockHttpResponse should be async
                var accept = request.Headers.Accept.ToString();
                if (!accept.Contains("text/event-stream"))
                {
                    throw new InvalidOperationException("");
                }

                // TODO: How to properly do this
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        event: next
                        data: {"data":{"bookById":{"rating":"1"}}}

                        event: next
                        data: {"data":{"bookById":{"rating":"2"}}}

                        event: next
                        data: {"data":{"bookById":{"rating":"3"}}}

                        event: next
                        data: {"data":{"bookById":{"rating":"4"}}}

                        event: complete
                        """,
                        Encoding.UTF8,
                        "text/event-stream")
                };
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
                  b: name
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

//     [Fact]
//     public async Task Fetch_With_ApolloRequestBatching_JsonLines()
//     {
//         // arrange
//         using var server1 = CreateSourceSchema(
//             "a",
//             b => b.AddQueryType<SourceSchema1.Query>());
//
//         using var server2 = CreateSourceSchema(
//             "b",
//             b => b.AddQueryType<SourceSchema4.Query>(),
//             capabilities: SourceSchemaClientCapabilities.ApolloRequestBatching,
//             batchingAcceptHeaderValues: [new("application/jsonl") { CharSet = "utf-8" }]);
//
//         using var gateway = await CreateCompositeSchemaAsync(
//         [
//             ("a", server1),
//             ("b", server2)
//         ]);
//
//         // act
//         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
//
//         var request = new OperationRequest(
//             """
//             {
//               books {
//                 author {
//                   id
//                   name
//                 }
//               }
//             }
//             """);
//
//         using var result = await client.PostAsync(
//             request,
//             new Uri("http://localhost:5000/graphql"));
//
//         // assert
//         await MatchSnapshotAsync(gateway, request, result);
//     }
//
//     [Fact]
//     public async Task Fetch_With_ApolloRequestBatching_JsonLines_Large_Response()
//     {
//         // arrange
//         using var server1 = CreateSourceSchema(
//             "a",
//             b => b.AddQueryType<SourceSchema1.Query>());
//
//         using var server2 = CreateSourceSchema(
//             "b",
//             b => b.AddQueryType<SourceSchema4.Query>(),
//             capabilities: SourceSchemaClientCapabilities.ApolloRequestBatching,
//             batchingAcceptHeaderValues: [new("application/jsonl") { CharSet = "utf-8" }]);
//
//         using var gateway = await CreateCompositeSchemaAsync(
//         [
//             ("a", server1),
//             ("b", server2)
//         ]);
//
//         // act
//         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
//
//         var request = new OperationRequest(
//             """
//             {
//               books {
//                 author {
//                   id
//                   name(large: true)
//                 }
//               }
//             }
//             """);
//
//         using var result = await client.PostAsync(
//             request,
//             new Uri("http://localhost:5000/graphql"));
//
//         // assert
//         await MatchSnapshotAsync(gateway, request, result);
//     }
//
//     [Fact]
//     public async Task Fetch_With_ApolloRequestBatching_SSE()
//     {
//         // arrange
//         using var server1 = CreateSourceSchema(
//             "a",
//             b => b.AddQueryType<SourceSchema1.Query>());
//
//         using var server2 = CreateSourceSchema(
//             "b",
//             b => b.AddQueryType<SourceSchema4.Query>(),
//             capabilities: SourceSchemaClientCapabilities.ApolloRequestBatching,
//             batchingAcceptHeaderValues: [new("text/event-stream") { CharSet = "utf-8" }]);
//
//         using var gateway = await CreateCompositeSchemaAsync(
//         [
//             ("a", server1),
//             ("b", server2)
//         ]);
//
//         // act
//         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
//
//         var request = new OperationRequest(
//             """
//             {
//               books {
//                 author {
//                   id
//                   name
//                 }
//               }
//             }
//             """);
//
//         using var result = await client.PostAsync(
//             request,
//             new Uri("http://localhost:5000/graphql"));
//
//         // assert
//         await MatchSnapshotAsync(gateway, request, result);
//     }
//
//     [Fact]
//     public async Task Fetch_With_ApolloRequestBatching_SSE_Large_Response()
//     {
//         // arrange
//         using var server1 = CreateSourceSchema(
//             "a",
//             b => b.AddQueryType<SourceSchema1.Query>());
//
//         using var server2 = CreateSourceSchema(
//             "b",
//             b => b.AddQueryType<SourceSchema4.Query>(),
//             capabilities: SourceSchemaClientCapabilities.ApolloRequestBatching,
//             batchingAcceptHeaderValues: [new("text/event-stream") { CharSet = "utf-8" }]);
//
//         using var gateway = await CreateCompositeSchemaAsync(
//         [
//             ("a", server1),
//             ("b", server2)
//         ]);
//
//         // act
//         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
//
//         var request = new OperationRequest(
//             """
//             {
//               books {
//                 author {
//                   id
//                   name(large: true)
//                 }
//               }
//             }
//             """);
//
//         using var result = await client.PostAsync(
//             request,
//             new Uri("http://localhost:5000/graphql"));
//
//         // assert
//         await MatchSnapshotAsync(gateway, request, result);
//     }
//
//     [Fact]
//     public async Task Fetch_With_ApolloRequestBatching_JsonArray()
//     {
//         // arrange
//         using var server1 = CreateSourceSchema(
//             "a",
//             b => b.AddQueryType<SourceSchema1.Query>());
//
//         const string jsonArrayResponse =
//             """
//             [
//               {
//                 "data": {
//                   "authorById": {
//                     "name": "Author 1"
//                   }
//                 }
//               },
//               {
//                 "data": {
//                   "authorById": {
//                     "name": "Author 2"
//                   }
//                 }
//               },
//               {
//                 "data": {
//                   "authorById": {
//                     "name": "Author 2"
//                   }
//                 }
//               },
//               {
//                 "data": {
//                   "authorById": {
//                     "name": "Author 2"
//                   }
//                 }
//               }
//             ]
//             """;
//
//         using var server2 = CreateSourceSchema(
//             "b",
//             """
//             type Query {
//               authorById(id: ID!): Author @lookup
//             }
//
//             type Author {
//               id: ID!
//               name: String!
//             }
//             """,
//             capabilities: SourceSchemaClientCapabilities.ApolloRequestBatching,
//             httpClient: new HttpClient(new MockHttpMessageHandler(jsonArrayResponse)));
//
//         using var gateway = await CreateCompositeSchemaAsync(
//         [
//             ("a", server1),
//             ("b", server2)
//         ]);
//
//         // act
//         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
//
//         var request = new OperationRequest(
//             """
//             {
//               books {
//                 author {
//                   id
//                   name
//                 }
//               }
//             }
//             """);
//
//         using var result = await client.PostAsync(
//             request,
//             new Uri("http://localhost:5000/graphql"));
//
//         // assert
//         await MatchSnapshotAsync(gateway, request, result);
//     }
//
//     [Fact]
//     public async Task Fetch_With_Request_Batching_JsonArray_Large_Response()
//     {
//         // arrange
//         using var server1 = CreateSourceSchema(
//             "a",
//             b => b.AddQueryType<SourceSchema1.Query>());
//
//         var jsonArrayResponse =
//             $$"""
//               [
//                 {
//                   "data": {
//                     "authorById": {
//                       "name": "Author 1 {{GenerateRandomString(128)}}"
//                     }
//                   }
//                 },
//                 {
//                   "data": {
//                     "authorById": {
//                       "name": "Author 2 {{GenerateRandomString(128)}}"
//                     }
//                   }
//                 },
//                 {
//                   "data": {
//                     "authorById": {
//                       "name": "Author 2 {{GenerateRandomString(128)}}"
//                     }
//                   }
//                 },
//                 {
//                   "data": {
//                     "authorById": {
//                       "name": "Author 2 {{GenerateRandomString(128)}}"
//                     }
//                   }
//                 }
//               ]
//               """;
//
//         using var server2 = CreateSourceSchema(
//             "b",
//             """
//             type Query {
//               authorById(id: ID!): Author @lookup
//             }
//
//             type Author {
//               id: ID!
//               name: String!
//             }
//             """,
//             capabilities: SourceSchemaClientCapabilities.ApolloRequestBatching,
//             httpClient: new HttpClient(new MockHttpMessageHandler(jsonArrayResponse)));
//
//         using var gateway = await CreateCompositeSchemaAsync(
//         [
//             ("a", server1),
//             ("b", server2)
//         ]);
//
//         // act
//         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
//
//         var request = new OperationRequest(
//             """
//             {
//               books {
//                 author {
//                   id
//                   name
//                 }
//               }
//             }
//             """);
//
//         using var result = await client.PostAsync(
//             request,
//             new Uri("http://localhost:5000/graphql"));
//
//         // assert
//         await MatchSnapshotAsync(gateway, request, result);
//     }
//
//     [Fact(Skip = "The Gateway needs to produce errors for this")]
//     public async Task Fetch_With_Request_Batching_JsonArray_Returns_Wrong_Number_Of_Items()
//     {
//         // arrange
//         using var server1 = CreateSourceSchema(
//             "a",
//             b => b.AddQueryType<SourceSchema1.Query>());
//
//         // this contains just 2 entries, while it should contain 4.
//         const string jsonArrayResponse =
//             """
//             [
//               {
//                 "data": {
//                   "authorById": {
//                     "name": "Author 1"
//                   }
//                 }
//               },
//               {
//                 "data": {
//                   "authorById": {
//                     "name": "Author 2"
//                   }
//                 }
//               }
//             ]
//             """;
//
//         using var server2 = CreateSourceSchema(
//             "b",
//             """
//             type Query {
//               authorById(id: ID!): Author @lookup
//             }
//
//             type Author {
//               id: ID!
//               name: String!
//             }
//             """,
//             capabilities: SourceSchemaClientCapabilities.ApolloRequestBatching,
//             httpClient: new HttpClient(new MockHttpMessageHandler(jsonArrayResponse)));
//
//         using var gateway = await CreateCompositeSchemaAsync(
//         [
//             ("a", server1),
//             ("b", server2)
//         ]);
//
//         // act
//         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
//
//         var request = new OperationRequest(
//             """
//             {
//               books {
//                 author {
//                   id
//                   name
//                 }
//               }
//             }
//             """);
//
//         using var result = await client.PostAsync(
//             request,
//             new Uri("http://localhost:5000/graphql"));
//
//         // assert
//         await MatchSnapshotAsync(gateway, request, result);
//     }
//
//     [Fact(Skip = "The Gateway needs to produce errors for this")]
//     public async Task Fetch_With_Request_Batching_JsonArray_Returns_Singular_Response()
//     {
//         // arrange
//         using var server1 = CreateSourceSchema(
//             "a",
//             b => b.AddQueryType<SourceSchema1.Query>());
//
//         const string jsonResponse =
//             """
//             {
//               "data": {
//                 "authorById": {
//                   "name": "Author 1"
//                 }
//               }
//             }
//             """;
//
//         using var server2 = CreateSourceSchema(
//             "b",
//             """
//             type Query {
//               authorById(id: ID!): Author @lookup
//             }
//
//             type Author {
//               id: ID!
//               name: String!
//             }
//             """,
//             capabilities: SourceSchemaClientCapabilities.ApolloRequestBatching,
//             httpClient: new HttpClient(new MockHttpMessageHandler(jsonResponse)));
//
//         using var gateway = await CreateCompositeSchemaAsync(
//         [
//             ("a", server1),
//             ("b", server2)
//         ]);
//
//         // act
//         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
//
//         var request = new OperationRequest(
//             """
//             {
//               books {
//                 author {
//                   id
//                   name
//                 }
//               }
//             }
//             """);
//
//         using var result = await client.PostAsync(
//             request,
//             new Uri("http://localhost:5000/graphql"));
//
//         // assert
//         await MatchSnapshotAsync(gateway, request, result);
//     }

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
                new()
                {
                    [1] = new Book(1, "C# in Depth"),
                    [2] = new Book(2, "The Lord of the Rings"),
                    [3] = new Book(3, "The Hobbit"),
                    [4] = new Book(4, "The Silmarillion")
                };

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
            public string GetName() => "Author " + Id;
        }
    }
}
