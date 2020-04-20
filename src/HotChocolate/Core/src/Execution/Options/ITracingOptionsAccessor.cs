namespace HotChocolate.Execution.Options
{
    /// <summary>
    /// Represents a dedicated options accessor to read the instrumentation
    /// configuration.
    /// </summary>
    public interface IInstrumentationOptionsAccessor
    {
        /// <summary>
        /// Gets a value indicating whether tracing for performance measurement
        /// of query requests is enabled. The default value is
        /// <see cref="TracingPreference.Never"/>.
        /// </summary>
        TracingPreference TracingPreference { get; }
    }
}
