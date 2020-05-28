using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Utilities
{
    internal interface ITaskStatistics
    {
        event EventHandler<EventArgs> StateChanged;

        ConcurrentBag<ResolverTask> Work { get; }
        bool IsDone { get; }

        void DoWork(ResolverTask task);

        void DoProcessing(ValueTask task);

        int NewTasks { get; }

        int RunningTasks { get; }

        int AllTasks { get; }

        int CompletedTasks { get; }

        bool IsCompleted { get; }

        void TaskCreated();

        void TaskStarted();

        void TaskCompleted();
    }
}
