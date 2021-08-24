using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing.Internal;

namespace HotChocolate.Execution.Processing
{
    internal class DeferredWorkBacklog : IDeferredWorkBacklog
    {
        private readonly DeferredWorkQueue _queue = new();

        /// <inheritdoc />
        public bool HasWork => !_queue.IsEmpty;

        /// <inheritdoc />
        public bool TryTake([NotNullWhen(true)] out IDeferredExecutionTask? executionTask) =>
            _queue.TryDequeue(out executionTask);

        /// <inheritdoc />
        public void Register(IDeferredExecutionTask executionTask) =>
            _queue.Enqueue(executionTask);

        public void Clear() => _queue.ClearUnsafe();
    }
}
