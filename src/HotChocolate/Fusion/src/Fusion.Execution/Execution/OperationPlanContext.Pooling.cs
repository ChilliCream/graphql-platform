using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Results;

namespace HotChocolate.Fusion.Execution;

public sealed partial class OperationPlanContext
{
    private CancellationTokenSource _engineCancellationSource = new();

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
        IOperationPlan operationPlan,
        CancellationTokenSource cancellationTokenSource,
        MemoryArena? memory = null)
    {
        _activeNodeSlotCount = 0;
        _usesDynamicSchemaNames = true;
        _usesBatchNodes = true;

        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(operationPlan);

        _disposed = 0;
        RequestContext = requestContext;

        _memory = memory
            ?? requestContext.Memory
            ?? throw new InvalidOperationException(
                "The operation plan context requires a memory arena.");
        _memorySource.Set(_memory);
        _currentMemorySource = _memorySource;

        Variables = variables;
        OperationPlan = operationPlan;

        switch (operationPlan)
        {
            case OperationPlan plan:
                _usesDynamicSchemaNames = plan.UsesDynamicSchemaNames;
                _usesBatchNodes = plan.UsesBatchNodes;
                break;

            case IncrementalPlan plan:
                _usesDynamicSchemaNames = plan.UsesDynamicSchemaNames;
                _usesBatchNodes = plan.UsesBatchNodes;
                break;
        }

        IncludeFlags = operationPlan.Operation.CreateIncludeFlags(variables);
        DeferFlags = operationPlan.Operation.CreateDeferFlags(variables);
        _collectTelemetry = requestContext.CollectOperationPlanTelemetry();
        _clientScope ??= requestContext.CreateClientScope();
        _clientScopeCreatedAt = Stopwatch.GetTimestamp();

        _resultStore.Initialize(
            Memory,
            requestContext.Schema,
            _errorHandler,
            operationPlan.Operation,
            requestContext.ErrorHandlingMode(),
            IncludeFlags,
            DeferFlags,
            requestContext.Schema.GetOptions().PathSegmentLocalPoolCapacity);

        _executionState.Initialize(_collectTelemetry, cancellationTokenSource);

        var maxNodeId = operationPlan.MaxNodeId;
        EnsureNodeArrayCapacity(maxNodeId);
        _activeNodeSlotCount = maxNodeId + 1;
    }

    /// <summary>
    /// Arms the pooled engine cancellation source against the request token and returns it for
    /// this operation. The engine source halts the execution engine without cancelling the request
    /// pipeline. The returned registration links the request token into the engine source so that
    /// client-abort and server-shutdown still propagate, and it must be disposed before the source
    /// is returned for reuse.
    /// </summary>
    internal (CancellationTokenSource Source, CancellationTokenRegistration Registration) RentEngineCancellation(
        CancellationToken cancellationToken)
    {
        var registration = cancellationToken.UnsafeRegister(
            static state => Unsafe.As<CancellationTokenSource>(state)!.Cancel(),
            _engineCancellationSource);
        return (_engineCancellationSource, registration);
    }

    /// <summary>
    /// Returns the engine cancellation source for reuse. If it was cancelled and cannot be reset,
    /// it is disposed and replaced with a fresh source. The caller must dispose the registration
    /// from <see cref="RentEngineCancellation"/> before calling this.
    /// </summary>
    internal void ReturnEngineCancellation()
    {
        if (!_engineCancellationSource.TryReset())
        {
            _engineCancellationSource.Dispose();
            _engineCancellationSource = new CancellationTokenSource();
        }
    }

    /// <summary>
    /// Cleans the context for return to the pool.
    /// Releases per-request state while retaining reusable buffers.
    /// </summary>
    internal void Clean()
    {
        var activeNodeSlotCount = _activeNodeSlotCount;

        if (activeNodeSlotCount > 0)
        {
            DisposeNodeState(activeNodeSlotCount);
            Array.Clear(_nodesToComplete, 0, activeNodeSlotCount);
            Array.Clear(_dynamicVariableValueSets, 0, activeNodeSlotCount);

            if (_usesDynamicSchemaNames)
            {
                Array.Clear(_schemaNames, 0, activeNodeSlotCount);
            }

            if (_usesBatchNodes)
            {
                Array.Clear(_skippedDefinitions, 0, activeNodeSlotCount);
                Array.Clear(_batchRequestErrors, 0, activeNodeSlotCount);
            }

            if (_collectTelemetry)
            {
                Array.Clear(_variableValueSets, 0, activeNodeSlotCount);
                Array.Clear(_transportUris, 0, activeNodeSlotCount);
                Array.Clear(_transportContentTypes, 0, activeNodeSlotCount);
            }
        }

        _activeNodeSlotCount = 0;
        _usesDynamicSchemaNames = true;
        _usesBatchNodes = true;

        _resultStore.Clean(256, 256);
        _executionState.Clean();

        RequestContext = default!;
        _memory = null;
        _memorySource.Clear();
        _currentMemorySource = null!;
        Variables = default!;
        OperationPlan = default!;
        DeferFlags = 0;
        // if a custom scope is used we cannot reuse it and have to null it.
        if (_clientScope is not DefaultSourceSchemaClientScope)
        {
            _clientScope = default!;
        }
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
        _clientScopeCreatedAt = 0;
    }

    /// <summary>
    /// Permanently destroys the context and its owned resources.
    /// Called when the pool drops a context (pool full) or during pool disposal.
    /// </summary>
    internal void Destroy()
    {
        _resultStore.Dispose();
        _executionState.Destroy();
        _engineCancellationSource.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        // If Initialize fails before creating a scope, _clientScope can be null.
        if (_clientScope is DefaultSourceSchemaClientScope reusableClientScope)
        {
            await reusableClientScope.ResetAsync();
        }
        else if (_clientScope is not null)
        {
            await _clientScope.DisposeAsync();
        }

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
            _batchRequestErrors = new Dictionary<int, Exception>?[nodeSlotCount];
            _variableValueSets = new ImmutableArray<VariableValues>[nodeSlotCount];
            _dynamicVariableValueSets = new ImmutableArray<VariableValues>[nodeSlotCount];
            _transportUris = new Uri?[nodeSlotCount];
            _transportContentTypes = new string?[nodeSlotCount];
            _nodeSlotCapacity = nodeSlotCount;
        }
    }
}
