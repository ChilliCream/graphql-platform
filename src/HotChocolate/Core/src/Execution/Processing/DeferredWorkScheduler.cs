using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.DependencyInjection;

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

    private DeferredWorkState StateOwner
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

            return _stateOwner.State;
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
        task.Begin(taskContextOwner, StateOwner.CreateId());
    }

    public void Complete(DeferredExecutionTaskResult result)
        => StateOwner.Complete(result);

    public IAsyncEnumerable<IQueryResult> CreateResultStream(IQueryResult initialResult)
        => new DeferResultStream(initialResult, StateOwner);

    private class DeferResultStream : IAsyncEnumerable<IQueryResult>
    {
        private readonly IQueryResult _initialResult;
        private readonly DeferredWorkState _state;

        public DeferResultStream(IQueryResult initialResult, DeferredWorkState state)
        {
            _initialResult = initialResult;
            _state = state;
        }

        public async IAsyncEnumerator<IQueryResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            var success = true;

            yield return _initialResult;

            while (success && !cancellationToken.IsCancellationRequested)
            {
                var result = await _state.TryDequeueResultAsync(cancellationToken);

                if (result is not null)
                {
                    success = true;
                    yield return result;
                }
            }
        }
    }
}
