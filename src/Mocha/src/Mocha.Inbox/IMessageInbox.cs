using Mocha.Middlewares;

namespace Mocha.Inbox;

/// <summary>
/// Represents a message inbox for tracking processed messages
/// to ensure idempotent message consumption per consumer type.
/// </summary>
public interface IMessageInbox
{
    /// <summary>
    /// Checks if a message has already been processed by a specific consumer type.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="consumerType">The consumer type name that identifies the handler.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the message has been processed by the specified consumer; otherwise, <c>false</c>.
    /// </returns>
    ValueTask<bool> ExistsAsync(
        string messageId,
        string consumerType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically attempts to claim a message for processing by a specific consumer type
    /// by inserting it into the inbox. If another instance of the same consumer type has
    /// already claimed the message, returns <c>false</c>.
    /// </summary>
    /// <remarks>
    /// This method is the primary mechanism for preventing duplicate message processing under
    /// concurrent delivery. It uses an atomic INSERT (with conflict detection) so that exactly
    /// one caller wins the claim for a given <paramref name="envelope"/> and
    /// <paramref name="consumerType"/> combination. Callers should only process the message
    /// when this method returns <c>true</c>.
    /// <para/>
    /// Different consumer types can independently claim and process the same message, enabling
    /// fan-out scenarios where multiple handlers each process the same message exactly once.
    /// </remarks>
    /// <param name="envelope">The message envelope to claim.</param>
    /// <param name="consumerType">The consumer type name that identifies the handler.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the message was successfully claimed (inserted); <c>false</c> if it was
    /// already claimed by this consumer type.
    /// </returns>
    ValueTask<bool> TryClaimAsync(
        MessageEnvelope envelope,
        string consumerType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records a message as processed by a specific consumer type in the inbox.
    /// </summary>
    /// <param name="envelope">The message envelope to record.</param>
    /// <param name="consumerType">The consumer type name that identifies the handler.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask RecordAsync(
        MessageEnvelope envelope,
        string consumerType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cleans up processed inbox messages older than the specified age.
    /// </summary>
    /// <param name="maxAge">The maximum age of processed messages to retain.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of messages cleaned up.</returns>
    ValueTask<int> CleanupAsync(
        TimeSpan maxAge,
        CancellationToken cancellationToken);
}
