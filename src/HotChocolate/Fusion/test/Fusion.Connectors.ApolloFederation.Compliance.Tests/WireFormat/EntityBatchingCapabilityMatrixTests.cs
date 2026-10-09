using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.WireFormat.Left;
using HotChocolate.Fusion.WireFormat.Right;

namespace HotChocolate.Fusion.WireFormat;

/// <summary>
/// Pins the gateway to subgraph wire format that every combination of the batching capabilities
/// produces on the Apollo Federation entity path. The query selects <c>child</c> twice with
/// different sub-selections, so the planner forms two distinct <c>_entities</c> operations against
/// the <c>right</c> subgraph in one wave. The entity keys of an <c>_entities</c> operation travel
/// in its <c>representations</c> list, so the operation always carries one variable set and
/// variable batching never applies. The snapshot captures the HTTP requests that reached the
/// subgraphs plus the merged gateway result, recording both the number and the body shape of the
/// requests the source schema client produced. A second query covers the case where one of the
/// merged <c>_entities</c> operations resolves several representations at once.
/// </summary>
public sealed class EntityBatchingCapabilityMatrixTests
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

    private const string MultiEntityQuery =
        """
        {
          parents {
            a: child { a: value }
          }
          parent {
            b: child { b: value(suffix: "!") }
          }
        }
        """;

    [Theory]
    [Trait("Category", "WireFormat")]
    [InlineData(SourceSchemaClientCapabilities.None)]
    [InlineData(SourceSchemaClientCapabilities.VariableBatching)]
    [InlineData(SourceSchemaClientCapabilities.RequestBatching)]
    [InlineData(
        SourceSchemaClientCapabilities.VariableBatching
        | SourceSchemaClientCapabilities.RequestBatching)]
    [InlineData(SourceSchemaClientCapabilities.AliasBatching)]
    [InlineData(
        SourceSchemaClientCapabilities.AliasBatching
        | SourceSchemaClientCapabilities.VariableBatching)]
    [InlineData(
        SourceSchemaClientCapabilities.AliasBatching
        | SourceSchemaClientCapabilities.RequestBatching)]
    [InlineData(
        SourceSchemaClientCapabilities.AliasBatching
        | SourceSchemaClientCapabilities.VariableBatching
        | SourceSchemaClientCapabilities.RequestBatching)]
    public async Task EntityLookups_Should_UseTheProtocolTheCapabilitiesSelect(
        SourceSchemaClientCapabilities capabilities)
    {
        // arrange
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder.ComposeAsync(
            capture,
            CreateSettings(capabilities),
            (LeftSubgraph.Name, LeftSubgraph.BuildAsync),
            (RightSubgraph.Name, RightSubgraph.BuildAsync));

        // act
        var result = await gateway.Executor.ExecuteAsync(Query, TestContext.Current.CancellationToken);

        // assert
        // every combination has to resolve the same data, so all rows share one inline snapshot of
        // the gateway result; only the wire format below is pinned per combination.
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "parent": {
                  "a": {
                    "a": "child-1"
                  },
                  "b": {
                    "b": "child-1!"
                  }
                }
              }
            }
            """);

        await WireFormatSnapshot.MatchExchangeAsync(capture, result, CreateLabel(capabilities));
    }

    [Fact]
    [Trait("Category", "WireFormat")]
    public async Task EntityLookups_Should_KeepEntitiesAligned_When_AliasBatchingMergesAMultiEntityLookup()
    {
        // arrange
        // 'parents' yields two children through one lookup while 'parent' yields a third through a
        // second lookup, so the merged aliased document carries one prefixed representations list
        // with two elements next to a list with one element.
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder.ComposeAsync(
            capture,
            CreateSettings(SourceSchemaClientCapabilities.AliasBatching),
            (LeftSubgraph.Name, LeftSubgraph.BuildAsync),
            (RightSubgraph.Name, RightSubgraph.BuildAsync));

        // act
        var result = await gateway.Executor.ExecuteAsync(
            MultiEntityQuery,
            TestContext.Current.CancellationToken);

        // assert
        await WireFormatSnapshot.MatchExchangeAsync(capture, result);
    }

    /// <summary>
    /// Declares the batching capabilities of the <c>right</c> subgraph the way an operator would in
    /// the gateway settings.
    /// </summary>
    private static Dictionary<string, string> CreateSettings(
        SourceSchemaClientCapabilities capabilities)
        => new()
        {
            [RightSubgraph.Name] =
                $$"""
                {
                  "transports": {
                    "http": {
                      "capabilities": {
                        "batching": {
                          "variableBatching": {{Declares(
                              capabilities,
                              SourceSchemaClientCapabilities.VariableBatching)}},
                          "requestBatching": {{Declares(
                              capabilities,
                              SourceSchemaClientCapabilities.RequestBatching)}},
                          "aliasBatching": {{Declares(
                              capabilities,
                              SourceSchemaClientCapabilities.AliasBatching)}}
                        }
                      }
                    }
                  }
                }
                """
        };

    private static string Declares(
        SourceSchemaClientCapabilities capabilities,
        SourceSchemaClientCapabilities capability)
        => capabilities.HasFlag(capability) ? "true" : "false";

    /// <summary>
    /// Composes the filename-safe label of a capability combination.
    /// </summary>
    private static string CreateLabel(SourceSchemaClientCapabilities capabilities)
    {
        if (capabilities is SourceSchemaClientCapabilities.None)
        {
            return "None";
        }

        var label = string.Empty;

        if (capabilities.HasFlag(SourceSchemaClientCapabilities.AliasBatching))
        {
            label += "A";
        }

        if (capabilities.HasFlag(SourceSchemaClientCapabilities.VariableBatching))
        {
            label += "V";
        }

        if (capabilities.HasFlag(SourceSchemaClientCapabilities.RequestBatching))
        {
            label += "R";
        }

        return label;
    }
}
