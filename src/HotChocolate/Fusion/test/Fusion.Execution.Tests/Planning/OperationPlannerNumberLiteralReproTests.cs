using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public class OperationPlannerNumberLiteralReproTests : FusionTestBase
{
    // Repro for customer report: a Relay connection query with `first: 100` produces a
    // subgraph operation whose serialized source text is corrupted, e.g. the `100` literal
    // becomes a non-digit character, causing the subgraph parse to fail with
    // "Invalid number, expected digit but got: `c`" (HC0011).
    [Fact]
    public void Plan_Users_Connection_SubgraphOperations_AreValidGraphQL()
    {
        // arrange
        var schema = ComposeShoppingSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query Users($cursor: String) {
              users(first: 100, after: $cursor) {
                pageInfo {
                  hasNextPage
                  endCursor
                  __typename
                }
                edges {
                  node {
                    ...UserFields
                    __typename
                  }
                  __typename
                }
                __typename
              }
            }
            fragment UserFields on User {
              id
              name
              username
              __typename
            }
            """);

        // assert
        var executionNodes = plan.AllNodes.OfType<OperationExecutionNode>().ToList();
        Assert.NotEmpty(executionNodes);

        foreach (var node in executionNodes)
        {
            var sourceText = node.Operation.SourceText;

            // Re-parse each subgraph operation. The bug corrupts the `first: 100` literal,
            // making this throw SyntaxException ("Invalid number, expected digit but got ...").
            var ex = Record.Exception(() => Utf8GraphQLParser.Parse(sourceText));
            Assert.True(
                ex is null,
                $"Subgraph operation did not parse:\n{sourceText}\n\nError: {ex?.Message}");
        }
    }
}
