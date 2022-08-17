using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.QueryResultBuilder;

namespace HotChocolate.Execution.Processing;

internal sealed class DeferredWorkScheduler : IDeferredWorkScheduler
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
        var services = operationContext.Services;

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

    public void Register(DeferredExecutionTask task)
    {
        var resultId = StateOwner.State.CreateId();
        var taskContextOwner = _operationContextFactory.Create();
        taskContextOwner.OperationContext.InitializeFrom(_parentContext);
        task.Begin(taskContextOwner, resultId);
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
            var hasNext = true;

            try
            {

                yield return _initialResult;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await _stateOwner.State.TryDequeueResultAsync(cancellationToken);
                    if (result is not null)
                    {
                        hasNext = result.HasNext ?? false;
                        yield return result;
                    }
                    else
                    {
                        if (hasNext)
                        {
                            yield return new QueryResult(null, hasNext: false);
                        }
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
