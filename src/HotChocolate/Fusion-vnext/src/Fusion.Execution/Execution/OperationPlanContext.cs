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
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanContext : IFeatureProvider, IAsyncDisposable
{
    private static readonly JsonOperationPlanFormatter s_planFormatter = new();
    private readonly FetchResultStore _resultStore;
    private readonly bool _collectTelemetry;
    private string? _traceId;
    private long _start;
    private bool _disposed;

    public OperationPlanContext(
        OperationPlan operationPlan,
        IVariableValueCollection variables,
        RequestContext requestContext,
        ResultPoolSession resultPoolSession)
    {
        OperationPlan = operationPlan;
        RequestContext = requestContext;
        Variables = variables;
        IncludeFlags = operationPlan.Operation.CreateIncludeFlags(variables);

        // TODO : fully implement and inject ResultPoolSession
        _resultStore = new FetchResultStore(
            RequestContext.Schema,
            resultPoolSession,
            operationPlan.Operation,
            IncludeFlags);

        _collectTelemetry = RequestContext.Schema.GetRequestOptions().CollectOperationPlanTelemetry;

        // create a client scope for the current request context.
        var clientScopeFactory = requestContext.RequestServices.GetRequiredService<ISourceSchemaClientScopeFactory>();
        ClientScope = clientScopeFactory.CreateScope(requestContext.Schema);
        ResultPool = resultPoolSession;
    }

    public OperationPlan OperationPlan { get; }

    public IVariableValueCollection Variables { get; }

    public ISchemaDefinition Schema => RequestContext.Schema;

    public RequestContext RequestContext { get; }

    public ISourceSchemaClientScope ClientScope { get; }

    public ResultPoolSession ResultPool { get; }

    public ulong IncludeFlags { get; }

    public IFeatureCollection Features => RequestContext.Features;

    public ImmutableArray<ExecutionNodeTrace> Traces { get; set; } = ImmutableArray<ExecutionNodeTrace>.Empty;

    public ImmutableArray<VariableValues> CreateVariableValueSets(
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

    public void AddPartialResults(SelectionPath sourcePath, ReadOnlySpan<SourceSchemaResult> results)
        => _resultStore.AddPartialResults(sourcePath, results);

    public void AddPartialResults(ObjectResult result, ReadOnlySpan<Selection> selections)
        => _resultStore.AddPartialResults(result, selections);

    public PooledArrayWriter CreateRentedBuffer()
        => _resultStore.CreateRentedBuffer();

    public void Begin()
    {
        if (_collectTelemetry)
        {
            _traceId = Activity.Current?.TraceId.ToHexString();
            _start = Stopwatch.GetTimestamp();
        }
    }

    internal IExecutionResult Complete()
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
            .Build();

        result.Features.Set(OperationPlan);

        if (trace is not null)
        {
            result.Features.Set(trace);
        }

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
            await ClientScope.DisposeAsync().ConfigureAwait(false);
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
            builder.RegisterForCleanup(() =>
            {
                disposable.Dispose();
                return ValueTask.CompletedTask;
            });
        }

        return builder;
    }
}
