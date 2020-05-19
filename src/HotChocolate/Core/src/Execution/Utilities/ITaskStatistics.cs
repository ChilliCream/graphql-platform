using System;

namespace HotChocolate.Execution.Utilities
{
    internal interface ITaskStatistics
    {
        event EventHandler<EventArgs> StateChanged;

        int Enqueued { get; }

        int Running { get; }

        void TaskEnqueued();

        void TaskDequeued();

        void TaskStarted();

        void TaskCompleted();
    }
}
