namespace HotChocolate.Fusion.Planning;

public class UnionTests : FusionTestBase
{
    [Fact(Skip = "Not yet supported")]
    public void Just_Typename_Selected()
    {
        // arrange
        var subgraphA = new TestSubgraph(
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              imageUrl: String!
            }

            type Discussion {
              id: ID!
              title: String
            }
            """);

        var subgraphs = new TestSubgraphCollection(subgraphA);
        var schema = subgraphs.BuildFusionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query testQuery {
              post {
                __typename
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
