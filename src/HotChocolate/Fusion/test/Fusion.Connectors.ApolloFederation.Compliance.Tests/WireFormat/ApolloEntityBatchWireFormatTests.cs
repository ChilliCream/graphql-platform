using HotChocolate.Execution;
using HotChocolate.Fusion.WireFormat.Left;
using HotChocolate.Fusion.WireFormat.Right;

namespace HotChocolate.Fusion.WireFormat;

/// <summary>
/// Pins the gateway to subgraph wire format that the Apollo Federation entity-batch
/// execution path produces over the uniform default transport. The query selects
/// <c>child</c> twice with different sub-selections, so the planner forms two
/// <c>_entities</c> sub-requests against the <c>right</c> subgraph. No batching
/// capabilities are declared, so the gateway defaults to alias batching and the
/// subgraph accepts it, sending the two sub-requests as a single aliased operation.
/// The snapshot captures the HTTP requests that reached the
/// subgraphs plus the merged gateway result, recording both the number and the body
/// shape of the requests the source schema client produced.
/// </summary>
public sealed class ApolloEntityBatchWireFormatTests
{
    private const string Query =
        """
        {
          parent {
            a: child { a: value }
            b: child { b: value(suffix: "!") }
          }
        }
        """;

    [Fact]
    [Trait("Category", "WireFormat")]
    public async Task ApolloEntityBatch_Should_SendOneAliasedOperation_When_CapabilitiesUndeclared()
    {
        // arrange
        // no batching capabilities are declared, so the gateway defaults to alias batching
        // and the subgraph accepts it, merging both sub-requests into one operation.
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder.ComposeAsync(
            capture,
            (LeftSubgraph.Name, LeftSubgraph.BuildAsync),
            (RightSubgraph.Name, RightSubgraph.BuildAsync));

        // act
        var result = await gateway.Executor.ExecuteAsync(Query, TestContext.Current.CancellationToken);

        // assert
        await WireFormatSnapshot.MatchExchangeAsync(capture, result);
    }

    [Fact]
    [Trait("Category", "WireFormat")]
    public async Task ApolloEntityBatch_Should_SendSequentialSingleRequests_When_OperatorDeclaresRequestBatchingFalse()
    {
        // arrange
        // the operator declares that the 'right' subgraph supports neither variable, request,
        // nor alias batching, so the gateway must send the two _entities sub-requests as two
        // sequential single requests instead of a single batched operation.
        var capture = new SubgraphRequestCapture();
        var settings = new Dictionary<string, string>
        {
            [RightSubgraph.Name] =
                """
                {
                  "transports": {
                    "http": {
                      "capabilities": {
                        "batching": {
                          "variableBatching": false,
                          "requestBatching": false,
                          "aliasBatching": false
                        }
                      }
                    }
                  }
                }
                """
        };

        await using var gateway = await FusionGatewayBuilder.ComposeAsync(
            capture,
            settings,
            (LeftSubgraph.Name, LeftSubgraph.BuildAsync),
            (RightSubgraph.Name, RightSubgraph.BuildAsync));

        // act
        var result = await gateway.Executor.ExecuteAsync(Query, TestContext.Current.CancellationToken);

        // assert
        await WireFormatSnapshot.MatchExchangeAsync(capture, result);
    }
}
