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
        using var server = CreateSourceSchema("A", SourceSchema);

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
                    "score": 0.8974776268005371
                  },
                  {
                    "coordinate": "User.email",
                    "score": 0.71161288022995
                  },
                  {
                    "coordinate": "User.name",
                    "score": 0.71161288022995
                  },
                  {
                    "coordinate": "User.age",
                    "score": 0.6750305891036987
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
        using var server = CreateSourceSchema("A", SourceSchema);

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
                    "score": 0.9191438555717468
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
        using var server = CreateSourceSchema("A", SourceSchema);

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
                    "score": 0.8174113631248474
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
        using var server = CreateSourceSchema("A", SourceSchema);

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
        using var server = CreateSourceSchema("A", SourceSchema);

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
        using var server = CreateSourceSchema("A", SourceSchema);

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
        using var server = CreateSourceSchema("A", SourceSchema);

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
                        "Query.productSearch",
                        "Product.name"
                      ]
                    ]
                  },
                  {
                    "coordinate": "Query.productSearch",
                    "pathsToRoot": [
                      [
                        "Query.productSearch"
                      ]
                    ]
                  },
                  {
                    "coordinate": "User.name",
                    "pathsToRoot": [
                      [
                        "Query.userByEmail",
                        "User.name"
                      ]
                    ]
                  },
                  {
                    "coordinate": "Product",
                    "pathsToRoot": [
                      [
                        "Query.productSearch"
                      ]
                    ]
                  },
                  {
                    "coordinate": "Product.category",
                    "pathsToRoot": [
                      [
                        "Query.productSearch",
                        "Product.category"
                      ]
                    ]
                  },
                  {
                    "coordinate": "Product.price",
                    "pathsToRoot": [
                      [
                        "Query.productSearch",
                        "Product.price"
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
        using var server = CreateSourceSchema("A", SourceSchema);

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
                    "score": 0.8174113631248474
                  },
                  {
                    "coordinate": "Product.name",
                    "score": 0.8174113631248474
                  },
                  {
                    "coordinate": "Product.price",
                    "score": 0.7237380146980286
                  },
                  {
                    "coordinate": "Query.productSearch",
                    "score": 0.6175786256790161
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
        using var server = CreateSourceSchema("A", SourceSchema);

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
                "__search": [
                  {
                    "coordinate": "Query.userByEmail",
                    "definition": {
                      "name": "userByEmail",
                      "description": "Retrieve a user by their email address",
                      "args": [
                        {
                          "name": "email"
                        }
                      ]
                    }
                  },
                  {
                    "coordinate": "User.email",
                    "definition": {
                      "name": "email",
                      "description": "The email address of the user",
                      "args": []
                    }
                  },
                  {
                    "coordinate": "User",
                    "definition": {}
                  },
                  {
                    "coordinate": "Query.orderById",
                    "definition": {
                      "name": "orderById",
                      "description": "Retrieve an order by its unique identifier",
                      "args": [
                        {
                          "name": "id"
                        }
                      ]
                    }
                  },
                  {
                    "coordinate": "Query.productSearch",
                    "definition": {
                      "name": "productSearch",
                      "description": "Search for products by name or category",
                      "args": [
                        {
                          "name": "term"
                        }
                      ]
                    }
                  },
                  {
                    "coordinate": "User.name",
                    "definition": {
                      "name": "name",
                      "description": "The full name of the user",
                      "args": []
                    }
                  },
                  {
                    "coordinate": "User.age",
                    "definition": {
                      "name": "age",
                      "description": "The age of the user in years",
                      "args": []
                    }
                  },
                  {
                    "coordinate": "Float",
                    "definition": {}
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_ResolveDefinition_AsType()
    {
        // arrange
        using var server = CreateSourceSchema("A", SourceSchema);

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
                            __typename
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
                "__search": [
                  {
                    "coordinate": "Product",
                    "definition": {
                      "__typename": "__Type",
                      "name": "Product",
                      "kind": "OBJECT",
                      "fields": [
                        {
                          "name": "category"
                        },
                        {
                          "name": "name"
                        },
                        {
                          "name": "price"
                        }
                      ]
                    }
                  },
                  {
                    "coordinate": "Product.category",
                    "definition": {
                      "__typename": "__Field"
                    }
                  },
                  {
                    "coordinate": "Product.name",
                    "definition": {
                      "__typename": "__Field"
                    }
                  },
                  {
                    "coordinate": "Product.price",
                    "definition": {
                      "__typename": "__Field"
                    }
                  },
                  {
                    "coordinate": "Query.productSearch",
                    "definition": {
                      "__typename": "__Field"
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Definitions_Should_ResolveTypeByCoordinate()
    {
        // arrange
        using var server = CreateSourceSchema("A", SourceSchema);

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
                "__definitions": [
                  {
                    "name": "User",
                    "kind": "OBJECT",
                    "fields": [
                      {
                        "name": "age"
                      },
                      {
                        "name": "email"
                      },
                      {
                        "name": "name"
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Definitions_Should_ResolveFieldByCoordinate()
    {
        // arrange
        using var server = CreateSourceSchema("A", SourceSchema);

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
                "__definitions": [
                  {
                    "name": "userByEmail",
                    "description": "Retrieve a user by their email address",
                    "args": [
                      {
                        "name": "email",
                        "type": {
                          "name": null,
                          "kind": "NON_NULL"
                        }
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Definitions_Should_ResolveMultipleCoordinates()
    {
        // arrange
        using var server = CreateSourceSchema("A", SourceSchema);

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
                "__definitions": [
                  {
                    "typeName": "User",
                    "kind": "OBJECT"
                  },
                  {
                    "typeName": "Product",
                    "kind": "OBJECT"
                  },
                  {
                    "fieldName": "orderById",
                    "description": "Retrieve an order by its unique identifier"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Definitions_Should_SkipInvalidCoordinates()
    {
        // arrange
        using var server = CreateSourceSchema("A", SourceSchema);

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
                "__definitions": [
                  {
                    "typeName": "User"
                  },
                  {
                    "fieldName": "orderById"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Definitions_Should_ResolveEnumValueByCoordinate()
    {
        // arrange
        using var server = CreateSourceSchema("A", SourceSchema);

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
                "__definitions": [
                  {
                    "name": "PENDING",
                    "description": "Order is pending processing"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_NotExist_When_SemanticIntrospectionDisabled()
    {
        // arrange
        using var server = CreateSourceSchema("A", SourceSchema);

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
        using var server = CreateSourceSchema("A", SourceSchema);

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

    private const string SourceSchema =
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
          price: Decimal!
          "The product category"
          category: String!
        }

        "A customer order"
        type Order {
          "The unique order identifier"
          id: ID!
          "The order total amount"
          total: Decimal!
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
          "List all users with optional filtering"
          users: [User]
          "Search for products by name or category"
          productSearch(term: String!): [Product]
          "Retrieve an order by its unique identifier"
          orderById(id: ID!): Order
        }
        """;
}
