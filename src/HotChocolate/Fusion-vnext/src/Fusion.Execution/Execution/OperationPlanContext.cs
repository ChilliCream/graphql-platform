using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Buffers;
using HotChocolate.Collections.Immutable;
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
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanContext : IFeatureProvider, IAsyncDisposable
{
    private static readonly JsonOperationPlanFormatter s_planFormatter = new();
    private readonly ConcurrentDictionary<int, List<ExecutionNode>> _nodesToComplete = new();
    private readonly ConcurrentDictionary<int, NodeContext> _nodeContexts = new();
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
    private readonly FetchResultStore _resultStore;
    private readonly ExecutionState _executionState;
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
            IncludeFlags);

        _executionState = new ExecutionState(_collectTelemetry, cancellationTokenSource);
    }

    public OperationPlan OperationPlan { get; }

    public IVariableValueCollection Variables { get; }

    public ISchemaDefinition Schema => RequestContext.Schema;

    public RequestContext RequestContext { get; }

    public ISourceSchemaClientScope ClientScope => _clientScope;

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
        var dependentNodes = _nodesToComplete.GetOrAdd(node.Id, _ => [dependentNode]);
        if (!dependentNodes.Contains(dependentNode))
        {
            dependentNodes.Add(dependentNode);
        }
    }

    internal ImmutableArray<ExecutionNode> GetDependentsToExecute(ExecutionNode node)
        => _nodesToComplete.TryGetValue(node.Id, out var nodesToComplete)
            ? [.. nodesToComplete]
            : [];

    internal void SetDynamicSchemaName(ExecutionNode node, string schemaName)
    {
        _nodeContexts.AddOrUpdate(
            node.Id,
            static (_, schemaName) => new NodeContext { SchemaName = schemaName },
            static (_, context, schemaName) => context with { SchemaName = schemaName },
            schemaName);
    }

    public string GetDynamicSchemaName(ExecutionNode node)
    {
        if (_nodeContexts.TryGetValue(node.Id, out var context)
            && !string.IsNullOrEmpty(context.SchemaName))
        {
            return context.SchemaName;
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

        _nodeContexts.AddOrUpdate(
            node.Id,
            static (_, variableValueSets) => new NodeContext { Variables = variableValueSets },
            static (_, context, variableValueSets) => context with { Variables = variableValueSets },
            variableValueSets);
    }

    internal ImmutableArray<VariableValues> GetVariableValueSets(ExecutionNode node)
    {
        if (!CollectTelemetry)
        {
            return [];
        }

        return _nodeContexts.TryGetValue(node.Id, out var context)
            ? context.Variables
            : [];
    }

    internal void TrackSourceSchemaClientResponse(ExecutionNode node, SourceSchemaClientResponse result)
    {
        if (!CollectTelemetry)
        {
            return;
        }

        _nodeContexts.AddOrUpdate(
            node.Id,
            static (_, result) => new NodeContext { Uri = result.Uri, ContentType = result.ContentType },
            static (_, context, result) => context with { Uri = result.Uri, ContentType = result.ContentType },
            result);
    }

    internal (Uri? Uri, string? ContentType) GetTransportDetails(ExecutionNode node)
    {
        if (!CollectTelemetry)
        {
            return (null, null);
        }

        return _nodeContexts.TryGetValue(node.Id, out var context)
            ? (context.Uri, context.ContentType)
            : (null, null);
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
                return [];
            }

            var variableValues = GetPathThroughVariables(forwardedVariables);
            return [new VariableValues(Path.Root, new ObjectValueNode(variableValues))];
        }
        else
        {
            var variableValues = GetPathThroughVariables(forwardedVariables);
            return _resultStore.CreateVariableValueSets(selectionSet, variableValues, requiredData);
        }
    }

    internal void AddPartialResults(
        SelectionPath sourcePath,
        ReadOnlySpan<SourceSchemaResult> results,
        ReadOnlySpan<string> responseNames)
    {
        var canExecutionContinue = _resultStore.AddPartialResults(sourcePath, results, responseNames);

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

    internal PooledArrayWriter CreateRentedBuffer()
        => _resultStore.CreateRentedBuffer();

    internal void Begin(long? start = null, string? traceId = null)
    {
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

    private List<ObjectFieldNode> GetPathThroughVariables(
        ReadOnlySpan<string> forwardedVariables)
    {
        if (Variables.IsEmpty || forwardedVariables.Length == 0)
        {
            return [];
        }

        var variables = new List<ObjectFieldNode>();

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

        return variables;
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
            _resultStore.Dispose();
            await _clientScope.DisposeAsync();
        }
    }

    private sealed record NodeContext
    {
        public string? SchemaName { get; init; }

        public ImmutableArray<VariableValues> Variables { get; init; } = [];

        public Uri? Uri { get; init; }

        public string? ContentType { get; init; }
    }
}
