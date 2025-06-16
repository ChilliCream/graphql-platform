namespace HotChocolate.Fusion.Planning;

public class IntrospectionTests : FusionTestBase
{
    [Fact]
    public void Typename_On_Query()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              field: String
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              __typename
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            NOT SUPPORTED
            """);
    }

    [Fact]
    public void Typename_On_Query_With_Alias()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              field: String
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              alias: __typename
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            NOT SUPPORTED
            """);
    }

    [Fact]
    public void Typename_On_Object()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              object: Object
            }

            type Object {
              field: String
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              object {
                __typename
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: DEFAULT
              operation: >-
                query testQuery_1 {
                  object {
                    __typename
                  }
                }
            """);
    }

    [Fact]
    public void Typename_On_Interface()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              interface: Interface
            }

            interface Interface {
              field: String
            }

            type Object implements Interface {
              field: String
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              interface {
                __typename
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: DEFAULT
              operation: >-
                query testQuery_1 {
                  interface {
                    __typename
                  }
                }
            """);
    }

    [Fact]
    public void Typename_On_Union()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              union: Union
            }

            type Object {
              field: String
            }

            union Union = Object
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              union {
                __typename
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: DEFAULT
              operation: >-
                query testQuery_1 {
                  union {
                    __typename
                  }
                }
            """);
    }

    [Fact]
    public void Type_Lookup()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              object1: Object1
              object2: Object2
            }

            type Object1 {
              field: Int
            }

            type Object2 {
              field: String
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              __type(name: "Object1") {
                name
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            NOT SUPPORTED
            """);
    }

    [Fact]
    public void Type_Lookup_With_Alias()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              object1: Object1
              object2: Object2
            }

            type Object1 {
              field: Int
            }

            type Object2 {
              field: String
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              typeA: __type(name: "Object1") {
                name
              }
              typeB: __type(name: "Object2") {
                name
                fields {
                  name
                  type {
                    name
                    kind
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            NOT SUPPORTED
            """);
    }

    [Fact]
    public void Full_Introspection()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              field(arg1: String, arg2: SomeInput): String
              union: Union
              unionList: [Union!]
            }

            type Mutation {
              someMutation: String
            }

            type Subscription {
              someSubscription: String
            }

            input SomeInput {
              field: String
            }

            union Union = Object1 | Object2

            interface Interface1 {
              field: String
            }

            interface Interface2 implements Interface1 {
              field: String
            }

            type Object1 {
              nullableField: String
              field: Int
              listField: [Float!]!
              nullableListField: [String]
              customScalar: CustomScalar
            }

            type Object2 implements Interface2 & Interface1 {
              field: String
            }

            scalar CustomScalar
            """);

        var introspectionQuery = FileResource.Open("IntrospectionQuery.graphql");

        // act
        var plan = PlanOperation(schema, introspectionQuery);

        // assert
        MatchInline(
            plan,
            """
            NOT SUPPORTED
            """);
    }

    [Fact]
    public void Introspection_Fields_And_Regular_Fields()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              field1: String
              field2: String
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery($skip: Boolean!) {
              field1
              __schema {
                queryType {
                  name
                }
              }
              field2
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            NOT SUPPORTED
            """);
    }

    [Fact]
    public void Conditional_InlineFragment_Containing_Introspection_Fields()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              field1: String
              field2: String
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery($skip: Boolean!) {
              ... @skip(if: $skip) {
                __schema {
                  queryType {
                    name
                  }
                }
                field1
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            NOT SUPPORTED
            """);
    }
}
