namespace Mocha.Testing.Internal;

/// <summary>
/// Tracks the lifecycle of a single message envelope through dispatch, receive, and consume phases.
/// Uses an atomic bitfield for lock-free, allocation-free thread safety.
/// </summary>
internal sealed class EnvelopeTracker
{
    private int _state;

    /// <summary>
    /// Gets the composite key for this envelope (MessageId + "|" + DestinationAddress).
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvelopeTracker"/> class.
    /// </summary>
    /// <param name="key">The composite key for this envelope.</param>
    public EnvelopeTracker(string key) => Key = key;

    /// <summary>
    /// Records a lifecycle event for this envelope.
    /// </summary>
    /// <param name="kind">The kind of event that occurred.</param>
    public void Record(TrackedEventKind kind)
        => Interlocked.Or(ref _state, 1 << (int)kind);

    /// <summary>
    /// Determines whether the specified event kind has been recorded.
    /// </summary>
    /// <param name="kind">The kind of event to check for.</param>
    /// <returns><c>true</c> if the event was recorded; otherwise, <c>false</c>.</returns>
    public bool Has(TrackedEventKind kind)
        => (Volatile.Read(ref _state) & (1 << (int)kind)) != 0;

    /// <summary>
    /// Determines whether this envelope has reached a terminal state.
    /// </summary>
    /// <returns><c>true</c> if the envelope was consumed or failed; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
        => Has(TrackedEventKind.ConsumeCompleted) || Has(TrackedEventKind.ConsumeFailed);

    /// <summary>
    /// Determines whether this envelope has recorded a consume failure.
    /// </summary>
    /// <returns><c>true</c> if a <see cref="TrackedEventKind.ConsumeFailed"/> event was recorded.</returns>
    public bool HasFailed()
        => Has(TrackedEventKind.ConsumeFailed);

    /// <summary>
    /// Determines whether this envelope was dispatched but never received.
    /// This indicates the message had no subscribers and will never be consumed.
    /// </summary>
    /// <returns><c>true</c> if only <see cref="TrackedEventKind.Dispatched"/> events were recorded.</returns>
    public bool IsDispatchedOnly()
    {
        var state = Volatile.Read(ref _state);
        return state != 0
            && (state & ~(1 << (int)TrackedEventKind.Dispatched)) == 0;
    }
}
