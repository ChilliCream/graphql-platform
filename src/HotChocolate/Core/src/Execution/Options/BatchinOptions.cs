using System;

namespace HotChocolate.Execution.Options
{
    /// <summary>
    /// Represents the settings to configure the behavior of the batching.
    /// </summary>
    public class BatchingOptions : IBatchingOptionsAccessor
    {
        /// <summary>
        /// If set to 0 (default), batched dataloads will trigger as soon as all internal tasks have been scheduled.
        /// If set to a positive value, batched dataloads will be put on hold after that.
        /// They wil trigger either when the timeout expires, or when all scheduled tasks have completed
        /// or are idle (async implementation awaiting trigger).
        /// The timeout is counted from the moment the last internal task has been scheduled.
        /// Not from the request of a batched value from the dataloader.
        /// </summary>
        /// <remarks>
        /// Setting this option to a non 0 value will incur a small performance penalty for keeping track of all tasks.
        /// However it has the benefit that your batches will be as large as possible.
        /// If your entire graph implementation is async, you can set this to the same value as
        /// RequestExecutorOptions.ExecutionTimeout to ensure maximum benefit from batching operations.
        /// </remarks>
        public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromMilliseconds(0);
    }
}
