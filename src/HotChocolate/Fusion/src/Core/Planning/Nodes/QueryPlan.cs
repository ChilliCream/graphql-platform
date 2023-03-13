using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Transactions;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Serialization;
using HotChocolate.Fusion.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Planning;

internal sealed class QueryPlan
{
    private readonly IOperation _operation;
    private readonly IReadOnlyDictionary<ISelectionSet, string[]> _exportKeysLookup;
    private readonly IReadOnlySet<ISelectionSet> _hasNodes;

    public QueryPlan(
        IOperation operation,
        QueryPlanNode rootNode,
        IReadOnlyDictionary<ISelectionSet, string[]> exportKeysLookup,
        IReadOnlySet<ISelectionSet> hasNodes)
    {
        _operation = operation;
        _exportKeysLookup = exportKeysLookup;
        _hasNodes = hasNodes;
        RootNode = rootNode;
    }

    public QueryPlanNode RootNode { get; }

    public bool HasNodes(ISelectionSet selectionSet)
        => _hasNodes.Contains(selectionSet);

    public IReadOnlyList<string> GetExportKeys(ISelectionSet selectionSet)
        => _exportKeysLookup.TryGetValue(selectionSet, out var keys) ? keys : Array.Empty<string>();

    public async Task<IQueryResult> ExecuteAsync(
        FusionExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (RootNode is SubscriptionNode)
        {
            // TODO : exception
            throw new InvalidOperationException(
                "A subscription execution plan can not be executed as a query.");
        }

        var operationContext = context.OperationContext;

        if (operationContext.ContextData.ContainsKey(WellKnownContextData.IncludeQueryPlan))
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            context.QueryPlan.Format(bufferWriter);
            operationContext.Result.SetExtension(
                "queryPlan",
                new RawJsonValue(bufferWriter.WrittenMemory));
        }

        // we store the context on the result for unit tests.
        operationContext.Result.SetContextData("queryPlan", context.QueryPlan);

        // Enqueue root node to initiate the execution process.
        var rootSelectionSet = context.Operation.RootSelectionSet;
        var rootResult = context.Result.RentObject(rootSelectionSet.Selections.Count);

        context.Result.SetData(rootResult);
        context.RegisterState(rootSelectionSet, rootResult);

        await RootNode.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

        context.Result.RegisterForCleanup(
            () =>
            {
                context.Dispose();
                return default;
            });
        return context.Result.BuildResult();
    }

    public Task<IResponseStream> SubscribeAsync(
        FusionExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (RootNode is not SubscriptionNode subscriptionNode)
        {
            // TODO : exception
            throw new InvalidOperationException(
                "A query execution plan can not be executed as a subscription.");
        }

        var result = new ResponseStream(
            () => subscriptionNode.SubscribeAsync(context, cancellationToken),
            kind: ExecutionResultKind.SubscriptionResult);

        result.RegisterForCleanup(
            () =>
            {
                context.Dispose();
                return default;
            });

        return Task.FromResult<IResponseStream>(result);
    }

    public void Format(IBufferWriter<byte> writer)
    {
        var jsonOptions = new JsonWriterOptions { Indented = true };
        using var jsonWriter = new Utf8JsonWriter(writer, jsonOptions);
        Format(jsonWriter);
        jsonWriter.Flush();
    }

    public void Format(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();

        writer.WriteString("document", _operation.Document.ToString(false));

        if (!string.IsNullOrEmpty(_operation.Name))
        {
            writer.WriteString("operation", _operation.Name);
        }

        writer.WritePropertyName("rootNode");
        RootNode.Format(writer);

        writer.WriteEndObject();
    }

    public override string ToString()
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        var jsonOptions = new JsonWriterOptions { Indented = true };
        using var jsonWriter = new Utf8JsonWriter(bufferWriter, jsonOptions);

        Format(jsonWriter);
        jsonWriter.Flush();

        return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
    }
}
