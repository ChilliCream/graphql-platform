using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
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
        snapshot.Add(result, "Result");

        if (result.ContextData is not null &&
            result.ContextData.TryGetValue("queryPlan", out var value) &&
            value is QueryPlan queryPlan)
        {
            snapshot.Add(queryPlan, "QueryPlan");
            snapshot.Add(queryPlan.Hash, "QueryPlan Hash");
        }

        snapshot.Add(fusionGraph, "Fusion Graph");
    }

    public static void CollectErrorSnapshotData(
        Snapshot snapshot,
        DocumentNode request,
        IExecutionResult result)
    {
        snapshot.Add(request, "User Request");
        snapshot.Add(result, "Result");

        if (result.ContextData is not null &&
            result.ContextData.TryGetValue("queryPlan", out var value) &&
            value is QueryPlan queryPlan)
        {
            snapshot.Add(queryPlan, "QueryPlan");
        }
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

        snapshot.Add(fusionGraph, "Fusion Graph");
    }
}
