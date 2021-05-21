using System;
using HotChocolate.Fetching;

namespace HotChocolate.Execution.Processing
{
    internal class NoopBatchDispatcher : IBatchDispatcher
    {
        public event EventHandler? TaskEnqueued;

        public bool HasTasks => false;

        public bool DispatchOnSchedule { get; set; } = false;

        public void Dispatch(Action<IExecutionTaskDefinition> enqueue) { }

        public static NoopBatchDispatcher Default { get; } = new();
    }
}
