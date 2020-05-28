using System;
using System.Threading;
using System.Threading.Channels;

namespace HotChocolate.Execution.Utilities
{
    internal class TaskStatistics : ITaskStatistics
    {
        private int _allTasks;
        private int _newTasks;
        private int _runningTasks;
        private int _completedTasks;

        public event EventHandler<EventArgs>? StateChanged;

        public int AllTasks => _allTasks;

        public int NewTasks => _newTasks;

        public int RunningTasks => _runningTasks;

        public int CompletedTasks => _completedTasks;

        public bool IsCompleted => _allTasks == _completedTasks;

        public Channel<ResolverTask> Work { get; private set; } =
            Channel.CreateUnbounded<ResolverTask>();

        public void DoWork(ResolverTask task)
        {
            Interlocked.Increment(ref _allTasks);
            Work.Writer.TryWrite(task);
        }

        public void TaskCreated()
        {
            Interlocked.Increment(ref _allTasks);
        }

        public void TaskStarted()
        {
            Interlocked.Increment(ref _runningTasks);
            Interlocked.Decrement(ref _newTasks);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void TaskCompleted()
        {
            Interlocked.Increment(ref _completedTasks);
            if (IsCompleted)
            {
                Work.Writer.TryComplete();
            }
        }

        public void Clear()
        {
            _newTasks = 0;
            _runningTasks = 0;
            Work = Channel.CreateUnbounded<ResolverTask>();
        }
    }
}
