using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Execution.Batching
{
    public class NoopContextBatchDispatcherTests
    {
        [Fact]
        public void Basic()
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
