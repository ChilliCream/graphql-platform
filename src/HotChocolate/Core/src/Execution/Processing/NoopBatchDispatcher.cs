using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Fetching;

namespace HotChocolate.Execution.Processing
{
    internal class NoopBatchDispatcher : IBatchDispatcher
    {
        private static readonly Task<BatchDispatcherResult> _success =
            Task.FromResult(BatchDispatcherResult.Success);

        public event EventHandler? TaskEnqueued;

        public bool HasTasks => false;

        public bool DispatchOnSchedule { get; set; } = false;

        public Task<BatchDispatcherResult> DispatchAsync(CancellationToken cancellationToken)
            => _success;

        public static NoopBatchDispatcher Default { get; } = new();
    }
}
