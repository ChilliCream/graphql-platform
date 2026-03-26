using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Execution.Results;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Provides the execution context for a single Fusion operation plan execution.
/// </summary>
public sealed partial class OperationPlanContext : IFeatureProvider, IAsyncDisposable
{
    private static readonly JsonOperationPlanFormatter s_planFormatter = new();
    private NodeCompletionSet?[] _nodesToComplete = [];
    private int _dependentBitsetWordCount;
    private string?[] _schemaNames = [];
    private ImmutableArray<VariableValues>[] _variableValueSets = [];
    private Uri?[] _transportUris = [];
    private string?[] _transportContentTypes = [];
    private List<IOperationPlanNode>?[] _skippedDefinitions = [];
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
    private readonly FetchResultStore _resultStore;
    private readonly ExecutionState _executionState;
    private readonly INodeIdParser _nodeIdParser;
    private readonly IErrorHandler _errorHandler;
    private bool _collectTelemetry;
    private ISourceSchemaClientScope _clientScope = default!;
    private string? _traceId;
    private long _start;
    private bool _disposed;
    private int _nodeSlotCapacity;
    internal OperationPlanContextPool? _pool;

    /// <summary>
    /// Gets the operation plan being executed.
    /// </summary>
    public OperationPlan OperationPlan { get; private set; } = default!;

    /// <summary>
    /// Gets the coerced variable values for the current request.
    /// </summary>
    public IVariableValueCollection Variables { get; private set; } = default!;

    /// <summary>
    /// Gets the schema definition associated with this execution.
    /// </summary>
    public ISchemaDefinition Schema => RequestContext.Schema;

    /// <summary>
    /// Gets the request context for the current request.
    /// </summary>
    public RequestContext RequestContext { get; private set; } = default!;

    /// <summary>
    /// Gets the source schema client scope used to obtain HTTP clients for downstream subgraphs.
    /// </summary>
    public ISourceSchemaClientScope ClientScope => _clientScope;

    internal ExecutionState ExecutionState => _executionState;

    internal bool IsNodeSkipped(int nodeId)
        => _executionState.IsNodeSkipped(nodeId);

    /// <summary>
    /// Gets the evaluated include flags derived from <c>@skip</c> and <c>@include</c> directives.
    /// </summary>
    public ulong IncludeFlags { get; private set; }

    /// <summary>
    /// Gets a value indicating whether operation plan telemetry is being collected for this request.
    /// </summary>
    public bool CollectTelemetry => _collectTelemetry;

    /// <summary>
    /// Gets the feature collection associated with the current request.
    /// </summary>
    public IFeatureCollection Features => RequestContext.Features;

    /// <summary>
    /// Gets the execution traces collected during plan execution.
    /// Only populated when <see cref="CollectTelemetry"/> is <c>true</c>.
    /// </summary>
    public ImmutableDictionary<int, ExecutionNodeTrace> Traces { get; internal set; } =
#if NET10_0_OR_GREATER
        [];
#else
        ImmutableDictionary<int, ExecutionNodeTrace>.Empty;
#endif

    /// <summary>
    /// Gets the diagnostic events handler for the Fusion execution pipeline.
    /// </summary>
    public IFusionExecutionDiagnosticEvents DiagnosticEvents => _diagnosticEvents;

    internal void EnqueueForExecution(ExecutionNode node, ExecutionNode dependentNode)
    {
        var nodeId = node.Id;
        var nodeCompletionSet = _nodesToComplete[nodeId];

        if (nodeCompletionSet is null)
        {
            var newSet = new NodeCompletionSet(_dependentBitsetWordCount);
            nodeCompletionSet = Interlocked.CompareExchange(ref _nodesToComplete[nodeId], newSet, null) ?? newSet;
        }

        nodeCompletionSet.Add(dependentNode);
    }

    internal ImmutableArray<ExecutionNode> GetDependentsToExecute(ExecutionNode node)
    {
        var nodeCompletionSet = _nodesToComplete[node.Id];
        return nodeCompletionSet?.GetSnapshot() ?? [];
    }

    internal void TrackSkippedDefinition(ExecutionNode node, IOperationPlanNode skippedDefinition)
    {
        var nodeId = node.Id;
        var list = _skippedDefinitions[nodeId];

        if (list is null)
        {
            list = [];
            _skippedDefinitions[nodeId] = list;
        }

        list.Add(skippedDefinition);
    }

    internal ImmutableArray<IOperationPlanNode> GetSkippedDefinitions(ExecutionNode node)
    {
        var list = _skippedDefinitions[node.Id];
        return list is null or { Count: 0 } ? [] : [.. list];
    }

    internal void SetDynamicSchemaName(ExecutionNode node, string schemaName)
        => _schemaNames[node.Id] = schemaName;

