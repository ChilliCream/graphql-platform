using System;

namespace HotChocolate.Fetching
{
    public interface IBatchDispatcher
    {
        bool HasTasks { get; }

        event EventHandler? TaskEnqueued;

        void Dispatch();
    }
}
