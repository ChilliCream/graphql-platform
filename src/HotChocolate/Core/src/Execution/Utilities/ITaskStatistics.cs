using System;
using System.Threading.Channels;

namespace HotChocolate.Execution.Utilities
{
    internal interface ITaskStatistics
    {
        event EventHandler<EventArgs> StateChanged;

        Channel<ResolverTask> Work { get; }

        void DoWork(ResolverTask task);

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
