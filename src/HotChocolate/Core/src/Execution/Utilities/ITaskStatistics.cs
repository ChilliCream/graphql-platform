using System;

namespace HotChocolate.Execution.Utilities
{
    internal interface ITaskStatistics
    {
        event EventHandler<EventArgs> StateChanged;

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
