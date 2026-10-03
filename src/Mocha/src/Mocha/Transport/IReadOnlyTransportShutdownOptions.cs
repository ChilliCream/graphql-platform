namespace Mocha;

/// <summary>
/// Provides read-only access to cancellation grace and transport cleanup timeouts.
/// </summary>
public interface IReadOnlyTransportShutdownOptions
{
    /// <summary>
    /// Gets how long cancelled in-flight messages have to return before the stop completes without them.
    /// </summary>
    TimeSpan CancellationGracePeriod { get; }

    /// <summary>
    /// Gets the maximum time each transport cleanup operation of a stop may take, such as returning
    /// messages, closing channels, or removing temporary resources.
    /// </summary>
    TimeSpan CleanupTimeout { get; }
}
