using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanContext : IFeatureProvider, IAsyncDisposable
{
    private static readonly JsonOperationPlanFormatter s_planFormatter = new();
    private readonly FetchResultStore _resultStore;
    private readonly bool _collectTelemetry;
    private ResultPoolSessionHolder _resultPoolSessionHolder;
    private ISourceSchemaClientScope _clientScope;
    private string? _traceId;
    private long _start;
    private bool _disposed;

    public OperationPlanContext(
        RequestContext requestContext,
        OperationPlan operationPlan)
        : this(requestContext, requestContext.VariableValues[0], operationPlan)
    {
    }

    public OperationPlanContext(
        RequestContext requestContext,
        IVariableValueCollection variables,
        OperationPlan operationPlan)
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

        _resultStore = new FetchResultStore(
            requestContext.Schema,
            _resultPoolSessionHolder,
            operationPlan.Operation,
            IncludeFlags);
    }

    public OperationPlan OperationPlan { get; }

    public IVariableValueCollection Variables { get; }

    public ISchemaDefinition Schema => RequestContext.Schema;

    public RequestContext RequestContext { get; }

    public ISourceSchemaClientScope ClientScope => _clientScope;

    public ResultPoolSession ResultPool => _resultPoolSessionHolder;

    public ulong IncludeFlags { get; }

    public bool CollectTelemetry => _collectTelemetry;

    public IFeatureCollection Features => RequestContext.Features;

    public ImmutableArray<ExecutionNodeTrace> Traces { get; internal set; } = [];

    internal ImmutableArray<VariableValues> CreateVariableValueSets(
        SelectionPath selectionSet,
        ReadOnlySpan<string> requiredVariables,
        ReadOnlySpan<OperationRequirement> requiredData)
    {
        ArgumentNullException.ThrowIfNull(selectionSet);

        if (requiredData.Length == 0)
        {
            if (requiredVariables.Length == 0)
            {
                return [];
            }

            var variableValues = GetPathThroughVariables(requiredVariables);
            return [new VariableValues(Path.Root, new ObjectValueNode(variableValues))];
        }
        else
        {
            var variableValues = GetPathThroughVariables(requiredVariables);
            return _resultStore.CreateVariableValueSets(selectionSet, variableValues, requiredData);
        }
    }

    internal void AddPartialResults(SelectionPath sourcePath, ReadOnlySpan<SourceSchemaResult> results)
        => _resultStore.AddPartialResults(sourcePath, results);

    internal void AddPartialResults(ObjectResult result, ReadOnlySpan<Selection> selections)
        => _resultStore.AddPartialResults(result, selections);

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
        var trace = _collectTelemetry
            ? new OperationPlanTrace
            {
                TraceId = _traceId,
                Duration = Stopwatch.GetElapsedTime(_start),
                Nodes = Traces.ToImmutableDictionary(t => t.Id)
            }
            : null;

        var resultBuilder = OperationResultBuilder.New();

        if (RequestContext.ContextData.ContainsKey(ExecutionContextData.IncludeQueryPlan))
        {
            var writer = new PooledArrayWriter();
            s_planFormatter.Format(writer, OperationPlan, trace);
            var value = new RawJsonValue(writer.WrittenMemory);
            resultBuilder.SetExtension("fusion", new Dictionary<string, object?> { { "operationPlan", value } });
            resultBuilder.RegisterForCleanup(writer);
        }

        var result = resultBuilder
            .AddErrors(_resultStore.Errors)
            .SetData(_resultStore.Data)
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
        ReadOnlySpan<string> requiredVariables)
    {
        if (Variables.IsEmpty || requiredVariables.Length == 0)
        {
            return [];
        }

        var variables = new List<ObjectFieldNode>();

        foreach (var variableName in requiredVariables)
        {
            if (Variables.TryGetValue<IValueNode>(variableName, out var variableValue))
            {
                variables.Add(new ObjectFieldNode(variableName, variableValue));
            }
            else
            {
                throw new InvalidOperationException(
                    $"The variable '{variableName}' is not defined.");
            }
        }

        return variables;
    }

    public ISourceSchemaClient GetClient(string schemaName, OperationType operationType)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaName);

        return ClientScope.GetClient(schemaName, operationType);
    }

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
