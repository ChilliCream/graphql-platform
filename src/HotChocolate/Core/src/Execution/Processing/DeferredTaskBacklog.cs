using System;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Processing
{
    internal class DeferredTaskBacklog
        : IDeferredTaskBacklog
    {
        /*
        private UnsortedChannel<IDeferredExecutionTask> _channel =
            new UnsortedChannel<IDeferredExecutionTask>(true);
            */

        public bool IsEmpty => true;
        // _channel.IsEmpty;

        public bool TryTake([NotNullWhen(true)] out IDeferredExecutionTask? task) =>
            throw new NotImplementedException();
        //_channel.Reader.TryRead(out task);

        public void Register(IDeferredExecutionTask deferredTask) =>
            throw new NotImplementedException();
        //_channel.Writer.TryWrite(deferredTask);

        public void Clear() { }
        //_channel = new UnsortedChannel<IDeferredExecutionTask>(true);
    }
}
