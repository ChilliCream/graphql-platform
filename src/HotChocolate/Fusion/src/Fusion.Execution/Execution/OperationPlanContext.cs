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
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanContext : IFeatureProvider, IAsyncDisposable
{
    private static readonly JsonOperationPlanFormatter s_planFormatter = new();
    private readonly NodeCompletionSet?[] _nodesToComplete;
    private readonly int _dependentBitsetWordCount;
    private readonly string?[] _schemaNames;
    private readonly ImmutableArray<VariableValues>[] _variableValueSets;
    private readonly Uri?[] _transportUris;
    private readonly string?[] _transportContentTypes;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
    private readonly FetchResultStore _resultStore;
    private readonly ExecutionState _executionState;
    private readonly SourceSchemaRequestDispatcher _sourceSchemaDispatcher;
    private readonly INodeIdParser _nodeIdParser;
    private readonly bool _collectTelemetry;
    private ISourceSchemaClientScope _clientScope;
    private string? _traceId;
    private long _start;
    private bool _disposed;

    public OperationPlanContext(
        RequestContext requestContext,
        OperationPlan operationPlan,
        CancellationTokenSource cancellationTokenSource)
        : this(requestContext, requestContext.VariableValues[0], operationPlan, cancellationTokenSource)
    {
    }

    public OperationPlanContext(
        RequestContext requestContext,
        IVariableValueCollection variables,
        OperationPlan operationPlan,
        CancellationTokenSource cancellationTokenSource)
    {
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(operationPlan);

        RequestContext = requestContext;
        Variables = variables;
        OperationPlan = operationPlan;
        IncludeFlags = operationPlan.Operation.CreateIncludeFlags(variables);

        _collectTelemetry = requestContext.CollectOperationPlanTelemetry();
        _clientScope = requestContext.CreateClientScope();
        _nodeIdParser = requestContext.Schema.Services.GetRequiredService<INodeIdParser>();
        _diagnosticEvents = requestContext.Schema.Services.GetRequiredService<IFusionExecutionDiagnosticEvents>();
        var errorHandler = requestContext.Schema.Services.GetRequiredService<IErrorHandler>();

        _resultStore = new FetchResultStore(
            requestContext.Schema,
            errorHandler,
            operationPlan.Operation,
            requestContext.ErrorHandlingMode(),
            IncludeFlags,
            requestContext.Schema.GetOptions().PathSegmentLocalPoolCapacity);

        _executionState = new ExecutionState(_collectTelemetry, cancellationTokenSource);
        _sourceSchemaDispatcher = new SourceSchemaRequestDispatcher(this);

        var maxNodeId = 0;

        foreach (var executionNode in operationPlan.AllNodes)
        {
            if (executionNode.Id > maxNodeId)
            {
                maxNodeId = executionNode.Id;
            }
        }

        var nodeSlotCount = maxNodeId + 1;
        _dependentBitsetWordCount = (maxNodeId >> 6) + 1;
        _nodesToComplete = new NodeCompletionSet?[nodeSlotCount];
        _schemaNames = new string?[nodeSlotCount];
        _variableValueSets = new ImmutableArray<VariableValues>[nodeSlotCount];
        _transportUris = new Uri?[nodeSlotCount];
        _transportContentTypes = new string?[nodeSlotCount];
    }

    public OperationPlan OperationPlan { get; }

    public IVariableValueCollection Variables { get; }

    public ISchemaDefinition Schema => RequestContext.Schema;

    public RequestContext RequestContext { get; }

    public ISourceSchemaClientScope ClientScope => _clientScope;

    public ISourceSchemaScheduler SourceSchemaScheduler => _sourceSchemaDispatcher;

    public ISourceSchemaDispatcher SourceSchemaDispatcher => _sourceSchemaDispatcher;

    internal ExecutionState ExecutionState => _executionState;

    public ulong IncludeFlags { get; }

    public bool CollectTelemetry => _collectTelemetry;

    public IFeatureCollection Features => RequestContext.Features;

    public ImmutableDictionary<int, ExecutionNodeTrace> Traces { get; internal set; } =
#if NET10_0_OR_GREATER
        [];
#else
        ImmutableDictionary<int, ExecutionNodeTrace>.Empty;
#endif

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

    internal void SetDynamicSchemaName(ExecutionNode node, string schemaName)
        => _schemaNames[node.Id] = schemaName;

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
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        ArgumentNullException.ThrowIfNull(selectionSet);

        if (requiredData.Length == 0)
        {
            if (forwardedVariables.Length == 0)
            {
                if (selectionSet.IsRoot)
                {
                    return [];
                }

                return [new VariableValues(ToResultPath(selectionSet), new ObjectValueNode([]))];
            }

            var variableValues = GetPathThroughVariables(forwardedVariables);
            return [new VariableValues(CompactPath.Root, new ObjectValueNode(variableValues))];
        }
        else
        {
            var variableValues = GetPathThroughVariables(forwardedVariables);
            return _resultStore.CreateVariableValueSets(selectionSet, variableValues, requiredData);
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
            return [new VariableValues(CompactPath.Root, new ObjectValueNode(variableValues))];
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

        Span<int> buffer = stackalloc int[32];
        // This helper can run concurrently across nodes; avoid using the request-local
        // pool here since that pool is synchronized through FetchResultStore's lock.
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

    internal void AddPartialResults(
        SelectionPath sourcePath,
        ReadOnlySpan<SourceSchemaResult> results,
        ReadOnlySpan<string> responseNames,
        bool containsErrors = true)
    {
        var canExecutionContinue =
            _resultStore.AddPartialResults(
                sourcePath,
                results,
                responseNames,
                containsErrors);

        if (!canExecutionContinue)
        {
            ExecutionState.CancelProcessing();
        }
    }

    internal void AddPartialResults(SourceResultDocument result, ReadOnlySpan<string> responseNames)
    {
        var canExecutionContinue = _resultStore.AddPartialResults(result, responseNames);

        if (!canExecutionContinue)
        {
            ExecutionState.CancelProcessing();
        }
    }

    internal void AddErrors(IError error, ReadOnlySpan<string> responseNames, params ReadOnlySpan<Path> paths)
    {
        var canExecutionContinue = _resultStore.AddErrors(error, responseNames, paths);

        if (!canExecutionContinue)
        {
            ExecutionState.CancelProcessing();
        }
    }

    internal void AddErrors(IError error, ReadOnlySpan<string> responseNames, ReadOnlySpan<CompactPath> paths)
    {
        var canExecutionContinue = _resultStore.AddErrors(error, responseNames, paths);

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
        while (_resultStore.MemoryOwners.TryPop(out var disposable))
        {
            operationResult.RegisterForCleanup(disposable);
        }

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

    public ISourceSchemaClient GetClient(string schemaName, OperationType operationType)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaName);

        return ClientScope.GetClient(schemaName, operationType);
    }

    public bool TryParseTypeNameFromId(string id, [NotNullWhen(true)] out string? typeName)
        => _nodeIdParser.TryParseTypeName(id, out typeName);

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            DisposeNodeState();
            _sourceSchemaDispatcher.Abort();
            _resultStore.Dispose();
            await _clientScope.DisposeAsync();
        }
    }

    private void ResetNodeState()
    {
        Array.Clear(_schemaNames);

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
        private readonly object _sync = new();
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
