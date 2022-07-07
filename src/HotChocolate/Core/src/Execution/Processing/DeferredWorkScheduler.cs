using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Instrumentation;
using static HotChocolate.Execution.QueryResultBuilder;

namespace HotChocolate.Execution.Processing;

internal sealed class DeferredWorkScheduler : IDeferredWorkScheduler
{
    private readonly IFactory<OperationContextOwner> _operationContextFactory;
    private readonly IFactory<DeferredWorkStateOwner> _deferredWorkStateFactory;
    private readonly object _stateSync = new();
    private OperationContext _parentContext = default!;
    private DeferredWorkStateOwner? _stateOwner;

    public DeferredWorkScheduler(
        IFactory<OperationContextOwner> operationContextFactory,
        IFactory<DeferredWorkStateOwner> deferredWorkStateFactory)
    {
        _operationContextFactory = operationContextFactory;
        _deferredWorkStateFactory = deferredWorkStateFactory;
    }

    private DeferredWorkStateOwner StateOwner
    {
        get
        {
            if (_stateOwner is null)
            {
                lock (_stateSync)
                {
                    if (_stateOwner is null)
                    {
                        _stateOwner = _deferredWorkStateFactory.Create();
                    }
                }
            }

            return _stateOwner;
        }
    }

    public bool HasResults => _stateOwner?.State.HasResults is true;

    public void Initialize(OperationContext operationContext)
    {
        _parentContext = operationContext;
    }

    public void InitializeFrom(OperationContext operationContext, DeferredWorkScheduler scheduler)
    {
        _parentContext = operationContext;
        _stateOwner = scheduler._stateOwner;
    }

    public void Register(DeferredExecutionTask task)
    {
        var taskContextOwner = _operationContextFactory.Create();
        taskContextOwner.OperationContext.InitializeFrom(_parentContext);
        task.Begin(taskContextOwner, StateOwner.State.CreateId());
    }

    public void Complete(DeferredExecutionTaskResult result)
        => StateOwner.State.Complete(result);

    public IAsyncEnumerable<IQueryResult> CreateResultStream(IQueryResult initialResult)
        => new DeferResultStream(
            initialResult,
            StateOwner,
            _parentContext.Operation,
            _parentContext.DiagnosticEvents);

    private class DeferResultStream : IAsyncEnumerable<IQueryResult>
    {
        private readonly IQueryResult _initialResult;
        private readonly DeferredWorkStateOwner _stateOwner;
        private readonly IOperation _operation;
        private readonly IExecutionDiagnosticEvents _diagnosticEvents;

        public DeferResultStream(
            IQueryResult initialResult,
            DeferredWorkStateOwner stateOwner,
            IOperation operation,
            IExecutionDiagnosticEvents diagnosticEvents)
        {
            _initialResult = FromResult(initialResult).SetHasNext(true).Create();
            _stateOwner = stateOwner;
            _operation = operation;
            _diagnosticEvents = diagnosticEvents;
        }

        public async IAsyncEnumerator<IQueryResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            var span = _diagnosticEvents.ExecuteStream(_operation);

            try
            {

                yield return _initialResult;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await _stateOwner.State.TryDequeueResultAsync(cancellationToken);
                    if (result is not null)
                    {
                        yield return result;
                    }
                    else
                    {

                        yield break;
                    }
                }
            }
            finally
            {
                _stateOwner.Dispose();
                span.Dispose();
            }
        }
    }
}
