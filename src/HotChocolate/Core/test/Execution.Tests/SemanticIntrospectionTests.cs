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
                    "score": 0.8974776268005371
                  },
                  {
                    "coordinate": "User.name",
                    "score": 0.71161288022995
                  },
                  {
                    "coordinate": "User.email",
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
                    "score": 0.8174113631248474
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Search_Should_ErrorOn_FirstExceedingLimit()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "product", first: 151) {
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
                  "message": "The `first` argument must not exceed 150.",
                  "path": [
                    "__search"
                  ]
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task Search_Should_ErrorOn_FirstLessThanOrEqualToZero()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "product", first: 0) {
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
                  "message": "The `first` argument must be greater than zero.",
                  "path": [
                    "__search"
                  ]
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task Definitions_Should_ErrorOn_CoordinatesExceedingLimit()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();
        var coordinates = string.Join(", ", Enumerable.Repeat("\"User\"", 151));

        // act
        var result = await executor.ExecuteAsync(
            $$"""
            {
                __definitions(coordinates: [{{coordinates}}]) {
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
                  "message": "The `coordinates` argument must not exceed 150 items.",
                  "path": [
                    "__definitions"
                  ]
                }
              ],
              "data": null
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
                    "score": 0.8174113631248474
                  },
                  {
                    "coordinate": "Product.category",
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
    public async Task Definitions_Should_ErrorOn_UnknownCoordinate()
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
        result.MatchSnapshot();
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
                    "score": 0.5982846021652222,
                    "definition": {
                      "fieldName": "email",
                      "description": "The email address of the user"
                    }
                  },
                  {
                    "coordinate": "User",
                    "score": 0.1880880743265152,
                    "definition": {
                      "typeName": "User",
                      "kind": "OBJECT"
                    }
                  },
                  {
                    "coordinate": "Query.orderById",
                    "score": 0.18292827904224396,
                    "definition": {
                      "fieldName": "orderById",
                      "description": "Retrieve an order by its unique identifier"
                    }
                  },
                  {
                    "coordinate": "Query.productSearch",
                    "score": 0.1353328675031662,
                    "definition": {
                      "fieldName": "productSearch",
                      "description": "Search for products by name or category"
                    }
                  },
                  {
                    "coordinate": "User.name",
                    "score": 0.13384589552879333,
                    "definition": {
                      "fieldName": "name",
                      "description": "The full name of the user"
                    }
                  },
                  {
                    "coordinate": "User.age",
                    "score": 0.1269652098417282,
                    "definition": {
                      "fieldName": "age",
                      "description": "The age of the user in years"
                    }
                  },
                  {
                    "coordinate": "Float",
                    "score": 0.07807315140962601,
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
                    "score": 0.5671203136444092,
                    "definition": {
                      "typeName": "ID",
                      "kind": "SCALAR"
                    }
                  },
                  {
                    "coordinate": "Product",
                    "score": 0.2852042019367218,
                    "definition": {
                      "typeName": "Product",
                      "kind": "OBJECT"
                    }
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
                    "score": 0.789767861366272,
                    "definition": {
                      "fieldName": "status",
                      "description": "The current order status"
                    }
                  },
                  {
                    "coordinate": "User.name",
                    "score": 0.32556161284446716,
                    "definition": {
                      "fieldName": "name",
                      "description": "The full name of the user"
                    }
                  },
                  {
                    "coordinate": "User.email",
                    "score": 0.32556161284446716,
                    "definition": {
                      "fieldName": "email",
                      "description": "The email address of the user"
                    }
                  },
                  {
                    "coordinate": "User",
                    "score": 0.31599652767181396,
                    "definition": {
                      "typeName": "User",
                      "kind": "OBJECT"
                    }
                  },
                  {
                    "coordinate": "User.age",
                    "score": 0.3104621171951294,
                    "definition": {
                      "fieldName": "age",
                      "description": "The age of the user in years"
                    }
                  },
                  {
                    "coordinate": "Order.id",
                    "score": 0.25484970211982727,
                    "definition": {
                      "fieldName": "id",
                      "description": "The unique order identifier"
                    }
                  },
                  {
                    "coordinate": "Order.total",
                    "score": 0.25484970211982727,
                    "definition": {
                      "fieldName": "total",
                      "description": "The order total amount"
                    }
                  },
                  {
                    "coordinate": "Order",
                    "score": 0.24078181385993958,
                    "definition": {
                      "typeName": "Order",
                      "kind": "OBJECT"
                    }
                  },
                  {
                    "coordinate": "String",
                    "score": 0.2084837555885315,
                    "definition": {
                      "typeName": "String",
                      "kind": "SCALAR"
                    }
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

    [Fact]
    public async Task PathsToRoot_Should_BeCorrect_When_CoordinateIsFieldOnRootType()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "orderById", first: 1) {
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
                    "coordinate": "Query.orderById",
                    "pathsToRoot": [
                      [
                        "Query.orderById"
                      ]
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task PathsToRoot_Should_BeCorrect_When_CoordinateIsFieldOnNestedType()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "email address", first: 1) {
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
                    "coordinate": "User.email",
                    "pathsToRoot": [
                      [
                        "Query.userByEmail",
                        "User.email"
                      ]
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task PathsToRoot_Should_BeCorrect_When_CoordinateIsObjectType()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "Product", first: 1) {
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
                    "coordinate": "Product",
                    "pathsToRoot": [
                      [
                        "Query.productSearch"
                      ]
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task PathsToRoot_Should_BeCorrect_When_CoordinateIsScalarType()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "Decimal", first: 1) {
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
                    "coordinate": "Decimal",
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
    public async Task PathsToRoot_Should_BeCorrect_When_CoordinateIsEnumType()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "OrderStatus", first: 2) {
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
                    "coordinate": "Order.status",
                    "pathsToRoot": [
                      [
                        "Query.orderById",
                        "Order.status"
                      ]
                    ]
                  },
                  {
                    "coordinate": "OrderStatus",
                    "pathsToRoot": [
                      [
                        "Query.orderById",
                        "Order.status"
                      ]
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task PathsToRoot_Should_BeCorrect_When_CoordinateIsEnumValue()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "PENDING", first: 1) {
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
                    "coordinate": "OrderStatus.PENDING",
                    "pathsToRoot": [
                      [
                        "Query.orderById",
                        "Order.status",
                        "OrderStatus.PENDING"
                      ]
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task PathsToRoot_Should_BeEmpty_When_CoordinateIsRootType()
    {
        // arrange
        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                __search(query: "Query", first: 1) {
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
                    "coordinate": "Query",
                    "pathsToRoot": []
                  }
                ]
              }
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
