using System;
using System.Threading;

namespace HotChocolate.Execution
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
