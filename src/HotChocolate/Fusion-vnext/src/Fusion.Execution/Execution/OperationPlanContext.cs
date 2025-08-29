using System.Collections.Concurrent;
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
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

// TODO : make poolable
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
    private ResultPoolSessionHolder _resultPoolSessionHolder;
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

        _resultPoolSessionHolder = requestContext.CreateResultPoolSession();
        _collectTelemetry = requestContext.CollectOperationPlanTelemetry();
        _clientScope = requestContext.CreateClientScope();
        _nodeIdParser = requestContext.Schema.Services.GetRequiredService<INodeIdParser>();
        _diagnosticEvents = requestContext.Schema.Services.GetRequiredService<IFusionExecutionDiagnosticEvents>();

        _resultStore = new FetchResultStore(
            requestContext.Schema,
            _resultPoolSessionHolder,
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

    public ResultPoolSession ResultPool => _resultPoolSessionHolder;

    internal ExecutionState ExecutionState => _executionState;

    public ulong IncludeFlags { get; }

    public bool CollectTelemetry => _collectTelemetry;

    public IFeatureCollection Features => RequestContext.Features;

    public ImmutableArray<ExecutionNodeTrace> Traces { get; internal set; } = [];

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

    internal string GetDynamicSchemaName(ExecutionNode node)
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

        return _nodeContexts.TryGetValue(node.Id, out var variableValueSets)
            ? variableValueSets.Variables
            : [];
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

    internal void AddPartialResults(ObjectResult result, ReadOnlySpan<Selection> selections)
        => _resultStore.AddPartialResults(result, selections);

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

    internal IOperationResult Complete()
    {
        var environment = Schema.TryGetEnvironment();

        var trace = _collectTelemetry
            ? new OperationPlanTrace
            {
                TraceId = _traceId,
                AppId = environment?.AppId,
                EnvironmentName = environment?.Name,
                Duration = Stopwatch.GetElapsedTime(_start),
                Nodes = Traces.ToImmutableDictionary(t => t.Id)
            }
            : null;

        var resultBuilder = OperationResultBuilder.New();

        if (RequestContext.ContextData.ContainsKey(ExecutionContextData.IncludeOperationPlan))
        {
            var writer = new PooledArrayWriter();
            s_planFormatter.Format(writer, OperationPlan, trace);
            var value = new RawJsonValue(writer.WrittenMemory);
            resultBuilder.SetExtension("fusion", new Dictionary<string, object?> { { "operationPlan", value } });
            resultBuilder.RegisterForCleanup(writer);
        }

        var result = resultBuilder
            .AddErrors(_resultStore.Errors)
            .SetData(_resultStore.Data.IsInvalidated ? null : _resultStore.Data)
            .RegisterForCleanup(_resultStore.MemoryOwners)
            .RegisterForCleanup(_resultPoolSessionHolder)
            .Build();

        result.Features.Set(OperationPlan);

        if (trace is not null)
        {
            result.Features.Set(trace);
        }

        _clientScope = RequestContext.CreateClientScope();
        _resultPoolSessionHolder = RequestContext.CreateResultPoolSession();
        _resultStore.Reset(_resultPoolSessionHolder);

        return result;
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
            _resultPoolSessionHolder.Dispose();
            _resultStore.Dispose();
            await _clientScope.DisposeAsync();
        }
    }

    private sealed record NodeContext
    {
        public string? SchemaName { get; init; }

        [SuppressMessage("ReSharper", "TypeWithSuspiciousEqualityIsUsedInRecord.Local")]
        public ImmutableArray<VariableValues> Variables { get; init; } = [];
    }
}

file static class OperationPlanContextExtensions
{
    public static OperationResultBuilder RegisterForCleanup(
        this OperationResultBuilder builder,
        ConcurrentStack<IDisposable> disposables)
    {
        while (disposables.TryPop(out var disposable))
        {
            RegisterForCleanup(builder, disposable);
        }

        return builder;
    }

    public static OperationResultBuilder RegisterForCleanup(
        this OperationResultBuilder builder,
        IDisposable disposable)
    {
        builder.RegisterForCleanup(disposable.Dispose);
        return builder;
    }
}
