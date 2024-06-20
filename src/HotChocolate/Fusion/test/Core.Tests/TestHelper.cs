using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Serialization;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Shared;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Snapshot = CookieCrumble.Snapshot;

namespace HotChocolate.Fusion;

internal static class TestHelper
{
    // TODO: Temporary
    public static async Task<IExecutionResult> ProduceProperError(string schemaText, string request)
    {
        var subgraph = await TestSubgraph.CreateAsync(schemaText);
        var executor = await subgraph.TestServer.Services.GetRequestExecutorAsync();

        return await executor.ExecuteAsync(request);
    }

    public static void MatchMarkdownSnapshot(
        string requestText,
        IExecutionResult result)
    {
        var snapshot = new Snapshot();
        var request = Utf8GraphQLParser.Parse(requestText);

        CollectSnapshotData(snapshot, request, result);
        snapshot.MatchMarkdownSnapshot();
    }

    public static void CollectSnapshotData(
        Snapshot snapshot,
        DocumentNode request,
        IExecutionResult result)
    {
        snapshot.Add(result, "Result");
        snapshot.Add(request, "Request");

        if (result.ContextData is not null &&
            result.ContextData.TryGetValue("queryPlan", out var value) &&
            value is QueryPlan queryPlan)
        {
            snapshot.Add(queryPlan.Hash, "QueryPlan Hash");
            snapshot.Add(queryPlan, "QueryPlan");
        }
    }

    public static async Task CollectStreamSnapshotData(
        Snapshot snapshot,
        DocumentNode request,
        IExecutionResult result,
        CancellationToken cancellationToken)
    {
        var i = 0;
        QueryPlan? plan = null;

        await foreach (var item in result.ExpectResponseStream()
                           .ReadResultsAsync().WithCancellation(cancellationToken))
        {
            if (item.ContextData is not null &&
                item.ContextData.TryGetValue("queryPlan", out var value) &&
                value is QueryPlan queryPlan)
            {
                plan = queryPlan;
            }

            snapshot.Add(new OperationResultSnapshot(item, $"Result {++i}"));
        }

        snapshot.Add(request, "Request");

        if (plan is not null)
        {
            snapshot.Add(plan.Hash, "QueryPlan Hash");
            snapshot.Add(plan, "QueryPlan");
        }
    }

    private sealed class OperationResultSnapshot : SnapshotValue
    {
        private static readonly JsonResultFormatter _formatter =
            new(new JsonResultFormatterOptions { Indented = true });

        private readonly byte[] _value;

        public OperationResultSnapshot(IOperationResult result, string? name = null)
        {
            Name = name;

            using var writer = new ArrayWriter();
            _formatter.Format(result, writer);
            _value = writer.GetWrittenSpan().ToArray();
        }


        public override string? Name { get; }

        public override ReadOnlySpan<byte> Value => _value;

        protected override string MarkdownType => "text";
    }
}
