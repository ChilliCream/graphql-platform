using HotChocolate.AspNetCore;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Fusion;

public class SemanticIntrospectionTests : FusionTestBase
{
    [Fact]
    public async Task Search_Should_ReturnResults_When_QueryMatchesFieldName()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __search(query: "user") {
                        coordinate
                        score
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "coordinate": "User",
                    "score": 1
                  },
                  {
                    "coordinate": "Query.userByEmail",
                    "score": 0.8877184
                  },
                  {
                    "coordinate": "User.email",
                    "score": 0.69699603
                  },
                  {
                    "coordinate": "User.name",
                    "score": 0.69699603
                  },
                  {
                    "coordinate": "User.age",
                    "score": 0.65830594
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_ReturnResults_When_QueryMatchesDescription()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __search(query: "email address") {
                        coordinate
                        score
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "coordinate": "User.email",
                    "score": 1
                  },
                  {
                    "coordinate": "Query.userByEmail",
                    "score": 0.9123855
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_RespectFirstArgument()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __search(query: "product", first: 2) {
                        coordinate
                        score
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "coordinate": "Product",
                    "score": 1
                  },
                  {
                    "coordinate": "Product.category",
                    "score": 0.81051797
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_FilterByMinScore()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __search(query: "product", first: 100, min_score: 0.9) {
                        coordinate
                        score
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "coordinate": "Product",
                    "score": 1
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_ReturnCursors_And_SupportPagination()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // Get first page.
        using var firstResult = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __search(query: "product", first: 1) {
                        cursor
                        coordinate
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        using var firstResponse = await firstResult.ReadAsResultAsync();
        var firstJson = firstResponse.Data.ToString()!;
        var cursorStart = firstJson.IndexOf("\"cursor\":\"", StringComparison.Ordinal) + 10;
        var cursorEnd = firstJson.IndexOf("\"", cursorStart, StringComparison.Ordinal);
        var cursor = firstJson[cursorStart..cursorEnd];

        // act - Get second page.
        using var secondResult = await client.PostAsync(
            new OperationRequest(
                query: """
                       query($after: String) {
                           __search(query: "product", first: 1, after: $after) {
                               cursor
                               coordinate
                           }
                       }
                       """,
                variables: new Dictionary<string, object?> { { "after", cursor } }),
            new Uri("http://localhost:5000/graphql"));

        // assert
        firstResponse.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "cursor": "AQAAAA==",
                    "coordinate": "Product"
                  }
                ]
              }
            }
            """);

        using var secondResponse = await secondResult.ReadAsResultAsync();
        secondResponse.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "cursor": "AgAAAA==",
                    "coordinate": "Product.category"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_ReturnEmptyList_When_NoMatches()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __search(query: "xyznonexistent") {
                        coordinate
                        score
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": []
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_IncludePathsToRoot()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __search(query: "product name") {
                        coordinate
                        pathsToRoot
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "coordinate": "Product.name",
                    "pathsToRoot": [
                      [
                        "Product.name",
                        "Product",
                        "Query.productSearch",
                        "Query"
                      ]
                    ]
                  },
                  {
                    "coordinate": "Query.productSearch",
                    "pathsToRoot": [
                      [
                        "Query.productSearch",
                        "Query"
                      ]
                    ]
                  },
                  {
                    "coordinate": "User.name",
                    "pathsToRoot": [
                      [
                        "User.name",
                        "User",
                        "Query.userByEmail",
                        "Query"
                      ]
                    ]
                  },
                  {
                    "coordinate": "Product",
                    "pathsToRoot": [
                      [
                        "Product",
                        "Query.productSearch",
                        "Query"
                      ]
                    ]
                  },
                  {
                    "coordinate": "Product.category",
                    "pathsToRoot": [
                      [
                        "Product.category",
                        "Product",
                        "Query.productSearch",
                        "Query"
                      ]
                    ]
                  },
                  {
                    "coordinate": "Product.price",
                    "pathsToRoot": [
                      [
                        "Product.price",
                        "Product",
                        "Query.productSearch",
                        "Query"
                      ]
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_ReturnScoresInDescendingOrder()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __search(query: "product", first: 100) {
                        coordinate
                        score
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "coordinate": "Product",
                    "score": 1
                  },
                  {
                    "coordinate": "Product.category",
                    "score": 0.81051797
                  },
                  {
                    "coordinate": "Product.name",
                    "score": 0.81051797
                  },
                  {
                    "coordinate": "Product.price",
                    "score": 0.70929694
                  },
                  {
                    "coordinate": "Query.productSearch",
                    "score": 0.5973899
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_ResolveDefinition_AsField()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __search(query: "userByEmail") {
                        coordinate
                        definition {
                            ... on __Field {
                                name
                                description
                                args {
                                    name
                                }
                            }
                        }
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": null
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_ResolveDefinition_AsType()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __search(query: "Product") {
                        coordinate
                        definition {
                            ... on __Type {
                                name
                                kind
                                fields {
                                    name
                                }
                            }
                        }
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": null
              }
            }
            """);
    }

    [Fact]
    public async Task Definitions_Should_ResolveTypeByCoordinate()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __definitions(coordinates: ["User"]) {
                        ... on __Type {
                            name
                            kind
                            fields {
                                name
                            }
                        }
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__definitions": null
              }
            }
            """);
    }

    [Fact]
    public async Task Definitions_Should_ResolveFieldByCoordinate()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __definitions(coordinates: ["Query.userByEmail"]) {
                        ... on __Field {
                            name
                            description
                            args {
                                name
                                type {
                                    name
                                    kind
                                }
                            }
                        }
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__definitions": null
              }
            }
            """);
    }

    [Fact]
    public async Task Definitions_Should_ResolveMultipleCoordinates()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __definitions(coordinates: ["User", "Product", "Query.orderById"]) {
                        ... on __Type {
                            typeName: name
                            kind
                        }
                        ... on __Field {
                            fieldName: name
                            description
                        }
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__definitions": null
              }
            }
            """);
    }

    [Fact]
    public async Task Definitions_Should_SkipInvalidCoordinates()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __definitions(coordinates: ["User", "NonExistentType", "Query.orderById"]) {
                        ... on __Type {
                            typeName: name
                        }
                        ... on __Field {
                            fieldName: name
                        }
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__definitions": null
              }
            }
            """);
    }

    [Fact]
    public async Task Definitions_Should_ResolveEnumValueByCoordinate()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __definitions(coordinates: ["OrderStatus.PENDING"]) {
                        ... on __EnumValue {
                            name
                            description
                        }
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__definitions": null
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_NotExist_When_SemanticIntrospectionDisabled()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server, enableSemanticIntrospection: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __search(query: "user") {
                        coordinate
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The field `__search` does not exist on the type `Query`.",
                  "locations": [
                    {
                      "line": 2,
                      "column": 5
                    }
                  ],
                  "extensions": {
                    "type": "Query",
                    "field": "__search",
                    "responseName": "__search",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Field-Selections"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Definitions_Should_NotExist_When_SemanticIntrospectionDisabled()
    {
        // arrange
        using var server = CreateSourceSchema("A", _sourceSchema);

        using var gateway = await CreateGatewayAsync(server, enableSemanticIntrospection: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                    __definitions(coordinates: ["User"]) {
                        ... on __Type {
                            name
                        }
                    }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The field `__definitions` does not exist on the type `Query`.",
                  "locations": [
                    {
                      "line": 2,
                      "column": 5
                    }
                  ],
                  "extensions": {
                    "type": "Query",
                    "field": "__definitions",
                    "responseName": "__definitions",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Field-Selections"
                  }
                }
              ]
            }
            """);
    }

    private Task<Gateway> CreateGatewayAsync(
        Microsoft.AspNetCore.TestHost.TestServer server,
        bool enableSemanticIntrospection = true)
    {
        return CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b =>
            {
                b.ModifyOptions(o => o.EnableSemanticIntrospection = enableSemanticIntrospection);

                b.ConfigureSchemaServices((_, s) =>
                {
                    s.RemoveAll<IHttpRequestInterceptor>();
                    s.AddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>();
                });
            });
    }

    private const string _sourceSchema =
        """
        "A registered user of the system"
        type User {
          "The full name of the user"
          name: String!
          "The email address of the user"
          email: String!
          "The age of the user in years"
          age: Int!
        }

        "A product available for purchase"
        type Product {
          "The product name"
          name: String!
          "The product price in dollars"
          price: Float!
          "The product category"
          category: String!
        }

        "A customer order"
        type Order {
          "The unique order identifier"
          id: ID!
          "The order total amount"
          total: Float!
          "The current order status"
          status: OrderStatus
        }

        "The status of an order"
        enum OrderStatus {
          "Order is pending processing"
          PENDING
          "Order has been shipped"
          SHIPPED
          "Order has been delivered"
          DELIVERED
          "Order has been cancelled"
          CANCELLED
        }

        type Query {
          "Retrieve a user by their email address"
          userByEmail(email: String!): User
          "Search for products by name or category"
          productSearch(term: String!): [Product]
          "Retrieve an order by its unique identifier"
          orderById(id: ID!): Order
        }
        """;
}
