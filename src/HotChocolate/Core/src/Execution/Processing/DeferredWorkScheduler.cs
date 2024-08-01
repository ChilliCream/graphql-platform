using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.OperationResultBuilder;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a backlog for deferred work.
/// </summary>
internal sealed class DeferredWorkScheduler
{
    private readonly object _stateSync = new();
    private IFactory<OperationContextOwner> _operationContextFactory = default!;
    private IFactory<DeferredWorkStateOwner> _deferredWorkStateFactory = default!;
    private OperationContext _parentContext = default!;
    private DeferredWorkStateOwner? _stateOwner;

    private DeferredWorkStateOwner StateOwner
    {
        get
        {
            if (_stateOwner is null)
            {
                lock (_stateSync)
                {
                    _stateOwner ??= _deferredWorkStateFactory.Create();
                }
            }

            return _stateOwner;
        }
    }

    /// <summary>
    /// Specifies if there was deferred work enqueued.
    /// </summary>
    public bool HasResults => _stateOwner?.State.HasResults is true;

    public void Initialize(OperationContext operationContext)
    {
        var services = operationContext.Services;

        _stateOwner = null;
        _parentContext = operationContext;
        _operationContextFactory = services.GetRequiredService<IFactory<OperationContextOwner>>();
        _deferredWorkStateFactory = services.GetRequiredService<IFactory<DeferredWorkStateOwner>>();
    }

    public void InitializeFrom(OperationContext operationContext, DeferredWorkScheduler scheduler)
    {
        _stateOwner = scheduler.StateOwner;
        _parentContext = operationContext;
        _operationContextFactory = scheduler._operationContextFactory;
        _deferredWorkStateFactory = scheduler._deferredWorkStateFactory;
    }

    /// <summary>
    /// Registers deferred work
    /// </summary>
    public void Register(DeferredExecutionTask task, ResultData parentResult)
    {
        // first we get the result identifier which is used to refer to the result that we defer.
        var resultId = StateOwner.State.CreateId();

        // next we assign a patch identifier to the result set into which the deferred result
        // shall be patched into.
        var patchId = StateOwner.State.AssignPatchId(parentResult);

        // for the spawned execution we need a operation context which we will initialize
        // from the current operation context.
        var taskContextOwner = _operationContextFactory.Create();
        taskContextOwner.OperationContext.InitializeFrom(_parentContext);

        // Last we register our patch identifier with the parent result so that
        // we can more efficiently mark discarded result sets to not send down
        // patches that cannot be applied.
        _parentContext.Result.AddPatchId(patchId);

        // with all in place we will start the execution of the deferred task.
        task.Begin(taskContextOwner, resultId, patchId);
    }

    public void Register(DeferredExecutionTask task, uint patchId)
    {
        var resultId = StateOwner.State.CreateId();
        var taskContextOwner = _operationContextFactory.Create();
        taskContextOwner.OperationContext.InitializeFrom(_parentContext);
        task.Begin(taskContextOwner, resultId, patchId);
    }

    public void Complete(DeferredExecutionTaskResult result)
        => StateOwner.State.Complete(result);

    public IAsyncEnumerable<IOperationResult> CreateResultStream(IOperationResult initialResult)
        => new DeferredResultStream(
            initialResult,
            StateOwner,
            _parentContext.Operation,
            _parentContext.DiagnosticEvents);

    public void Clear()
    {
        _stateOwner = null;
        _operationContextFactory = default!;
        _deferredWorkStateFactory = default!;
        _parentContext = default!;
    }

    private class DeferredResultStream : IAsyncEnumerable<IOperationResult>
    {
        private readonly IOperationResult _initialResult;
        private readonly DeferredWorkStateOwner _stateOwner;
        private readonly IOperation _operation;
        private readonly IExecutionDiagnosticEvents _diagnosticEvents;

        public DeferredResultStream(
            IOperationResult initialResult,
            DeferredWorkStateOwner stateOwner,
            IOperation operation,
            IExecutionDiagnosticEvents diagnosticEvents)
        {
            _initialResult = FromResult(initialResult).SetHasNext(true).Build();
            _stateOwner = stateOwner;
            _operation = operation;
            _diagnosticEvents = diagnosticEvents;
        }

        public async IAsyncEnumerator<IOperationResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            var span = _diagnosticEvents.ExecuteStream(_operation);
            var state = _stateOwner.State;
            var hasNext = true;
            var completed = false;

            try
            {
                yield return _initialResult;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await state
                        .TryDequeueResultsAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (result is not null)
                    {
                        hasNext = result.HasNext ?? false;
                        yield return result;
                    }
                    else if (state.IsCompleted)
                    {
                        if (hasNext)
                        {
                            yield return new OperationResult(null, hasNext: false);
                        }

                        yield break;
                    }
                }

                completed = !cancellationToken.IsCancellationRequested;
            }
            finally
            {
                span.Dispose();
            }

            // we only return the state back to its pool if the operation was not cancelled
            // or otherwise faulted.
            if (completed)
            {
                _stateOwner.Dispose();
            }
        }
    }
}
