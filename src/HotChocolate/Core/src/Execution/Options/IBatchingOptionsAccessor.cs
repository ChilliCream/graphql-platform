using System;

namespace HotChocolate.Execution.Options
{
    /// <summary>
    /// Represents a dedicated options accessor to read the configured batching options.
    /// </summary>
    public interface IBatchingOptionsAccessor
    {
        /// <summary>
        /// The amount of time in milliseconds that we wait before starting a batched dataload.
        /// The default value is <c>10</c> milliseconds.
        /// A dataload can be started sooner if there is no more work in progress.
        /// A dataload can be started later if there is a heavy load on the machine.
        /// </summary>
        TimeSpan BatchTimeout { get; }

        /// <summary>If set allow running the experimental but more optimized batching code</summary>
        [Obsolete("This option will be removed in the next major release")]
        bool AllowExperimental { get; }
    }
}
