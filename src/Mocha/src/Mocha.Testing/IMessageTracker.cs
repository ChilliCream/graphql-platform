namespace Mocha.Testing;

/// <summary>
/// Tracks messages flowing through the messaging pipeline and provides
/// completion detection and diagnostic output.
/// </summary>
public interface IMessageTracker : ITrackedMessages
{
    /// <summary>
    /// Waits for all dispatched messages to be consumed or to fail.
    /// </summary>
    /// <param name="timeout">
    /// The maximum time to wait. Defaults to 30 seconds if not specified.
    /// </param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A result containing all tracked messages and completion status.</returns>
    Task<MessageTrackingResult> WaitForCompletionAsync(
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    /// <summary>
    /// Waits for a message of the specified type to be consumed.
    /// </summary>
    /// <typeparam name="T">The message type to wait for.</typeparam>
    /// <param name="timeout">
    /// The maximum time to wait. Defaults to 30 seconds if not specified.
    /// </param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The consumed message payload.</returns>
    Task<T> WaitForConsumed<T>(
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the ordered timeline of all tracked events.
    /// </summary>
    IReadOnlyList<TrackedEvent> Timeline { get; }

    /// <summary>
    /// Returns a human-readable diagnostic string describing all tracked activity.
    /// </summary>
    string ToDiagnosticString();

    /// <summary>
    /// Configures a stub response for messages of the specified type.
    /// </summary>
    /// <typeparam name="T">The message type to stub.</typeparam>
    /// <returns>A builder for configuring the stub response.</returns>
    IMessageStubBuilder<T> WhenSent<T>();
}