    /// <summary>
    /// Gets the dynamically resolved schema name for the specified execution node.
    /// </summary>
    /// <param name="node">The execution node whose schema name to retrieve.</param>
    /// <returns>The schema name assigned to the node.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no schema name has been assigned to the node.
    /// </exception>
    public string GetDynamicSchemaName(ExecutionNode node)
    {
        var schemaName = _schemaNames[node.Id];

        if (!string.IsNullOrEmpty(schemaName))
        {
            return schemaName;
        }

        throw new InvalidOperationException(
            $"Expected to find a schema name for a dynamic operation node '{node.Id}'.");
    }

    internal bool TryGetNodeLookupSchemaForType(string typeName, [NotNullWhen(true)] out string? schemaName)
        => RequestContext.Schema.Features.GetRequired<NodeFallbackLookup>()
            .TryGetNodeLookupSchemaForType(typeName, out schemaName);

    internal void TrackVariableValueSets(ExecutionNode node, ImmutableArray<VariableValues> variableValueSets)
    {
        if (!CollectTelemetry || variableValueSets.IsEmpty)
        {
            return;
        }

        _variableValueSets[node.Id] = variableValueSets;
    }

    internal ImmutableArray<VariableValues> GetVariableValueSets(ExecutionNode node)
    {
        if (!CollectTelemetry)
        {
            return [];
        }

        var variableValueSets = _variableValueSets[node.Id];
        return variableValueSets.IsDefault ? [] : variableValueSets;
    }

    internal void TrackSourceSchemaClientResponse(ExecutionNode node, SourceSchemaClientResponse result)
    {
        if (!CollectTelemetry)
        {
            return;
        }

        _transportUris[node.Id] = result.Uri;
        _transportContentTypes[node.Id] = result.ContentType;
    }

    internal (Uri? Uri, string? ContentType) GetTransportDetails(ExecutionNode node)
    {
        if (!CollectTelemetry)
        {
            return (null, null);
        }

        return (_transportUris[node.Id], _transportContentTypes[node.Id]);
    }

    internal void CompleteNode(ExecutionNodeResult result)
        => _executionState.EnqueueForCompletion(result);

    internal ImmutableArray<VariableValues> CreateVariableValueSets(
        SelectionPath selectionSet,
        ReadOnlySpan<string> forwardedVariables,
        ReadOnlySpan<OperationRequirement> requirements)
    {
        ArgumentNullException.ThrowIfNull(selectionSet);

        if (requirements.Length == 0)
        {
            if (!selectionSet.IsRoot)
            {
                throw new InvalidOperationException(
                    "Non-root selection paths must have requirements or forwarded variables.");
            }


            if (forwardedVariables.Length == 0)
            {
                return [];
            }

            var variableValues = GetPathThroughVariables(forwardedVariables);
            return [_resultStore.CreateVariableValueSets(CompactPath.Root, variableValues)];
        }
        else
        {
            var variableValues = GetPathThroughVariables(forwardedVariables);
            return _resultStore.CreateVariableValueSets(selectionSet, variableValues, requirements);
        }
    }

    internal ImmutableArray<VariableValues> CreateVariableValueSets(
        ReadOnlySpan<SelectionPath> selectionSets,
        ReadOnlySpan<string> forwardedVariables,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        if (requiredData.Length == 0)
        {
            if (forwardedVariables.Length == 0)
            {
                return [];
            }

            var variableValues = GetPathThroughVariables(forwardedVariables);
            return [_resultStore.CreateVariableValueSets(CompactPath.Root, variableValues)];
        }
        else
        {
            var variableValues = GetPathThroughVariables(forwardedVariables);
            return _resultStore.CreateVariableValueSets(selectionSets, variableValues, requiredData);
        }
    }

    private CompactPath ToResultPath(SelectionPath selectionSet)
    {
        if (selectionSet.IsRoot)
        {
            return CompactPath.Root;
        }

        // CompactPathBuilder can run concurrently across nodes; avoid using the request-local
        // pool here since that pool is synchronized through FetchResultStore's lock.
        Span<int> buffer = stackalloc int[32];
        var builder = new CompactPathBuilder(buffer, pool: null);
        var operation = OperationPlan.Operation;
        var currentSelectionSet = operation.RootSelectionSet;
        Selection? currentSelection = null;

        for (var i = 0; i < selectionSet.Length; i++)
        {
            var segment = selectionSet[i];

            if (segment.Kind is SelectionPathSegmentKind.Root)
            {
                continue;
            }

            if (segment.Kind is SelectionPathSegmentKind.InlineFragment)
            {
                if (currentSelection is null)
                {
                    continue;
                }

                var objectType = Schema.Types.GetType<IObjectTypeDefinition>(segment.Name);
                currentSelectionSet = operation.GetSelectionSet(currentSelection, objectType);
                continue;
            }

            if (!currentSelectionSet.TryGetSelection(segment.Name, out var selection))
            {
                throw new InvalidOperationException(
                    $"Could not resolve selection path segment '{segment.Name}'.");
            }

            builder.AppendField(selection.Id);
            currentSelection = selection;

            if (selection.Type.NamedType() is IObjectTypeDefinition objectTypeForSelection)
            {
                currentSelectionSet = operation.GetSelectionSet(selection, objectTypeForSelection);
            }
        }

        return builder.ToPath();
    }

