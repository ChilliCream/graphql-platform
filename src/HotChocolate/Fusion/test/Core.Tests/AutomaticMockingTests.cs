using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Fusion.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class AutomaticMockingTests
{
    #region Objects

    [Fact]
    public async Task Object()
    {
        // arrange
        var schema =
            """
            type Query {
              obj: Object
            }

            type Object {
              id: ID!
              str: String!
            }
            """;
        var request =
            """
            query {
              obj {
                id
                str
              }
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "obj": {
                  "id": "1",
                  "str": "string"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Object_List()
    {
        // arrange
        var schema =
            """
            type Query {
              objs: [Object!]!
            }

            type Object {
              id: ID!
              str: String!
            }
            """;
        var request =
            """
            query {
              objs {
                id
                str
              }
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "objs": [
                  {
                    "id": "1",
                    "str": "string"
                  },
                  {
                    "id": "2",
                    "str": "string"
                  },
                  {
                    "id": "3",
                    "str": "string"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Object_List_NullAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              objs: [Object] @null(atIndex: 1)
            }

            type Object {
              id: ID!
            }
            """;
        var request =
            """
            query {
              objs {
                id
              }
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "objs": [
                  {
                    "id": "1"
                  },
                  null,
                  {
                    "id": "2"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Object_List_ErrorAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              objs: [Object] @error(atIndex: 1)
            }

            type Object {
              id: ID!
            }
            """;
        var request =
            """
            query {
              objs {
                id
              }
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "locations": [
                    {
                      "line": 2,
                      "column": 3
                    }
                  ],
                  "path": [
                    "objs",
                    1
                  ]
                }
              ],
              "data": {
                "objs": [
                  {
                    "id": "1"
                  },
                  null,
                  {
                    "id": "2"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Object_List_Property_NullAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              objs: [Object!]!
            }

            type Object {
              str: String @null(atIndex: 1)
            }
            """;
        var request =
            """
            query {
              objs {
                str
              }
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "objs": [
                  {
                    "str": "string"
                  },
                  {
                    "str": null
                  },
                  {
                    "str": "string"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Object_List_Property_ErrorAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              objs: [Object!]!
            }

            type Object {
              str: String @error(atIndex: 1)
            }
            """;
        var request =
            """
            query {
              objs {
                str
              }
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "locations": [
                    {
                      "line": 3,
                      "column": 5
                    }
                  ],
                  "path": [
                    "objs",
                    1,
                    "str"
                  ]
                }
              ],
              "data": {
                "objs": [
                  {
                    "str": "string"
                  },
                  {
                    "str": null
                  },
                  {
                    "str": "string"
                  }
                ]
              }
            }
            """);
    }

    #endregion

    #region Scalars

    [Fact]
    public async Task Scalar()
    {
        // arrange
        var schema =
            """
            type Query {
              str: String
            }
            """;
        var request =
            """
            query {
              str
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "str": "string"
              }
            }
            """);
    }

    [Fact]
    public async Task Scalar_List()
    {
        // arrange
        var schema =
            """
            type Query {
              scalars: [String!]!
            }
            """;
        var request =
            """
            query {
              scalars
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "scalars": [
                  "string",
                  "string",
                  "string"
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Scalar_List_NullAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              scalars: [String] @null(atIndex: 1)
            }
            """;
        var request =
            """
            query {
              scalars
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "scalars": [
                  "string",
                  null,
                  "string"
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Scalar_List_ErrorAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              scalars: [String] @error(atIndex: 1)
            }
            """;
        var request =
            """
            query {
              scalars
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "locations": [
                    {
                      "line": 2,
                      "column": 3
                    }
                  ],
                  "path": [
                    "scalars",
                    1
                  ]
                }
              ],
              "data": {
                "scalars": [
                  "string",
                  null,
                  "string"
                ]
              }
            }
            """);
    }

    #endregion

    #region Enums

    [Fact]
    public async Task Enum()
    {
        // arrange
        var schema =
            """
            type Query {
              enm: MyEnum
            }

            enum MyEnum {
              VALUE
            }
            """;
        var request =
            """
            query {
              enm
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "enm": "VALUE"
              }
            }
            """);
    }

    [Fact]
    public async Task Enum_List()
    {
        // arrange
        var schema =
            """
            type Query {
              enums: [MyEnum]
            }

            enum MyEnum {
              VALUE
            }
            """;
        var request =
            """
            query {
              enums
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "enums": [
                  "VALUE",
                  "VALUE",
                  "VALUE"
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Enum_List_NullAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              enums: [MyEnum] @null(atIndex: 1)
            }

            enum MyEnum {
              VALUE
            }
            """;
        var request =
            """
            query {
              enums
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "enums": [
                  "VALUE",
                  null,
                  "VALUE"
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Enum_List_ErrorAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              enums: [MyEnum] @error(atIndex: 1)
            }

            enum MyEnum {
              VALUE
            }
            """;
        var request =
            """
            query {
              enums
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "locations": [
                    {
                      "line": 2,
                      "column": 3
                    }
                  ],
                  "path": [
                    "enums",
                    1
                  ]
                }
              ],
              "data": {
                "enums": [
                  "VALUE",
                  null,
                  "VALUE"
                ]
              }
            }
            """);
    }

    #endregion

    #region byId

    [Fact]
    public async Task ById()
    {
        // arrange
        var schema =
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product {
              id: ID!
            }
            """;
        var request =
            """
            query {
              productById(id: "5") {
                id
              }
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "productById": {
                  "id": "5"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task ById_Null()
    {
        // arrange
        var schema =
            """
            type Query {
              productById(id: ID!): Product @null
            }

            type Product {
              id: ID!
            }
            """;
        var request =
            """
            query {
              productById(id: "5") {
                id
              }
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "productById": null
              }
            }
            """);
    }

    [Fact]
    public async Task ById_Error()
    {
        // arrange
        var schema =
            """
            type Query {
              productById(id: ID!): Product @error
            }

            type Product {
              id: ID!
            }
            """;
        var request =
            """
            query {
              productById(id: "5") {
                id
              }
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "locations": [
                    {
                      "line": 2,
                      "column": 3
                    }
                  ],
                  "path": [
                    "productById"
                  ]
                }
              ],
              "data": {
                "productById": null
              }
            }
            """);
    }

    #endregion

    #region byIds

    [Fact]
    public async Task ByIds()
    {
        // arrange
        var schema =
            """
            type Query {
              productsById(ids: [ID!]!): [Product!]!
            }

            type Product {
              id: ID!
            }
            """;
        var request =
            """
            query {
              productsById(ids: ["5", "6"]) {
                id
              }
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "productsById": [
                  {
                    "id": "5"
                  },
                  {
                    "id": "6"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ByIds_Null()
    {
        // arrange
        var schema =
            """
            type Query {
              productsById(ids: [ID!]!): [Product!] @null
            }

            type Product {
              id: ID!
            }
            """;
        var request =
            """
            query {
              productsById(ids: ["5", "6"]) {
                id
              }
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "productsById": null
              }
            }
            """);
    }

    [Fact]
    public async Task ByIds_Error()
    {
        // arrange
        var schema =
            """
            type Query {
              productsById(ids: [ID!]!): [Product!] @error
            }

            type Product {
              id: ID!
            }
            """;
        var request =
            """
            query {
              productsById(ids: ["5", "6"]) {
                id
              }
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "locations": [
                    {
                      "line": 2,
                      "column": 3
                    }
                  ],
                  "path": [
                    "productsById"
                  ]
                }
              ],
              "data": {
                "productsById": null
              }
            }
            """);
    }

    [Fact]
    public async Task ByIds_NullAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              productsById(ids: [ID!]!): [Product] @null(atIndex: 1)
            }

            type Product {
              id: ID!
            }
            """;
        var request =
            """
            query {
              productsById(ids: ["5", "6"]) {
                id
              }
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "productsById": [
                  {
                    "id": "5"
                  },
                  null
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ByIds_ErrorAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              productsById(ids: [ID!]!): [Product] @error(atIndex: 1)
            }

            type Product {
              id: ID!
            }
            """;
        var request =
            """
            query {
              productsById(ids: ["5", "6"]) {
                id
              }
            }
            """;

        // act
        var result = await ExecuteRequestAgainstSchemaAsync(request, schema);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "locations": [
                    {
                      "line": 2,
                      "column": 3
                    }
                  ],
                  "path": [
                    "productsById",
                    1
                  ]
                }
              ],
              "data": {
                "productsById": [
                  {
                    "id": "5"
                  },
                  null
                ]
              }
            }
            """);
    }

    #endregion

    private static async Task<IExecutionResult> ExecuteRequestAgainstSchemaAsync(
        string request,
        string schemaText)
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(schemaText)
            .AddResolverMocking()
            .AddTestDirectives()
            .BuildRequestExecutorAsync();

        return await executor.ExecuteAsync(request);
    }
}
