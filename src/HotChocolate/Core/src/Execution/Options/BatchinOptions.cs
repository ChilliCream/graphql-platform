using System;

namespace HotChocolate.Execution.Options
{
    /// <summary>
    /// Represents the settings to configure the behavior of the batching.
    /// </summary>
    public class BatchingOptions : IBatchingOptionsAccessor
    {
        /// <summary>
        /// The amount of time in milliseconds that we wait before starting a batch.
        /// The default value is <c>10</c> milliseconds.
        /// A batch can be started sooner if there is no more work in progress.
        /// A batch can be started later if there is a heavy load on the machine.
        /// </summary>
        public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromMilliseconds(10);

        /// <summary>If set allow running the experimental but more optimized batching code</summary>
        [Obsolete("This option will be removed in the next major release")]
        public bool AllowExperimental { get; set; } = true; // TODO: default to false after testing
    }
}
