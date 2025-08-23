using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

// TODO: Add more tests
public class RootSelectionSetPartitionerTests : FusionTestBase
{
    [Fact]
    public void Test()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        var schema = ComposeSchema(source1);

        var doc = Utf8GraphQLParser.Parse(
            """
            query($id: ID!) {
                node(id: $id) {
                    ... on Discussion {
                      title
                    }
                }
            }
            """);

        var operation = doc.Definitions
            .OfType<OperationDefinitionNode>()
            .Single();
        var index = SelectionSetIndexer.Create(operation);

        var input = new RootSelectionSetPartitionerInput()
        {
            SelectionSet = new SelectionSet(
                index.GetId(operation.SelectionSet),
                operation.SelectionSet,
                schema.Types["Node"],
                Execution.Nodes.SelectionPath.Root),
            SelectionSetIndex = index
        };
        var partitioner = new RootSelectionSetPartitioner(schema);

        // act
        var result = partitioner.Partition(input);

        // assert
        Assert.NotNull(result);
    }
}
