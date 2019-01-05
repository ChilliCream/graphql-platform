namespace HotChocolate.Execution.Configuration
{
    /// <summary>
    /// Represents a dedicated options accessor to read the tracing
    /// configuration.
    /// </summary>
    public interface ITracingOptionsAccessor
    {
        /// <summary>
        /// Gets a value indicating whether tracing for performance measurement
        /// of query requests is enabled. The default value is
        /// <see langword="false"/>.
        /// </summary>
        bool EnableTracing { get; }
    }
}
