using System;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Utilities
{
    internal interface ICompletionQueue
    {
        event EventHandler? TaskEnqueued;

        void Enqueue(ResolverTask task);

        bool TryDequeue([NotNullWhen(true)]out ResolverTask? task);
    }
}