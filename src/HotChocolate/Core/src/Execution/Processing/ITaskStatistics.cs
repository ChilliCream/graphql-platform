using System;

namespace HotChocolate.Execution.Processing
{
    internal interface ITaskStatistics
    {
        event EventHandler<EventArgs> StateChanged;

        event EventHandler<EventArgs>? AllTasksCompleted;

        /// <summary>
        /// Gets the amount of new tasks that are ready to be processed.
        /// </summary>
        /// <value></value>
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
