using HotChocolate.Types;

namespace HotChocolate.Execution;

public sealed class SemanticIntrospectionTests
{
    [Fact]
    public async Task Search_Should_ReturnResults_When_QueryMatchesFieldName()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "user") {
                    coordinate
                    score
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
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
                    "score": 0.9113392233848572
                  },
                  {
                    "coordinate": "User.name",
                    "score": 0.732876718044281
                  },
                  {
                    "coordinate": "User.email",
                    "score": 0.732876718044281
                  },
                  {
                    "coordinate": "User.age",
                    "score": 0.699621856212616
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
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "email address") {
                    coordinate
                    score
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
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
                    "score": 0.9289592504501343
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
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "product", first: 2) {
                    coordinate
                    score
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "coordinate": "Product",
                    "score": 1
                  },
                  {
                    "coordinate": "Product.name",
                    "score": 0.827045202255249
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
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "product", first: 100, min_score: 0.9) {
                    coordinate
                    score
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
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
        var executor = CreateSchema().MakeExecutable();

        // Get first page.
        var firstResult = await executor.ExecuteAsync(
            """
            {
                __search(query: "product", first: 1) {
                    cursor
                    coordinate
                }
            }
            """);

        var firstJson = firstResult.ToJson();
        var cursorStart = firstJson.IndexOf("\"cursor\": \"", StringComparison.Ordinal) + 11;
        var cursorEnd = firstJson.IndexOf("\"", cursorStart, StringComparison.Ordinal);
        var cursor = firstJson[cursorStart..cursorEnd];

        // act - Get second page.
        var secondResult = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($after: String) {
                        __search(query: "product", first: 1, after: $after) {
                            cursor
                            coordinate
                        }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { { "after", cursor } })
                .Build());

        // assert
        firstResult.MatchInlineSnapshot(
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

        secondResult.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "cursor": "AgAAAA==",
                    "coordinate": "Product.name"
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
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "xyznonexistent") {
                    coordinate
                    score
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
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
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "product name") {
                    coordinate
                    pathsToRoot
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "coordinate": "Product.name",
                    "pathsToRoot": [
                      "Product.name > Product > Query.productSearch > Query"
                    ]
                  },
                  {
                    "coordinate": "Query.productSearch",
                    "pathsToRoot": [
                      "Query.productSearch > Query"
                    ]
                  },
                  {
                    "coordinate": "User.name",
                    "pathsToRoot": [
                      "User.name > User > Query.userByEmail > Query"
                    ]
                  },
                  {
                    "coordinate": "Product",
                    "pathsToRoot": [
                      "Product > Query.productSearch > Query"
                    ]
                  },
                  {
                    "coordinate": "Product.category",
                    "pathsToRoot": [
                      "Product.category > Product > Query.productSearch > Query"
                    ]
                  },
                  {
                    "coordinate": "Product.price",
                    "pathsToRoot": [
                      "Product.price > Product > Query.productSearch > Query"
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
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "product", first: 100) {
                    coordinate
                    score
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "coordinate": "Product",
                    "score": 1
                  },
                  {
                    "coordinate": "Product.name",
                    "score": 0.827045202255249
                  },
                  {
                    "coordinate": "Product.category",
                    "score": 0.827045202255249
                  },
                  {
                    "coordinate": "Product.price",
                    "score": 0.7444983124732971
                  },
                  {
                    "coordinate": "Query.productSearch",
                    "score": 0.6475508809089661
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
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
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
            """);

        // assert
        result.MatchInlineSnapshot(
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
                    "coordinate": "@specifiedBy",
                    "definition": {}
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
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
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
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "coordinate": "Product",
                    "definition": {
                      "name": "Product",
                      "kind": "OBJECT",
                      "fields": [
                        {
                          "name": "name"
                        },
                        {
                          "name": "price"
                        },
                        {
                          "name": "category"
                        }
                      ]
                    }
                  },
                  {
                    "coordinate": "Product.name",
                    "definition": {}
                  },
                  {
                    "coordinate": "Product.category",
                    "definition": {}
                  },
                  {
                    "coordinate": "Product.price",
                    "definition": {}
                  },
                  {
                    "coordinate": "Query.productSearch",
                    "definition": {}
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
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
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
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__definitions": [
                  {
                    "name": "User",
                    "kind": "OBJECT",
                    "fields": [
                      {
                        "name": "name"
                      },
                      {
                        "name": "email"
                      },
                      {
                        "name": "age"
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
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
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
            """);

        // assert
        result.MatchInlineSnapshot(
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
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
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
            """);

        // assert
        result.MatchInlineSnapshot(
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
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
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
            """);

        // assert
        result.MatchInlineSnapshot(
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
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __definitions(coordinates: ["OrderStatus.PENDING"]) {
                    ... on __EnumValue {
                        name
                        description
                    }
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
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
    public async Task Search_Should_FindUserField_When_AskedNaturalQuestion()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "How do I look up a user by their email address?") {
                    coordinate
                    score
                    definition {
                        ... on __Field {
                            fieldName: name
                            description
                        }
                        ... on __Type {
                            typeName: name
                            kind
                        }
                    }
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "coordinate": "Query.userByEmail",
                    "score": 1,
                    "definition": {
                      "fieldName": "userByEmail",
                      "description": "Retrieve a user by their email address"
                    }
                  },
                  {
                    "coordinate": "User.email",
                    "score": 0.6059101819992065,
                    "definition": {
                      "fieldName": "email",
                      "description": "The email address of the user"
                    }
                  },
                  {
                    "coordinate": "User",
                    "score": 0.19035416841506958,
                    "definition": {
                      "typeName": "User",
                      "kind": "OBJECT"
                    }
                  },
                  {
                    "coordinate": "Query.orderById",
                    "score": 0.1684975028038025,
                    "definition": {
                      "fieldName": "orderById",
                      "description": "Retrieve an order by its unique identifier"
                    }
                  },
                  {
                    "coordinate": "User.name",
                    "score": 0.13950614631175995,
                    "definition": {
                      "fieldName": "name",
                      "description": "The full name of the user"
                    }
                  },
                  {
                    "coordinate": "User.age",
                    "score": 0.13317593932151794,
                    "definition": {
                      "fieldName": "age",
                      "description": "The age of the user in years"
                    }
                  },
                  {
                    "coordinate": "Query.productSearch",
                    "score": 0.12739528715610504,
                    "definition": {
                      "fieldName": "productSearch",
                      "description": "Search for products by name or category"
                    }
                  },
                  {
                    "coordinate": "@specifiedBy",
                    "score": 0.11778272688388824,
                    "definition": {}
                  },
                  {
                    "coordinate": "Float",
                    "score": 0.07715816795825958,
                    "definition": {
                      "typeName": "Float",
                      "kind": "SCALAR"
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_FindProducts_When_AskedAboutShopping()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "I want to search for products to buy") {
                    coordinate
                    score
                    definition {
                        ... on __Field {
                            fieldName: name
                            description
                        }
                        ... on __Type {
                            typeName: name
                            kind
                        }
                    }
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "coordinate": "Query.productSearch",
                    "score": 1,
                    "definition": {
                      "fieldName": "productSearch",
                      "description": "Search for products by name or category"
                    }
                  },
                  {
                    "coordinate": "ID",
                    "score": 0.4061276316642761,
                    "definition": {
                      "typeName": "ID",
                      "kind": "SCALAR"
                    }
                  },
                  {
                    "coordinate": "@specifiedBy",
                    "score": 0.354110985994339,
                    "definition": {}
                  },
                  {
                    "coordinate": "@skip",
                    "score": 0.2957645654678345,
                    "definition": {}
                  },
                  {
                    "coordinate": "@include",
                    "score": 0.286235511302948,
                    "definition": {}
                  },
                  {
                    "coordinate": "Product",
                    "score": 0.2594583332538605,
                    "definition": {
                      "typeName": "Product",
                      "kind": "OBJECT"
                    }
                  },
                  {
                    "coordinate": "@deprecated",
                    "score": 0.19725997745990753,
                    "definition": {}
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_FindOrders_When_AskedAboutOrderTracking()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "How can I track the status of my order?") {
                    coordinate
                    score
                    definition {
                        ... on __Field {
                            fieldName: name
                            description
                        }
                        ... on __Type {
                            typeName: name
                            kind
                        }
                    }
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__search": [
                  {
                    "coordinate": "OrderStatus",
                    "score": 1,
                    "definition": {
                      "typeName": "OrderStatus",
                      "kind": "ENUM"
                    }
                  },
                  {
                    "coordinate": "Order.status",
                    "score": 0.8016138672828674,
                    "definition": {
                      "fieldName": "status",
                      "description": "The current order status"
                    }
                  },
                  {
                    "coordinate": "User.name",
                    "score": 0.28660982847213745,
                    "definition": {
                      "fieldName": "name",
                      "description": "The full name of the user"
                    }
                  },
                  {
                    "coordinate": "User.email",
                    "score": 0.28660982847213745,
                    "definition": {
                      "fieldName": "email",
                      "description": "The email address of the user"
                    }
                  },
                  {
                    "coordinate": "User",
                    "score": 0.2793460786342621,
                    "definition": {
                      "typeName": "User",
                      "kind": "OBJECT"
                    }
                  },
                  {
                    "coordinate": "User.age",
                    "score": 0.27486422657966614,
                    "definition": {
                      "fieldName": "age",
                      "description": "The age of the user in years"
                    }
                  },
                  {
                    "coordinate": "Order.id",
                    "score": 0.25921085476875305,
                    "definition": {
                      "fieldName": "id",
                      "description": "The unique order identifier"
                    }
                  },
                  {
                    "coordinate": "Order.total",
                    "score": 0.25921085476875305,
                    "definition": {
                      "fieldName": "total",
                      "description": "The order total amount"
                    }
                  },
                  {
                    "coordinate": "Order",
                    "score": 0.25802066922187805,
                    "definition": {
                      "typeName": "Order",
                      "kind": "OBJECT"
                    }
                  },
                  {
                    "coordinate": "Query.orderById",
                    "score": 0.2061748057603836,
                    "definition": {
                      "fieldName": "orderById",
                      "description": "Retrieve an order by its unique identifier"
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_FindFields_When_AskedAboutPricing()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "What pricing information is available?") {
                    coordinate
                    score
                    definition {
                        ... on __Field {
                            fieldName: name
                            description
                        }
                        ... on __Type {
                            typeName: name
                            kind
                        }
                    }
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            PLACEHOLDER
            """);
    }

    [Fact]
    public async Task Search_Should_NotExist_When_SemanticIntrospectionDisabled()
    {
        // arrange
        var executor = CreateSchemaWithSemanticIntrospectionDisabled().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "user") {
                    coordinate
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
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
        var executor = CreateSchemaWithSemanticIntrospectionDisabled().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __definitions(coordinates: ["User"]) {
                    ... on __Type {
                        name
                    }
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
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

    private static Schema CreateSchema()
    {
        return SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType<UserType>()
            .AddType<ProductType>()
            .AddType<OrderType>()
            .AddType<OrderStatusType>()
            .Use(next => next)
            .Create();
    }

    private static Schema CreateSchemaWithSemanticIntrospectionDisabled()
    {
        return SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType<UserType>()
            .AddType<ProductType>()
            .AddType<OrderType>()
            .AddType<OrderStatusType>()
            .ModifyOptions(o => o.EnableSemanticIntrospection = false)
            .Use(next => next)
            .Create();
    }

    // -- Test schema types --

    private sealed class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name(OperationTypeNames.Query);

            descriptor.Field("userByEmail")
                .Description("Retrieve a user by their email address")
                .Argument("email", a => a.Type<NonNullType<StringType>>())
                .Type<UserType>()
                .Resolve(new User("Alice", "alice@example.com", 30));

            descriptor.Field("users")
                .Description("List all users with optional filtering")
                .Type<ListType<UserType>>()
                .Resolve(Array.Empty<object>());

            descriptor.Field("productSearch")
                .Description("Search for products by name or category")
                .Argument("term", a => a.Type<NonNullType<StringType>>())
                .Type<ListType<ProductType>>()
                .Resolve(Array.Empty<object>());

            descriptor.Field("orderById")
                .Description("Retrieve an order by its unique identifier")
                .Argument("id", a => a.Type<NonNullType<IdType>>())
                .Type<OrderType>()
                .Resolve(new Order("ORD-001", 99.99m, "PENDING"));
        }
    }

    private record User(string Name, string Email, int Age);

    private sealed class UserType : ObjectType<User>
    {
        protected override void Configure(IObjectTypeDescriptor<User> descriptor)
        {
            descriptor.Name("User");
            descriptor.Description("A registered user of the system");

            descriptor.Field(u => u.Name)
                .Description("The full name of the user")
                .Type<NonNullType<StringType>>();

            descriptor.Field(u => u.Email)
                .Description("The email address of the user")
                .Type<NonNullType<StringType>>();

            descriptor.Field(u => u.Age)
                .Description("The age of the user in years")
                .Type<NonNullType<IntType>>();
        }
    }

    private record Product(string Name, decimal Price, string Category);

    private sealed class ProductType : ObjectType<Product>
    {
        protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
        {
            descriptor.Name("Product");
            descriptor.Description("A product available for purchase");

            descriptor.Field(p => p.Name)
                .Description("The product name")
                .Type<NonNullType<StringType>>();

            descriptor.Field(p => p.Price)
                .Description("The product price in dollars")
                .Type<NonNullType<DecimalType>>();

            descriptor.Field(p => p.Category)
                .Description("The product category")
                .Type<NonNullType<StringType>>();
        }
    }

    private record Order(string Id, decimal Total, string Status);

    private sealed class OrderType : ObjectType<Order>
    {
        protected override void Configure(IObjectTypeDescriptor<Order> descriptor)
        {
            descriptor.Name("Order");
            descriptor.Description("A customer order");

            descriptor.Field(o => o.Id)
                .Description("The unique order identifier")
                .Type<NonNullType<IdType>>();

            descriptor.Field(o => o.Total)
                .Description("The order total amount")
                .Type<NonNullType<DecimalType>>();

            descriptor.Field("status")
                .Description("The current order status")
                .Type<OrderStatusType>()
                .Resolve("PENDING");
        }
    }

    private enum OrderStatus
    {
        Pending,
        Shipped,
        Delivered,
        Cancelled
    }

    private sealed class OrderStatusType : EnumType<OrderStatus>
    {
        protected override void Configure(IEnumTypeDescriptor<OrderStatus> descriptor)
        {
            descriptor.Name("OrderStatus");
            descriptor.Description("The status of an order");

            descriptor.Value(OrderStatus.Pending)
                .Name("PENDING")
                .Description("Order is pending processing");

            descriptor.Value(OrderStatus.Shipped)
                .Name("SHIPPED")
                .Description("Order has been shipped");

            descriptor.Value(OrderStatus.Delivered)
                .Name("DELIVERED")
                .Description("Order has been delivered");

            descriptor.Value(OrderStatus.Cancelled)
                .Name("CANCELLED")
                .Description("Order has been cancelled");
        }
    }
}
