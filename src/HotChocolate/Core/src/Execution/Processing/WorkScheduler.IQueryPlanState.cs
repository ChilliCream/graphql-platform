using System.Collections.Generic;
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
            var task = tasks[i];
            _stateMachine.TryInitializeTask(task);
            task.IsRegistered = true;

            if (_stateMachine.RegisterTask(task))
            {
                var work = task.IsSerial ? _serial : _work;
                work.Push(task);
            }
            else
            {
                _suspended.Enqueue(task);
            }
        }
    }
}
