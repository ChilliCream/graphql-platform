using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Serialization;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Planning;

internal sealed class QueryPlan
{
    private readonly IOperation _operation;
    private readonly Dictionary<ISelectionSet, string[]> _exportKeysLookup = new();
    private readonly Dictionary<(ISelectionSet, string), string[]> _exportPathsLookup = new();
    private readonly IReadOnlySet<ISelectionSet> _selectionSets;

    public QueryPlan(
        IOperation operation,
        QueryPlanNode rootNode,
        IReadOnlySet<ISelectionSet> selectionSets,
        IReadOnlyCollection<ExportDefinition> exports)
    {
        _operation = operation;
        RootNode = rootNode;
        _selectionSets = selectionSets;

        if (exports.Count > 0)
        {
            var context = new ExportPathVisitorContext();

            foreach (var exportGroup in exports.GroupBy(t => t.SelectionSet))
            {
                _exportKeysLookup.Add(exportGroup.Key, exportGroup.Select(t => t.StateKey).ToArray());

                foreach (var export in exportGroup)
                {
                    context.Path.Clear();
                    ExportPathVisitor.Instance.Visit(export.VariableDefinition.Select, context);
                    _exportPathsLookup.Add((exportGroup.Key, export.StateKey), context.Path.ToArray());
                }
            }
        }
    }

    public QueryPlanNode RootNode { get; }

    /// <summary>
    /// Specifies if the query plan has nodes to fetch data for the specified
    /// selection set.
    /// </summary>
    /// <param name="selectionSet">
    /// The selection set in question.
    /// </param>
    /// <returns>
    /// <c>true</c> if there query plan has nodes to fetch data for the
    /// specified selection set; otherwise, <c>false</c>.
    /// </returns>
    public bool HasNodesFor(ISelectionSet selectionSet)
        => _selectionSets.Contains(selectionSet);

    public IReadOnlyList<string> GetExportKeys(ISelectionSet selectionSet)
        => _exportKeysLookup.TryGetValue(selectionSet, out var keys) ? keys : Array.Empty<string>();

    public IReadOnlyList<string> GetExportPath(ISelectionSet selectionSet, string key)
        => _exportPathsLookup.TryGetValue((selectionSet, key), out var path)
            ? path
            : Array.Empty<string>();

    public async Task<IQueryResult> ExecuteAsync(
        FusionExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (RootNode is Subscribe)
        {
            throw ThrowHelper.SubscriptionsMustSubscribe();
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

        try
        {
            await RootNode.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (NonNullPropagateException)
        {
            context.Result.SetData(null);

            // TODO : REMOVE after non-null prop is good.
            if (context.Result.Errors.Count == 0)
            {
                context.Result.AddError(
                    ErrorBuilder.New()
                        .SetMessage("Error")
                        .Build());
            }
        }

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
        if (RootNode is not Subscribe subscriptionNode)
        {
            throw ThrowHelper.QueryAndMutationMustExecute();
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

    private sealed class ExportPathVisitor : SyntaxWalker<ExportPathVisitorContext>
    {
        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            ExportPathVisitorContext context)
        {
            context.Path.Enqueue(node.Name.Value);
            return base.Enter(node, context);
        }

        public static ExportPathVisitor Instance { get; } = new();
    }

    private sealed class ExportPathVisitorContext : ISyntaxVisitorContext
    {
        public Queue<string> Path { get; } = new();
    }
}
