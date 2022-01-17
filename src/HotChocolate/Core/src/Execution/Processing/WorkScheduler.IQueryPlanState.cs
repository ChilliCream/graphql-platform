using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HotChocolate.Execution.Processing.Internal;
using HotChocolate.Execution.Processing.Plan;

namespace HotChocolate.Execution.Processing;

internal partial class WorkScheduler : IQueryPlanState
{
    IOperationContext IQueryPlanState.Context => _operationContext;

    ISet<int> IQueryPlanState.Selections => _selections;

    void IQueryPlanState.RegisterUnsafe(IReadOnlyList<IExecutionTask> tasks)
    {
        for (var i = 0; i < tasks.Count; i++)
        {
            IExecutionTask task = tasks[i];
            _stateMachine.TryInitializeTask(task);
            task.IsRegistered = true;

            if (_stateMachine.RegisterTask(task))
            {
                WorkQueue work = task.IsSerial ? _serial : _work;
                work.Push(task);
            }
            else
            {
                _suspended.Enqueue(task);
            }
        }
    }

    void IQueryPlanState.OnResolverCompleted(
        ISelection selection,
        Path path,
        ExecutionTaskStatus status,
        object? result)
    {
        if (selection is null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (_subscriptions.Count > 0)
        {
            var resolverResult = new ResolverResult(selection, path, status, result);
            foreach (ResolverResultSubscription subscription in _subscriptions)
            {
                subscription.Publish(resolverResult);
            }
        }
    }

    IDisposable IObservable<ResolverResult>.Subscribe(
        IObserver<ResolverResult> observer)
    {
        if (observer is null)
        {
            throw new ArgumentNullException(nameof(observer));
        }

        var subscription = new ResolverResultSubscription(observer);
        _subscriptions.Add(subscription);
        return subscription;
    }

    private sealed class ResolverResultSubscription : IDisposable
    {
        private readonly IObserver<ResolverResult> _observer;
        private bool _disposed;

        public ResolverResultSubscription(
            IObserver<ResolverResult> observer)
        {
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        }

        public void Publish(ResolverResult result)
        {
            if (_disposed)
            {
                return;
            }

            _observer.OnNext(result);
        }

        public void Complete()
        {
            if (_disposed)
            {
                return;
            }

            _observer.OnCompleted();
        }

        public void Dispose() => _disposed = true;
    }
}
