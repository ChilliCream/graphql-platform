using System.Threading;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Options;
using HotChocolate.Fetching;
using Xunit;
using System;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Batching
{
    public class ContextBatchDispatcherTests
    {
        [Fact]
        public void Basic()
        {
            var dispatcher = new ContextBatchDispatcher(
                new BatchScheduler(),
                new BatchingOptions());
            var context1 = new Processing.ExecutionContext(default!, default!);
            Assert.Throws<ArgumentNullException>(() => dispatcher.Register(default!, CancellationToken.None));
            dispatcher.Register(context1, CancellationToken.None);
            Assert.Throws<ArgumentException>(() => dispatcher.Register(context1, CancellationToken.None));
            dispatcher.Unregister(context1);
            Assert.Throws<ArgumentException>(() => dispatcher.Unregister(context1));
            Assert.Throws<ArgumentNullException>(() => dispatcher.Unregister(default!));
        }

        [Fact]
        public void Noop()
        {
            var dispatcher = NoopContextBatchDispatcher.Default;
            Assert.Equal(TaskScheduler.Current, dispatcher.TaskScheduler);
            // ensure that no exceptions are thrown
            dispatcher.Register(null, CancellationToken.None);
            dispatcher.Unregister(null);
            dispatcher.Suspend();
            dispatcher.Resume();
        }
    }
}
