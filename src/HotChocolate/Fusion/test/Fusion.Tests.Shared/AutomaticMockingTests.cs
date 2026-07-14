using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class AutomaticMockingTests
{
    #region Objects

    [Fact]
    public async Task Object()
    {
        // arrange
        const string schema =
            """
            type Query {
              obj: Object
            }

            type Object {
              id: ID!
              str: String!
            }
            """;
        const string request =
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
                  "id": "T2JqZWN0OjE=",
                  "str": "Object: T2JqZWN0OjE="
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Object_Null()
    {
        // arrange
        const string schema =
            """
            type Query {
              obj: Object @null
            }

            type Object {
              id: ID!
              str: String!
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              obj: Object @error
            }

            type Object {
              id: ID!
              str: String!
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              objs: [Object!]!
            }

            type Object {
              id: ID!
              str: String!
            }
            """;
        const string request =
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
                    "id": "T2JqZWN0OjE=",
                    "str": "Object: T2JqZWN0OjE="
                  },
                  {
                    "id": "T2JqZWN0OjI=",
                    "str": "Object: T2JqZWN0OjI="
                  },
                  {
                    "id": "T2JqZWN0OjM=",
                    "str": "Object: T2JqZWN0OjM="
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
        const string schema =
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
        const string request =
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
                    "id": "T2JqZWN0OjE=",
                    "str": "Object: T2JqZWN0OjE="
                  },
                  {
                    "id": "T2JqZWN0OjI=",
                    "str": "Object: T2JqZWN0OjI="
                  },
                  {
                    "id": "T2JqZWN0OjM=",
                    "str": "Object: T2JqZWN0OjM="
                  }
                ],
                "objsB": [
                  {
                    "id": "T2JqZWN0OjQ=",
                    "str": "Object: T2JqZWN0OjQ="
                  },
                  {
                    "id": "T2JqZWN0OjU=",
                    "str": "Object: T2JqZWN0OjU="
                  },
                  {
                    "id": "T2JqZWN0OjY=",
                    "str": "Object: T2JqZWN0OjY="
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
        const string schema =
            """
            type Query {
              objs: [Object] @null(atIndex: 1)
            }

            type Object {
              id: ID!
            }
            """;
        const string request =
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
                    "id": "T2JqZWN0OjE="
                  },
                  null,
                  {
                    "id": "T2JqZWN0OjI="
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
        const string schema =
            """
            type Query {
              objs: [Object] @error(atIndex: 1)
            }

            type Object {
              id: ID!
            }
            """;
        const string request =
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
                  "path": [
                    "objs",
                    1
                  ]
                }
              ],
              "data": {
                "objs": [
                  {
                    "id": "T2JqZWN0OjE="
                  },
                  null,
                  {
                    "id": "T2JqZWN0OjI="
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
        const string schema =
            """
            type Query {
              objs: [Object!]!
            }

            type Object {
              str: String @null(atIndex: 1)
            }
            """;
        const string request =
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
                    "str": "Object: T2JqZWN0OjE="
                  },
                  {
                    "str": null
                  },
                  {
                    "str": "Object: T2JqZWN0OjM="
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
        const string schema =
            """
            type Query {
              objs: [Object!]!
            }

            type Object {
              str: String @error(atIndex: 1)
            }
            """;
        const string request =
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
                    "str": "Object: T2JqZWN0OjE="
                  },
                  {
                    "str": null
                  },
                  {
                    "str": "Object: T2JqZWN0OjM="
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
        const string schema =
            """
            type Query {
              intrface: Interface @returns(types: "Object")
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

            type Object2 implements Interface {
              id: ID!
              str: String!
              num: Int!
            }
            """;
        const string request =
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
                  "id": "T2JqZWN0OjE=",
                  "str": "Object: T2JqZWN0OjE=",
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
        const string schema =
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
        const string request =
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
        const string schema =
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
        const string request =
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
        const string schema =
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

            type Object2 implements Interface {
              id: ID!
              str: String!
            }

            type Object3 implements Interface {
              id: ID!
              str: String!
            }
            """;
        const string request =
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
                    "id": "T2JqZWN0OjE=",
                    "str": "Object: T2JqZWN0OjE=",
                    "num": 123
                  },
                  {
                    "__typename": "Object2",
                    "id": "T2JqZWN0Mjoy",
                    "str": "Object2: T2JqZWN0Mjoy"
                  },
                  {
                    "__typename": "Object3",
                    "id": "T2JqZWN0Mzoz",
                    "str": "Object3: T2JqZWN0Mzoz"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Interface_List_With_Returns_Directive()
    {
        // arrange
        const string schema =
            """
            type Query {
              interfaces: [Interface] @returns(types: ["Object2", "Object2", "Object"])
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

            type Object2 implements Interface {
              id: ID!
              str: String!
            }

            type Object3 implements Interface {
              id: ID!
              str: String!
            }
            """;
        const string request =
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
                    "__typename": "Object2",
                    "id": "T2JqZWN0Mjox",
                    "str": "Object2: T2JqZWN0Mjox"
                  },
                  {
                    "__typename": "Object2",
                    "id": "T2JqZWN0Mjoy",
                    "str": "Object2: T2JqZWN0Mjoy"
                  },
                  {
                    "__typename": "Object",
                    "id": "T2JqZWN0OjM=",
                    "str": "Object: T2JqZWN0OjM=",
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
        const string schema =
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
        const string request =
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
        const string schema =
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
        const string request =
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
        const string schema =
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
        const string request =
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
                    "id": "T2JqZWN0OjE=",
                    "str": "Object: T2JqZWN0OjE=",
                    "num": 123
                  },
                  null,
                  {
                    "__typename": "Object",
                    "id": "T2JqZWN0OjI=",
                    "str": "Object: T2JqZWN0OjI=",
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
        const string schema =
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
        const string request =
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
                    "id": "T2JqZWN0OjE=",
                    "str": "Object: T2JqZWN0OjE=",
                    "num": 123
                  },
                  null,
                  {
                    "__typename": "Object",
                    "id": "T2JqZWN0OjI=",
                    "str": "Object: T2JqZWN0OjI=",
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
        const string schema =
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
        const string request =
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
                    "id": "T2JqZWN0OjE=",
                    "str": "Object: T2JqZWN0OjE=",
                    "num": 123
                  },
                  {
                    "__typename": "Object",
                    "id": "T2JqZWN0OjI=",
                    "str": "Object: T2JqZWN0OjI=",
                    "num": null
                  },
                  {
                    "__typename": "Object",
                    "id": "T2JqZWN0OjM=",
                    "str": "Object: T2JqZWN0OjM=",
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
        const string schema =
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
        const string request =
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
                    "id": "T2JqZWN0OjE=",
                    "str": "Object: T2JqZWN0OjE=",
                    "num": 123
                  },
                  {
                    "__typename": "Object",
                    "id": "T2JqZWN0OjI=",
                    "str": "Object: T2JqZWN0OjI=",
                    "num": null
                  },
                  {
                    "__typename": "Object",
                    "id": "T2JqZWN0OjM=",
                    "str": "Object: T2JqZWN0OjM=",
                    "num": 123
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Interface_Multiple_Types_Implementing_Interface()
    {
        // arrange
        const string schema =
            """
            type Query {
              intrface: Interface @returns(types: "Object2")
            }

            interface Interface {
              id: ID!
              str: String!
            }

            type Object1 implements Interface {
              id: ID!
              str: String!
            }

            type Object2 implements Interface {
              id: ID!
              str: String!
              num: Int!
            }
            """;
        const string request =
            """
            query {
              intrface {
                __typename
                id
                str
                ... on Object2 {
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
                  "__typename": "Object2",
                  "id": "T2JqZWN0Mjox",
                  "str": "Object2: T2JqZWN0Mjox",
                  "num": 123
                }
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
        const string schema =
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
        const string request =
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
                  "id": "T2JqZWN0OjE=",
                  "str": "Object: T2JqZWN0OjE="
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Union_Null()
    {
        // arrange
        const string schema =
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
        const string request =
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
        const string schema =
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
        const string request =
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
        const string schema =
            """
            type Query {
              unions: [Union]
            }

            union Union = Object | Object2 | Object3

            type Object {
              id: ID!
              str: String!
            }

            type Object2 {
              id: ID!
              str: String!
            }

            type Object3 {
              id: ID!
              str: String!
            }
            """;
        const string request =
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
                    "id": "T2JqZWN0OjE=",
                    "str": "Object: T2JqZWN0OjE="
                  },
                  {
                    "__typename": "Object2"
                  },
                  {
                    "__typename": "Object3"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Union_List_With_Returns_Directive()
    {
        // arrange
        const string schema =
            """
            type Query {
              unions: [Union] @returns(types: ["Object2", "Object", "Object2"])
            }

            union Union = Object | Object2 | Object3

            type Object {
              id: ID!
              str: String!
            }

            type Object2 {
              id: ID!
              str: String!
            }

            type Object3 {
              id: ID!
              str: String!
            }
            """;
        const string request =
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
                    "__typename": "Object2"
                  },
                  {
                    "__typename": "Object",
                    "id": "T2JqZWN0OjI=",
                    "str": "Object: T2JqZWN0OjI="
                  },
                  {
                    "__typename": "Object2"
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
        const string schema =
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
        const string request =
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
        const string schema =
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
        const string request =
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
        const string schema =
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
        const string request =
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
                    "id": "T2JqZWN0OjE=",
                    "str": "Object: T2JqZWN0OjE="
                  },
                  null,
                  {
                    "__typename": "Object",
                    "id": "T2JqZWN0OjI=",
                    "str": "Object: T2JqZWN0OjI="
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
        const string schema =
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
        const string request =
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
                    "id": "T2JqZWN0OjE=",
                    "str": "Object: T2JqZWN0OjE="
                  },
                  null,
                  {
                    "__typename": "Object",
                    "id": "T2JqZWN0OjI=",
                    "str": "Object: T2JqZWN0OjI="
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
        const string schema =
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
        const string request =
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
                    "id": "T2JqZWN0OjE=",
                    "str": "Object: T2JqZWN0OjE="
                  },
                  {
                    "__typename": "Object",
                    "id": "T2JqZWN0OjI=",
                    "str": null
                  },
                  {
                    "__typename": "Object",
                    "id": "T2JqZWN0OjM=",
                    "str": "Object: T2JqZWN0OjM="
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
        const string schema =
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
        const string request =
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
                    "id": "T2JqZWN0OjE=",
                    "str": "Object: T2JqZWN0OjE="
                  },
                  {
                    "__typename": "Object",
                    "id": "T2JqZWN0OjI=",
                    "str": null
                  },
                  {
                    "__typename": "Object",
                    "id": "T2JqZWN0OjM=",
                    "str": "Object: T2JqZWN0OjM="
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
        const string schema =
            """
            type Query {
              str: String
            }
            """;
        const string request =
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
                "str": "Query"
              }
            }
            """);
    }

    [Fact]
    public async Task Scalar_Null()
    {
        // arrange
        const string schema =
            """
            type Query {
              str: String @null
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              str: String @error
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              scalars: [String!]!
            }
            """;
        const string request =
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
                  "Query",
                  "Query",
                  "Query"
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Scalar_List_Null()
    {
        // arrange
        const string schema =
            """
            type Query {
              scalars: [String!] @null
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              scalars: [String!] @error
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              scalars: [String] @null(atIndex: 1)
            }
            """;
        const string request =
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
                  "Query",
                  null,
                  "Query"
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Scalar_List_ErrorAtIndex()
    {
        // arrange
        const string schema =
            """
            type Query {
              scalars: [String] @error(atIndex: 1)
            }
            """;
        const string request =
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
                  "path": [
                    "scalars",
                    1
                  ]
                }
              ],
              "data": {
                "scalars": [
                  "Query",
                  null,
                  "Query"
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
        const string schema =
            """
            type Query {
              enm: MyEnum
            }

            enum MyEnum {
              VALUE
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              enm: MyEnum @null
            }

            enum MyEnum {
              VALUE
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              enm: MyEnum @error
            }

            enum MyEnum {
              VALUE
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              enums: [MyEnum]
            }

            enum MyEnum {
              VALUE
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              enums: [MyEnum] @null(atIndex: 1)
            }

            enum MyEnum {
              VALUE
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              enums: [MyEnum] @error(atIndex: 1)
            }

            enum MyEnum {
              VALUE
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product {
              id: ID!
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              productById(id: ID!): Product @null
            }

            type Product {
              id: ID!
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              productById(id: ID!): Product @error
            }

            type Product {
              id: ID!
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              productsById(ids: [ID!]!): [Product!]!
            }

            type Product {
              id: ID!
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              productsById(ids: [ID!]!): [Product!] @null
            }

            type Product {
              id: ID!
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              productsById(ids: [ID!]!): [Product!] @error
            }

            type Product {
              id: ID!
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              productsById(ids: [ID!]!): [Product] @null(atIndex: 1)
            }

            type Product {
              id: ID!
            }
            """;
        const string request =
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
        const string schema =
            """
            type Query {
              productsById(ids: [ID!]!): [Product] @error(atIndex: 1)
            }

            type Product {
              id: ID!
            }
            """;
        const string request =
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
        const string schema =
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
        const string request =
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
                  "name": "Product: 5"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task NodeField_Type_Is_Taken_From_Id()
    {
        // arrange
        const string schema =
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }

            type Author implements Node {
              id: ID!
              fullName: String!
            }
            """;
        const string request =
            """
            query {
              node(id: "UHJvZHVjdDox") {
                __typename
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
                "node": {
                  "__typename": "Product",
                  "id": "UHJvZHVjdDox"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task NodeField_Null()
    {
        // arrange
        const string schema =
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
        const string request =
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
        const string schema =
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
        const string request =
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
        const string schema =
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
        const string request =
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
                    "name": "Product: 5"
                  },
                  {
                    "id": "6",
                    "name": "Product: 6"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task NodesField_Types_Are_Taken_From_Ids()
    {
        // arrange
        const string schema =
            """
            type Query {
              nodes(ids: [ID!]!): [Node]!
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }

            type Author implements Node {
              id: ID!
              fullName: String!
            }
            """;
        const string request =
            """
            query {
              nodes(ids: ["UHJvZHVjdDox", "UHJvZHVjdDoz", "QXV0aG9yOjM=", "RGlzY3Vzc2lvbjoz", "QXV0aG9yOjEw"]) {
                __typename
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
                "nodes": [
                  {
                    "__typename": "Product",
                    "id": "UHJvZHVjdDox"
                  },
                  {
                    "__typename": "Product",
                    "id": "UHJvZHVjdDoz"
                  },
                  {
                    "__typename": "Author",
                    "id": "QXV0aG9yOjM="
                  },
                  {
                    "__typename": "Discussion",
                    "id": "RGlzY3Vzc2lvbjoz"
                  },
                  {
                    "__typename": "Author",
                    "id": "QXV0aG9yOjEw"
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
        const string schema =
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
        const string request =
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
        const string schema =
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
        const string request =
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
        const string schema =
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
        const string request =
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
                    "name": "Product: 5"
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
        const string schema =
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
        const string request =
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
                    "name": "Product: 5"
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
