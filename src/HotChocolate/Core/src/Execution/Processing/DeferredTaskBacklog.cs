using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Channels;

namespace HotChocolate.Execution.Processing
{
    internal class DeferredTaskBacklog
        : IDeferredTaskBacklog
    {
        private UnsortedChannel<IDeferredExecutionTask> _channel =
            new UnsortedChannel<IDeferredExecutionTask>(true);

        public bool IsEmpty => _channel.IsEmpty;

        public bool TryTake([NotNullWhen(true)] out IDeferredExecutionTask? task) =>
            _channel.Reader.TryRead(out task);

        public void Register(IDeferredExecutionTask deferredTask) =>
            _channel.Writer.TryWrite(deferredTask);

        public void Clear() =>
            _channel = new UnsortedChannel<IDeferredExecutionTask>(true);
    }
}
