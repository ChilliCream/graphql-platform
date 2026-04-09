namespace Mocha.Testing;

/// <summary>
/// Provides read-only access to tracked messages grouped by lifecycle phase.
/// </summary>
public interface ITrackedMessages
{
    /// <summary>
    /// Gets the messages that were dispatched (published or sent).
    /// </summary>
    IReadOnlyList<TrackedMessage> Dispatched { get; }

    /// <summary>
    /// Gets the messages that were successfully consumed.
    /// </summary>
    IReadOnlyList<TrackedMessage> Consumed { get; }

    /// <summary>
    /// Gets the messages that failed during consumption.
    /// </summary>
    IReadOnlyList<TrackedMessage> Failed { get; }
}
