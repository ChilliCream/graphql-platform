using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Serialization;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionContextDataKeys;
using static HotChocolate.Fusion.Utilities.Utf8QueryPlanPropertyNames;
using ThrowHelper = HotChocolate.Fusion.Utilities.ThrowHelper;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a query plan that describes how a GraphQL request shall be executed.
/// </summary>
internal sealed class QueryPlan
{
    private static readonly JsonWriterOptions _jsonOptions = new() { Indented = true, };
    private readonly IOperation _operation;
    private readonly Dictionary<ISelectionSet, string[]> _exportKeysLookup = new();
    private readonly Dictionary<(ISelectionSet, string), string[]> _exportPathsLookup = new();
    private readonly (string Key, string DisplayName)[] _exportKeyToVariableName;
    private readonly IReadOnlySet<ISelectionSet> _selectionSets;
    private string? _hash;

    /// <summary>
    /// Initializes a new instance of <see cref="QueryPlan"/>.
    /// </summary>
    /// <param name="operation">
    /// The operation for which the query plan was created.
    /// </param>
    /// <param name="rootNode">
    /// The root node of the query plan.
    /// </param>
    /// <param name="selectionSets">
    /// The selection sets that are part of the query plan.
    /// </param>
    /// <param name="exports">
    /// The exports that are part of the query plan.
    /// </param>
    public QueryPlan(
        IOperation operation,
        QueryPlanNode rootNode,
        IReadOnlySet<ISelectionSet> selectionSets,
        IReadOnlyCollection<ExportDefinition> exports)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(rootNode);
        ArgumentNullException.ThrowIfNull(selectionSets);
        ArgumentNullException.ThrowIfNull(exports);

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

            var index = 0;
            _exportKeyToVariableName = new (string, string)[exports.Count];

            foreach (var export in exports)
            {
                _exportKeyToVariableName[index++] = (export.StateKey, export.VariableDefinition.Name);
            }
        }
        else
        {
            _exportKeyToVariableName = [];
        }
    }

    public string Hash
    {
        get
        {
            if (_hash is not null)
            {
                return _hash;
            }

            using var bufferWriter = new ArrayWriter();
            Format(bufferWriter);
            _hash = ComputeHash(bufferWriter.GetWrittenSpan());
            return _hash;
        }
    }

    /// <summary>
    /// Gets the root node of the query plan.
    /// </summary>
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
        => _exportKeysLookup.TryGetValue(selectionSet, out var keys) ? keys : [];

    public IReadOnlyList<string> GetExportPath(ISelectionSet selectionSet, string key)
        => _exportPathsLookup.TryGetValue((selectionSet, key), out var path) ? path : [];

    /// <summary>
    /// Executes the query plan.
    /// </summary>
    /// <param name="context">
    /// The execution context.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the query result.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The query plan represents a subscription request
    /// and cannot be executed but must be subscribed to.
    /// </exception>
    public async Task<IOperationResult> ExecuteAsync(
        FusionExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (RootNode is Subscribe)
        {
            throw ThrowHelper.SubscriptionsMustSubscribe();
        }

        var operationContext = context.OperationContext;

        if (context.AllowQueryPlan && operationContext.ContextData.ContainsKey(WellKnownContextData.IncludeQueryPlan))
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            context.QueryPlan.Format(bufferWriter);

            var memory = bufferWriter.WrittenMemory;
            _hash ??= ComputeHash(memory.Span);
            operationContext.Result.SetExtension(QueryPlanProp, new RawJsonValue(memory));
            operationContext.Result.SetExtension(QueryPlanHashProp, _hash);
        }

        // we store the context on the result for unit tests.
        operationContext.Result.SetContextData(QueryPlanProp, context.QueryPlan);

        // Enqueue root node to initiate the execution process.
        var rootSelectionSet = Unsafe.As<SelectionSet>(context.Operation.RootSelectionSet);
        var rootResult = context.Result.RentObject(rootSelectionSet.Selections.Count);

        context.Result.SetData(rootResult);
        context.RegisterState(rootSelectionSet, rootResult);

        try
        {
            await RootNode.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            context.DiagnosticEvents.QueryPlanExecutionError(ex);

            if (context.Result.Errors.Count == 0)
            {
                var errorHandler = context.ErrorHandler;
                var error = errorHandler.CreateUnexpectedError(ex).Build();
                error = errorHandler.Handle(error);
                context.Result.AddError(error);
            }
        }

        context.Result.RegisterForCleanup(context);

        return context.Result.BuildResult();
    }

    /// <summary>
    /// Executes a subscription query plan.
    /// </summary>
    /// <param name="context">
    /// The execution context.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a response stream that represents the subscription result.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The query plan represents a query or mutation request
    /// and cannot be subscribed to but must be executed.
    /// </exception>
    public Task<IResponseStream> SubscribeAsync(
        FusionExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (RootNode is not Subscribe subscriptionNode)
        {
            throw ThrowHelper.QueryAndMutationMustExecute();
        }

        var result = new ResponseStream(
            () => subscriptionNode.SubscribeAsync(context, cancellationToken),
            kind: ExecutionResultKind.SubscriptionResult);

        result.RegisterForCleanup(context);

        return Task.FromResult<IResponseStream>(result);
    }

    public void Format(IBufferWriter<byte> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        using var jsonWriter = new Utf8JsonWriter(writer, _jsonOptions);
        Format(jsonWriter);
        jsonWriter.Flush();
    }

    public void Format(Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();
        writer.WriteString(DocumentProp, _operation.Document.ToString(false));

        if (!string.IsNullOrEmpty(_operation.Name))
        {
            writer.WriteString(OperationProp, _operation.Name);
        }

        writer.WritePropertyName(RootNodeProp);
        RootNode.Format(writer);

        if (_exportKeyToVariableName.Length > 0)
        {
            writer.WritePropertyName(StateProp);

            writer.WriteStartObject();

            foreach (var (key, displayName) in _exportKeyToVariableName)
            {
                writer.WriteString(key, displayName);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    public override string ToString()
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        using var jsonWriter = new Utf8JsonWriter(bufferWriter, _jsonOptions);

        Format(jsonWriter);
        jsonWriter.Flush();

        return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
    }

    private static unsafe string ComputeHash(ReadOnlySpan<byte> document)
    {
        Span<byte> hash = stackalloc byte[SHA1.HashSizeInBytes];
        ComputeHash(ref hash, document);
        return Convert.ToHexString(hash);
    }

    private static void ComputeHash(ref Span<byte> hash, ReadOnlySpan<byte> document)
    {
        if (SHA1.TryHashData(document, hash, out var bytesWritten))
        {
            hash = hash[..bytesWritten];
        }
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

    private sealed class ExportPathVisitorContext
    {
        public Queue<string> Path { get; } = new();
    }
}
