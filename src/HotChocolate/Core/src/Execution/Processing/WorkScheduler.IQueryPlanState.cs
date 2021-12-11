using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
}
