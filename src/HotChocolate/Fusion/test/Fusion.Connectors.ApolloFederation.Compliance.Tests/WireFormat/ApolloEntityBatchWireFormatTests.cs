using System.Text.Encodings.Web;
using System.Text.Json;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Fusion.WireFormat.Left;
using HotChocolate.Fusion.WireFormat.Right;

namespace HotChocolate.Fusion.WireFormat;

/// <summary>
/// Pins the gateway to subgraph wire format that the Apollo Federation entity-batch
/// execution path produces over the uniform default transport. The query selects
/// <c>child</c> twice with different sub-selections, so the planner forms two
/// <c>_entities</c> sub-requests against the <c>right</c> subgraph. No batching
/// capabilities are declared, so the gateway's default transport allows request
/// batching and the subgraph accepts it, sending the two sub-requests as a single
/// JSON-array operation batch. The snapshot captures the HTTP requests that reached the
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
    public async Task ApolloEntityBatch_Should_SendOneOperationBatch_When_CapabilitiesUndeclared()
    {
        // arrange
        // no batching capabilities are declared, so the gateway uses the uniform default
        // transport (request batching allowed) and the subgraph accepts the batch.
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder.ComposeAsync(
            capture,
            (LeftSubgraph.Name, LeftSubgraph.BuildAsync),
            (RightSubgraph.Name, RightSubgraph.BuildAsync));

        // act
        var result = await gateway.Executor.ExecuteAsync(Query, TestContext.Current.CancellationToken);

        // assert
        await MatchExchangeAsync(capture, result);
    }

    private static async Task MatchExchangeAsync(SubgraphRequestCapture capture, IExecutionResult result)
    {
        var snapshot = Snapshot.Create();
        var requests = capture.Requests;

        for (var i = 0; i < requests.Count; i++)
        {
            var request = requests[i];
            snapshot.Add(
                FormatJson(request.Body),
                $"HTTP Request {i + 1} to '{request.SubgraphName}'",
                "json");
        }

        snapshot.Add(result, "Gateway Result");

        await snapshot.MatchMarkdownAsync();
    }

    // Reformats the captured request body with indentation for readability while
    // preserving the wire escaping (embedded quotes stay as \" rather than "),
    // so the snapshot shows the body shape the client actually sent.
    private static string FormatJson(string body)
    {
        using var document = JsonDocument.Parse(body);
        return JsonSerializer.Serialize(document, s_indented);
    }

    private static readonly JsonSerializerOptions s_indented =
        new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
}
