namespace Mocha;

/// <summary>
/// Options for configuring the concurrency limiter middleware that restricts the number of messages processed in parallel.
/// </summary>
public class ConcurrencyLimiterOptions
{
    /// <summary>
    /// Gets or sets whether the concurrency limiter is enabled.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of messages that can be processed concurrently.
    /// </summary>
    public int? MaxConcurrency { get; set; }

    /// <summary>
    /// Provides the default values for concurrency limiter options.
    /// </summary>
    public static class Defaults
    {
        /// <summary>
        /// The default maximum concurrency, set to the number of available processors.
        /// </summary>
        public static int MaxConcurrency = Environment.ProcessorCount;
    }
}
