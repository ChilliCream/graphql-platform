using HotChocolate.Fusion.Shared;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class RemoveTests(ITestOutputHelper output)
{
    [Fact]
    public async Task One_Subgraph_Removes_Field_That_Is_Present_In_Another_Subgraph()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: String!
            }
            """,
            """
            schema @remove(coordinate: "Query.field") {
            }
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: String!
            }
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        fusionGraph.MatchSnapshot();
    }

    [Fact]
    public async Task Subgraph_Removes_A_Field_Exclusively_Owned_By_It()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              someField: SomeObject!
            }

            type SomeObject {
              property: String!
            }
            """,
            """
            schema @remove(coordinate: "Query.someField") {
            }
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              otherField: AnotherObject!
            }

            type AnotherObject {
              property: String!
            }
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        fusionGraph.MatchSnapshot();
    }
}
