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
    public async Task Object_Null()
    {
        // arrange
        var schema =
            """
            type Query {
              obj: Object @null
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
                "obj": null
              }
            }
            """);
    }

    [Fact]
    public async Task Object_Error()
    {
        // arrange
        var schema =
            """
            type Query {
              obj: Object @error
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
                    "obj"
                  ]
                }
              ],
              "data": {
                "obj": null
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
    public async Task Object_List_Twice()
    {
        // arrange
        var schema =
            """
            type Query {
              objsA: [Object!]!
              objsB: [Object!]!
            }

            type Object {
              id: ID!
              str: String!
            }
            """;
        var request =
            """
            query {
              objsA {
                id
                str
              }
              objsB {
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
                "objsA": [
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
                ],
                "objsB": [
                  {
                    "id": "4",
                    "str": "string"
                  },
                  {
                    "id": "5",
                    "str": "string"
                  },
                  {
                    "id": "6",
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
                    "id": "3"
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
                    "id": "3"
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

    #region Interfaces

    [Fact]
    public async Task Interface()
    {
        // arrange
        var schema =
            """
            type Query {
              intrface: Interface
            }

            interface Interface {
              id: ID!
              str: String!
            }

            type Object implements Interface {
              id: ID!
              str: String!
              num: Int!
            }
            """;
        var request =
            """
            query {
              intrface {
                __typename
                id
                str
                ... on Object {
                  num
                }
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
                "intrface": {
                  "__typename": "Object",
                  "id": "1",
                  "str": "string",
                  "num": 123
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Interface_Null()
    {
        // arrange
        var schema =
            """
            type Query {
              intrface: Interface @null
            }

            interface Interface {
              id: ID!
              str: String!
            }

            type Object implements Interface {
              id: ID!
              str: String!
              num: Int!
            }
            """;
        var request =
            """
            query {
              intrface {
                __typename
                id
                str
                ... on Object {
                  num
                }
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
                "intrface": null
              }
            }
            """);
    }

    [Fact]
    public async Task Interface_Error()
    {
        // arrange
        var schema =
            """
            type Query {
              intrface: Interface @error
            }

            interface Interface {
              id: ID!
              str: String!
            }

            type Object implements Interface {
              id: ID!
              str: String!
              num: Int!
            }
            """;
        var request =
            """
            query {
              intrface {
                __typename
                id
                str
                ... on Object {
                  num
                }
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
                    "intrface"
                  ]
                }
              ],
              "data": {
                "intrface": null
              }
            }
            """);
    }

    [Fact]
    public async Task Interface_List()
    {
        // arrange
        var schema =
            """
            type Query {
              interfaces: [Interface]
            }

            interface Interface {
              id: ID!
              str: String!
            }

            type Object implements Interface {
              id: ID!
              str: String!
              num: Int!
            }
            """;
        var request =
            """
            query {
              interfaces {
                __typename
                id
                str
                ... on Object {
                  num
                }
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
                "interfaces": [
                  {
                    "__typename": "Object",
                    "id": "1",
                    "str": "string",
                    "num": 123
                  },
                  {
                    "__typename": "Object",
                    "id": "2",
                    "str": "string",
                    "num": 123
                  },
                  {
                    "__typename": "Object",
                    "id": "3",
                    "str": "string",
                    "num": 123
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Interface_List_Null()
    {
        // arrange
        var schema =
            """
            type Query {
              interfaces: [Interface] @null
            }

            interface Interface {
              id: ID!
              str: String!
            }

            type Object implements Interface {
              id: ID!
              str: String!
              num: Int!
            }
            """;
        var request =
            """
            query {
              interfaces {
                __typename
                id
                str
                ... on Object {
                  num
                }
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
                "interfaces": null
              }
            }
            """);
    }

    [Fact]
    public async Task Interface_List_Error()
    {
        // arrange
        var schema =
            """
            type Query {
              interfaces: [Interface] @error
            }

            interface Interface {
              id: ID!
              str: String!
            }

            type Object implements Interface {
              id: ID!
              str: String!
              num: Int!
            }
            """;
        var request =
            """
            query {
              interfaces {
                __typename
                id
                str
                ... on Object {
                  num
                }
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
                    "interfaces"
                  ]
                }
              ],
              "data": {
                "interfaces": null
              }
            }
            """);
    }

    [Fact]
    public async Task Interface_List_NullAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              interfaces: [Interface] @null(atIndex: 1)
            }

            interface Interface {
              id: ID!
              str: String!
            }

            type Object implements Interface {
              id: ID!
              str: String!
              num: Int!
            }
            """;
        var request =
            """
            query {
              interfaces {
                __typename
                id
                str
                ... on Object {
                  num
                }
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
                "interfaces": [
                  {
                    "__typename": "Object",
                    "id": "1",
                    "str": "string",
                    "num": 123
                  },
                  null,
                  {
                    "__typename": "Object",
                    "id": "3",
                    "str": "string",
                    "num": 123
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Interface_List_ErrorAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              interfaces: [Interface] @error(atIndex: 1)
            }

            interface Interface {
              id: ID!
              str: String!
            }

            type Object implements Interface {
              id: ID!
              str: String!
              num: Int!
            }
            """;
        var request =
            """
            query {
              interfaces {
                __typename
                id
                str
                ... on Object {
                  num
                }
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
                    "interfaces",
                    1
                  ]
                }
              ],
              "data": {
                "interfaces": [
                  {
                    "__typename": "Object",
                    "id": "1",
                    "str": "string",
                    "num": 123
                  },
                  null,
                  {
                    "__typename": "Object",
                    "id": "3",
                    "str": "string",
                    "num": 123
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Interface_List_Property_NullAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              interfaces: [Interface]
            }

            interface Interface {
              id: ID!
              str: String!
            }

            type Object implements Interface {
              id: ID!
              str: String!
              num: Int @null(atIndex: 1)
            }
            """;
        var request =
            """
            query {
              interfaces {
                __typename
                id
                str
                ... on Object {
                  num
                }
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
                "interfaces": [
                  {
                    "__typename": "Object",
                    "id": "1",
                    "str": "string",
                    "num": 123
                  },
                  {
                    "__typename": "Object",
                    "id": "2",
                    "str": "string",
                    "num": null
                  },
                  {
                    "__typename": "Object",
                    "id": "3",
                    "str": "string",
                    "num": 123
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Interface_List_Property_ErrorAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              interfaces: [Interface]
            }

            interface Interface {
              id: ID!
              str: String!
            }

            type Object implements Interface {
              id: ID!
              str: String!
              num: Int @error(atIndex: 1)
            }
            """;
        var request =
            """
            query {
              interfaces {
                __typename
                id
                str
                ... on Object {
                  num
                }
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
                      "line": 7,
                      "column": 7
                    }
                  ],
                  "path": [
                    "interfaces",
                    1,
                    "num"
                  ]
                }
              ],
              "data": {
                "interfaces": [
                  {
                    "__typename": "Object",
                    "id": "1",
                    "str": "string",
                    "num": 123
                  },
                  {
                    "__typename": "Object",
                    "id": "2",
                    "str": "string",
                    "num": null
                  },
                  {
                    "__typename": "Object",
                    "id": "3",
                    "str": "string",
                    "num": 123
                  }
                ]
              }
            }
            """);
    }

    #endregion

    #region Union

    [Fact]
    public async Task Union()
    {
        // arrange
        var schema =
            """
            type Query {
              unon: Union
            }

            union Union = Object

            type Object {
              id: ID!
              str: String!
            }
            """;
        var request =
            """
            query {
              unon {
                __typename
                ... on Object {
                  id
                  str
                }
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
                "unon": {
                  "__typename": "Object",
                  "id": "1",
                  "str": "string"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Union_Null()
    {
        // arrange
        var schema =
            """
            type Query {
              unon: Union @null
            }

            union Union = Object

            type Object {
              id: ID!
              str: String!
            }
            """;
        var request =
            """
            query {
              unon {
                __typename
                ... on Object {
                  id
                  str
                }
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
                "unon": null
              }
            }
            """);
    }

    [Fact]
    public async Task Union_Error()
    {
        // arrange
        var schema =
            """
            type Query {
              unon: Union @error
            }

            union Union = Object

            type Object {
              id: ID!
              str: String!
            }
            """;
        var request =
            """
            query {
              unon {
                __typename
                ... on Object {
                  id
                  str
                }
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
                    "unon"
                  ]
                }
              ],
              "data": {
                "unon": null
              }
            }
            """);
    }

    [Fact]
    public async Task Union_List()
    {
        // arrange
        var schema =
            """
            type Query {
              unions: [Union]
            }

            union Union = Object

            type Object {
              id: ID!
              str: String!
            }
            """;
        var request =
            """
            query {
              unions {
                __typename
                ... on Object {
                  id
                  str
                }
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
                "unions": [
                  {
                    "__typename": "Object",
                    "id": "1",
                    "str": "string"
                  },
                  {
                    "__typename": "Object",
                    "id": "2",
                    "str": "string"
                  },
                  {
                    "__typename": "Object",
                    "id": "3",
                    "str": "string"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Union_List_Null()
    {
        // arrange
        var schema =
            """
            type Query {
              unions: [Union] @null
            }

            union Union = Object

            type Object {
              id: ID!
              str: String!
            }
            """;
        var request =
            """
            query {
              unions {
                __typename
                ... on Object {
                  id
                  str
                }
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
                "unions": null
              }
            }
            """);
    }

    [Fact]
    public async Task Union_List_Error()
    {
        // arrange
        var schema =
            """
            type Query {
              unions: [Union] @error
            }

            union Union = Object

            type Object {
              id: ID!
              str: String!
            }
            """;
        var request =
            """
            query {
              unions {
                __typename
                ... on Object {
                  id
                  str
                }
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
                    "unions"
                  ]
                }
              ],
              "data": {
                "unions": null
              }
            }
            """);
    }

    [Fact]
    public async Task Union_List_NullAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              unions: [Union] @null(atIndex: 1)
            }

            union Union = Object

            type Object {
              id: ID!
              str: String!
            }
            """;
        var request =
            """
            query {
              unions {
                __typename
                ... on Object {
                  id
                  str
                }
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
                "unions": [
                  {
                    "__typename": "Object",
                    "id": "1",
                    "str": "string"
                  },
                  null,
                  {
                    "__typename": "Object",
                    "id": "3",
                    "str": "string"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Union_List_ErrorAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              unions: [Union] @error(atIndex: 1)
            }

            union Union = Object

            type Object {
              id: ID!
              str: String!
            }
            """;
        var request =
            """
            query {
              unions {
                __typename
                ... on Object {
                  id
                  str
                }
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
                    "unions",
                    1
                  ]
                }
              ],
              "data": {
                "unions": [
                  {
                    "__typename": "Object",
                    "id": "1",
                    "str": "string"
                  },
                  null,
                  {
                    "__typename": "Object",
                    "id": "3",
                    "str": "string"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Union_List_Property_NullAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              unions: [Union]
            }

            union Union = Object

            type Object {
              id: ID!
              str: String @null(atIndex: 1)
            }
            """;
        var request =
            """
            query {
              unions {
                __typename
                ... on Object {
                  id
                  str
                }
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
                "unions": [
                  {
                    "__typename": "Object",
                    "id": "1",
                    "str": "string"
                  },
                  {
                    "__typename": "Object",
                    "id": "2",
                    "str": null
                  },
                  {
                    "__typename": "Object",
                    "id": "3",
                    "str": "string"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Union_List_Property_ErrorAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              unions: [Union]
            }

            union Union = Object

            type Object {
              id: ID!
              str: String @error(atIndex: 1)
            }
            """;
        var request =
            """
            query {
              unions {
                __typename
                ... on Object {
                  id
                  str
                }
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
                      "line": 6,
                      "column": 7
                    }
                  ],
                  "path": [
                    "unions",
                    1,
                    "str"
                  ]
                }
              ],
              "data": {
                "unions": [
                  {
                    "__typename": "Object",
                    "id": "1",
                    "str": "string"
                  },
                  {
                    "__typename": "Object",
                    "id": "2",
                    "str": null
                  },
                  {
                    "__typename": "Object",
                    "id": "3",
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
    public async Task Scalar_Null()
    {
        // arrange
        var schema =
            """
            type Query {
              str: String @null
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
                "str": null
              }
            }
            """);
    }

    [Fact]
    public async Task Scalar_Error()
    {
        // arrange
        var schema =
            """
            type Query {
              str: String @error
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
                    "str"
                  ]
                }
              ],
              "data": {
                "str": null
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
    public async Task Scalar_List_Null()
    {
        // arrange
        var schema =
            """
            type Query {
              scalars: [String!] @null
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
                "scalars": null
              }
            }
            """);
    }

    [Fact]
    public async Task Scalar_List_Error()
    {
        // arrange
        var schema =
            """
            type Query {
              scalars: [String!] @error
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
                    "scalars"
                  ]
                }
              ],
              "data": {
                "scalars": null
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
    public async Task Enum_Null()
    {
        // arrange
        var schema =
            """
            type Query {
              enm: MyEnum @null
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
                "enm": null
              }
            }
            """);
    }

    [Fact]
    public async Task Enum_Error()
    {
        // arrange
        var schema =
            """
            type Query {
              enm: MyEnum @error
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
                    "enm"
                  ]
                }
              ],
              "data": {
                "enm": null
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

    #region node

    [Fact]
    public async Task NodeField()
    {
        // arrange
        var schema =
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """;
        var request =
            """
            query {
              node(id: "5") {
                id
                ... on Product {
                  name
                }
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
                "node": {
                  "id": "5",
                  "name": "string"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task NodeField_Null()
    {
        // arrange
        var schema =
            """
            type Query {
              node(id: ID!): Node @null
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """;
        var request =
            """
            query {
              node(id: "5") {
                id
                ... on Product {
                  name
                }
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
                "node": null
              }
            }
            """);
    }

    [Fact]
    public async Task NodeField_Error()
    {
        // arrange
        var schema =
            """
            type Query {
              node(id: ID!): Node @error
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """;
        var request =
            """
            query {
              node(id: "5") {
                id
                ... on Product {
                  name
                }
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
                    "node"
                  ]
                }
              ],
              "data": {
                "node": null
              }
            }
            """);
    }

    #endregion

    #region nodes

    [Fact]
    public async Task NodesField()
    {
        // arrange
        var schema =
            """
            type Query {
              nodes(ids: [ID!]!): [Node]!
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """;
        var request =
            """
            query {
              nodes(ids: ["5", "6"]) {
                id
                ... on Product {
                  name
                }
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
                "nodes": [
                  {
                    "id": "5",
                    "name": "string"
                  },
                  {
                    "id": "6",
                    "name": "string"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task NodesField_Null()
    {
        // arrange
        var schema =
            """
            type Query {
              nodes(ids: [ID!]!): [Node]! @null
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """;
        var request =
            """
            query {
              nodes(ids: ["5", "6"]) {
                id
                ... on Product {
                  name
                }
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
                  "message": "Cannot return null for non-nullable field.",
                  "locations": [
                    {
                      "line": 2,
                      "column": 3
                    }
                  ],
                  "path": [
                    "nodes"
                  ],
                  "extensions": {
                    "code": "HC0018"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task NodesField_Error()
    {
        // arrange
        var schema =
            """
            type Query {
              nodes(ids: [ID!]!): [Node]! @error
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """;
        var request =
            """
            query {
              nodes(ids: ["5", "6"]) {
                id
                ... on Product {
                  name
                }
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
                    "nodes"
                  ]
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task NodesField_NullAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              nodes(ids: [ID!]!): [Node]! @null(atIndex: 1)
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """;
        var request =
            """
            query {
              nodes(ids: ["5", "6"]) {
                id
                ... on Product {
                  name
                }
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
                "nodes": [
                  {
                    "id": "5",
                    "name": "string"
                  },
                  null
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task NodesField_ErrorAtIndex()
    {
        // arrange
        var schema =
            """
            type Query {
              nodes(ids: [ID!]!): [Node]! @error(atIndex: 1)
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """;
        var request =
            """
            query {
              nodes(ids: ["5", "6"]) {
                id
                ... on Product {
                  name
                }
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
                    "nodes",
                    1
                  ]
                }
              ],
              "data": {
                "nodes": [
                  {
                    "id": "5",
                    "name": "string"
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
