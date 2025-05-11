namespace HotChocolate.Fusion.Planning;

public class IntrospectionTests : FusionTestBase
{
    [Fact(Skip = "Not yet supported")]
    public void Typename_On_Query()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              field: String
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();

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

    [Fact(Skip = "Not yet supported")]
    public void Typename_On_Query_With_Alias()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              field: String
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();

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
    public void Full_Introspection()
    {
        // arrange
        var subgraphA = new TestSubgraph(
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

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();
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
}
