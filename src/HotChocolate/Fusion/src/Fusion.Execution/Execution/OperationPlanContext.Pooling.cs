using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Results;

namespace HotChocolate.Fusion.Execution;

public sealed partial class OperationPlanContext
{
    internal OperationPlanContext(
        INodeIdParser nodeIdParser,
        IFusionExecutionDiagnosticEvents diagnosticEvents,
        IErrorHandler errorHandler)
    {
        _nodeIdParser = nodeIdParser;
        _diagnosticEvents = diagnosticEvents;
        _errorHandler = errorHandler;
        _resultStore = new FetchResultStore();
        _executionState = new ExecutionState();
    }

    internal void Initialize(
        RequestContext requestContext,
        IVariableValueCollection variables,
        OperationPlan operationPlan,
        CancellationTokenSource cancellationTokenSource)
    {
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(operationPlan);

        _disposed = 0;
        RequestContext = requestContext;
        Variables = variables;
        OperationPlan = operationPlan;
        IncludeFlags = operationPlan.Operation.CreateIncludeFlags(variables);
        DeferFlags = operationPlan.Operation.CreateDeferFlags(variables);
        _collectTelemetry = requestContext.CollectOperationPlanTelemetry();
        _clientScope = requestContext.CreateClientScope();

        _resultStore.Initialize(
            requestContext.Schema,
            _errorHandler,
            operationPlan.Operation,
            requestContext.ErrorHandlingMode(),
            IncludeFlags,
            DeferFlags,
            requestContext.Schema.GetOptions().PathSegmentLocalPoolCapacity);

        _executionState.Initialize(_collectTelemetry, cancellationTokenSource);

        EnsureNodeArrayCapacity(operationPlan.MaxNodeId);
    }

    /// <summary>
    /// Cleans the context for return to the pool.
    /// Releases per-request state while retaining reusable buffers.
    /// </summary>
    internal void Clean()
    {
        DisposeNodeState();

        if (_nodeSlotCapacity > 0)
        {
            Array.Clear(_nodesToComplete, 0, _nodeSlotCapacity);
            Array.Clear(_schemaNames, 0, _nodeSlotCapacity);
            Array.Clear(_skippedDefinitions, 0, _nodeSlotCapacity);
            Array.Clear(_variableValueSets, 0, _nodeSlotCapacity);
            Array.Clear(_transportUris, 0, _nodeSlotCapacity);
            Array.Clear(_transportContentTypes, 0, _nodeSlotCapacity);
        }

        _resultStore.Clean(256, 256);
        _executionState.Clean();

        RequestContext = default!;
        Variables = default!;
        OperationPlan = default!;
        DeferFlags = 0;
        _clientScope = default!;
        _requirementValues = default;
        _requirementKeys = null;
        Traces =
#if NET10_0_OR_GREATER
            [];
#else
            ImmutableDictionary<int, ExecutionNodeTrace>.Empty;
#endif
        _traceId = null;
        _start = 0;
    }

    /// <summary>
    /// Permanently destroys the context and its owned resources.
    /// Called when the pool drops a context (pool full) or during pool disposal.
    /// </summary>
    internal void Destroy()
    {
        _resultStore.Dispose();
        _executionState.Destroy();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await _clientScope.DisposeAsync();

        var pool = _pool;
        _pool = null;
        pool?.Return(this);
    }

    private void EnsureNodeArrayCapacity(int maxNodeId)
    {
        var nodeSlotCount = maxNodeId + 1;
        _dependentBitsetWordCount = (maxNodeId >> 6) + 1;

        if (nodeSlotCount > _nodeSlotCapacity)
        {
            _nodesToComplete = new NodeCompletionSet?[nodeSlotCount];
            _schemaNames = new string?[nodeSlotCount];
            _skippedDefinitions = new List<IOperationPlanNode>?[nodeSlotCount];
            _variableValueSets = new ImmutableArray<VariableValues>[nodeSlotCount];
            _transportUris = new Uri?[nodeSlotCount];
            _transportContentTypes = new string?[nodeSlotCount];
            _nodeSlotCapacity = nodeSlotCount;
        }
    }
}