    internal void AddPartialResult(
        SelectionPath sourcePath,
        SourceSchemaResult result,
        ResultSelectionSet resultSelectionSet,
        bool containsErrors)
    {
        var canExecutionContinue =
            _resultStore.AddPartialResult(sourcePath, result, resultSelectionSet, containsErrors);

        if (!canExecutionContinue)
        {
            ExecutionState.CancelProcessing();
        }
    }

    internal void AddPartialResults(
        SelectionPath sourcePath,
        ReadOnlySpan<SourceSchemaResult> results,
        ResultSelectionSet resultSelectionSet,
        bool containsErrors)
    {
        var canExecutionContinue =
            _resultStore.AddPartialResults(sourcePath, results, resultSelectionSet, containsErrors);

        if (!canExecutionContinue)
        {
            ExecutionState.CancelProcessing();
        }
    }

    internal void AddPartialResults(SourceResultDocument result, ResultSelectionSet resultSelectionSet)
    {
        var canExecutionContinue = _resultStore.AddPartialResults(result, resultSelectionSet);

        if (!canExecutionContinue)
        {
            ExecutionState.CancelProcessing();
        }
    }

    internal void AddErrors(
        IError error,
        ResultSelectionSet resultSelectionSet,
        params ReadOnlySpan<Path> paths)
    {
        var canExecutionContinue = _resultStore.AddErrors(error, resultSelectionSet, paths);

        if (!canExecutionContinue)
        {
            ExecutionState.CancelProcessing();
        }
    }

    internal void AddErrors(IError error, ResultSelectionSet resultSelectionSet, ReadOnlySpan<CompactPath> paths)
    {
        var canExecutionContinue = _resultStore.AddErrors(error, resultSelectionSet, paths);

        if (!canExecutionContinue)
        {
            ExecutionState.CancelProcessing();
        }
    }

    internal PooledArrayWriter CreateRentedBuffer()
        => _resultStore.CreateRentedBuffer();

    internal void Begin(long? start = null, string? traceId = null)
    {
        ResetNodeState();

        if (_collectTelemetry)
        {
            _start = start ?? Stopwatch.GetTimestamp();
            _traceId = traceId ?? Activity.Current?.TraceId.ToHexString();
        }
    }

    internal OperationResult Complete(bool reusable = false)
    {
        _resultStore.FinalizePocketedErrors();

        var environment = Schema.TryGetEnvironment();

        var trace = _collectTelemetry
            ? new OperationPlanTrace
            {
                TraceId = _traceId,
                AppId = environment?.AppId,
                EnvironmentName = environment?.Name,
                Duration = Stopwatch.GetElapsedTime(_start),
                Nodes = Traces
            }
            : null;

        var resultDocument = _resultStore.Result;
        var operationResult = new OperationResult(
            new OperationResultData(
                resultDocument,
                resultDocument.Data.IsNullOrInvalidated,
                resultDocument,
                resultDocument),
            _resultStore.Errors?.ToImmutableList());

        // we take over the memory owners from the result context
        // and store them on the response so that the server can
        // dispose them when it disposes of the result itself.
        var memoryOwners = _resultStore.MemoryOwners;
        foreach (var disposable in memoryOwners)
        {
            operationResult.RegisterForCleanup(disposable);
        }

        memoryOwners.Clear();

        operationResult.Features.Set(OperationPlan);

        if (RequestContext.ContextData.ContainsKey(ExecutionContextData.IncludeOperationPlan))
        {
            var writer = new PooledArrayWriter();
            s_planFormatter.Format(writer, OperationPlan, trace);
            var value = new RawJsonValue(writer.WrittenMemory);
            operationResult.Extensions = operationResult.Extensions.SetItem(
                "fusion",
                new Dictionary<string, object?> { { "operationPlan", value } });
            operationResult.RegisterForCleanup(writer);
        }

        if (trace is not null)
        {
            operationResult.Features.Set(trace);
        }

        Debug.Assert(
            !resultDocument.Data.IsInvalidated
                || operationResult.Errors.Count > 0,
            "Expected to either valid data or errors");

        // resets the store and client scope for another execution.
        if (reusable)
        {
            _clientScope = RequestContext.CreateClientScope();
            _resultStore.Reset();
        }

        return operationResult;
    }

