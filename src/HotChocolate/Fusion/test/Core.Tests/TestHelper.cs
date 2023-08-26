using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion;

internal static class TestHelper
{
    public static void CollectSnapshotData(
        Snapshot snapshot,
        DocumentNode request,
        IExecutionResult result,
        Skimmed.Schema fusionGraph)
    {
        snapshot.Add(request, "User Request");

        if (result.ContextData is not null &&
            result.ContextData.TryGetValue("queryPlan", out var value) &&
            value is QueryPlan queryPlan)
        {
            snapshot.Add(queryPlan, "QueryPlan");
            snapshot.Add(queryPlan.Hash, "QueryPlan Hash");
        }

        snapshot.Add(result, "Result");
        snapshot.Add(SchemaFormatter.FormatAsString(fusionGraph), "Fusion Graph");
    }

    public static async Task CollectStreamSnapshotData(
        Snapshot snapshot,
        DocumentNode request,
        IExecutionResult result,
        Skimmed.Schema fusionGraph,
        CancellationToken cancellationToken)
    {
        snapshot.Add(request, "User Request");

        var i = 0;

        await foreach (var item in result.ExpectResponseStream()
            .ReadResultsAsync().WithCancellation(cancellationToken))
        {
            if (item.ContextData is not null &&
                item.ContextData.TryGetValue("queryPlan", out var value) &&
                value is QueryPlan queryPlan)
            {
                snapshot.Add(queryPlan, "QueryPlan");
                snapshot.Add(queryPlan.Hash, "QueryPlan Hash");
            }

            snapshot.Add(item, $"Result {++i}");
        }

        snapshot.Add(SchemaFormatter.FormatAsString(fusionGraph), "Fusion Graph");
    }
}