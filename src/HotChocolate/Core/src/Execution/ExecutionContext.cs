using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;

namespace HotChocolate.Execution
{
    internal partial class ExecutionContext
        : IExecutionContext
    {
        private readonly ManualResetEvent _waitHandle = new ManualResetEvent(true);

        public ITaskQueue Tasks => _taskQueue;

        public ITaskStatistics TaskStats => _taskStatistics;

        public bool IsCompleted => TaskStats.IsCompleted;

        public ObjectPool<ResolverTask> TaskPool { get; }

        public IBatchDispatcher BatchDispatcher { get; private set; } = default!;

        public Task WaitAsync(CancellationToken cancellationToken) =>
            _waitHandle.FromWaitHandle(cancellationToken);

        private void SetEngineState()
        {
            if (TaskStats.NewTasks == 0 && !BatchDispatcher.HasTasks && !TaskStats.IsCompleted)
            {
                _waitHandle.Reset();
            }
            else if (TaskStats.NewTasks == 0 && BatchDispatcher.HasTasks)
            {
                BatchDispatcher.Dispatch();
            }
            else
            {
                _waitHandle.Set();
            }
        }

        private void BatchDispatcherEventHandler(
            object? source, EventArgs args) =>
            SetEngineState();

        private void TaskStatisticsEventHandler(
            object? source, EventArgs args) =>
            SetEngineState();
    }
}