    private IReadOnlyList<ObjectFieldNode> GetPathThroughVariables(
        ReadOnlySpan<string> forwardedVariables)
    {
        if (Variables.IsEmpty || forwardedVariables.Length == 0)
        {
            return Array.Empty<ObjectFieldNode>();
        }

        var variables = new List<ObjectFieldNode>(forwardedVariables.Length);

        foreach (var variableName in forwardedVariables)
        {
            // we pass through the required pass through variables,
            // if they were not omitted.
            //
            // it is valid for the GraphQL request to omit nullable variables.
            //
            // if they were not nullable we would not get here as the
            // GraphQL validation would reject such a request.
            //
            // but even if the validation failed we do not need to
            // guard against it and can just pass this to the
            // source schema which would in any case validate
            // any request and would reject it if a required
            // variable was missing.
            if (Variables.TryGetValue<IValueNode>(variableName, out var variableValue))
            {
                variables.Add(new ObjectFieldNode(variableName, variableValue));
            }
        }

        return variables.Count == 0
            ? Array.Empty<ObjectFieldNode>()
            : variables;
    }

    /// <summary>
    /// Gets or creates a source schema client for the specified schema and operation type.
    /// </summary>
    /// <param name="schemaName">The name of the downstream subgraph schema.</param>
    /// <param name="operationType">The GraphQL operation type (query, mutation, subscription).</param>
    /// <returns>The source schema client for communicating with the downstream subgraph.</returns>
    public ISourceSchemaClient GetClient(string schemaName, OperationType operationType)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaName);

        return ClientScope.GetClient(schemaName, operationType);
    }

    /// <summary>
    /// Tries to extract the type name from a relay-style global node identifier.
    /// </summary>
    /// <param name="id">The global node identifier to parse.</param>
    /// <param name="typeName">When successful, the extracted type name.</param>
    /// <returns><c>true</c> if the type name was successfully extracted; otherwise, <c>false</c>.</returns>
    public bool TryParseTypeNameFromId(string id, [NotNullWhen(true)] out string? typeName)
        => _nodeIdParser.TryParseTypeName(id, out typeName);

    private void ResetNodeState()
    {
        Array.Clear(_schemaNames);
        Array.Clear(_skippedDefinitions);

        if (_collectTelemetry)
        {
            Array.Clear(_variableValueSets);
            Array.Clear(_transportUris);
            Array.Clear(_transportContentTypes);
        }

        foreach (var nodeCompletionSet in _nodesToComplete)
        {
            nodeCompletionSet?.Reset();
        }
    }

    private void DisposeNodeState()
    {
        foreach (var nodeCompletionSet in _nodesToComplete)
        {
            nodeCompletionSet?.Dispose();
        }
    }

    private sealed class NodeCompletionSet(int bitsetWordCount) : IDisposable
    {
#if NET9_0_OR_GREATER
        private readonly Lock _sync = new();
#else
        private readonly object _sync = new();
#endif
        private ExecutionNode[] _dependents = [];
        private ulong[]? _seenDependents;
        private int _count;

        public void Add(ExecutionNode node)
        {
            lock (_sync)
            {
                _seenDependents ??= RentBitset(bitsetWordCount);

                var nodeId = node.Id;
                var index = nodeId >> 6;
                var bit = 1UL << (nodeId & 63);

                if ((_seenDependents[index] & bit) != 0)
                {
                    return;
                }

                _seenDependents[index] |= bit;

                if (_dependents.Length == 0)
                {
                    _dependents = new ExecutionNode[2];
                }
                else if (_count == _dependents.Length)
                {
                    Array.Resize(ref _dependents, _dependents.Length * 2);
                }

                _dependents[_count++] = node;
            }
        }

        public ImmutableArray<ExecutionNode> GetSnapshot()
        {
            lock (_sync)
            {
                if (_count == 0)
                {
                    return [];
                }

                return ImmutableArray.Create(_dependents, 0, _count);
            }
        }

        public void Reset()
        {
            lock (_sync)
            {
                _count = 0;

                if (_seenDependents is not null)
                {
                    Array.Clear(_seenDependents, 0, bitsetWordCount);
                }
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_seenDependents is not null)
                {
                    ArrayPool<ulong>.Shared.Return(_seenDependents, clearArray: true);
                    _seenDependents = null;
                }

                _count = 0;
                _dependents = [];
            }
        }

        private static ulong[] RentBitset(int bitsetWordCount)
        {
            var seenDependents = ArrayPool<ulong>.Shared.Rent(bitsetWordCount);
            Array.Clear(seenDependents, 0, bitsetWordCount);
            return seenDependents;
        }
    }
}
