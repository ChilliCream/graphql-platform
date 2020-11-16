using System;
using HotChocolate.Execution;

namespace HotChocolate.Fetching
{
    public interface IBatchDispatcher
    {
        event EventHandler? TaskEnqueued;

        bool HasTasks { get; }

        void Dispatch(Action<IExecutionTaskDefinition> enqueue);
    }
}
