namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Internal settlement abstraction over the SDK's two processor event-args types
/// (<c>ProcessMessageEventArgs</c> and <c>ProcessSessionMessageEventArgs</c>) so the
/// acknowledgement middleware can complete, abandon, or dead-letter the current message
/// through one uniform call site regardless of whether the endpoint is session-bound. Not
/// part of the public surface; user code settles via the SDK args returned by
/// <see cref="AzureServiceBusContextExtensions.GetAzureServiceBusEventArgs"/> /
/// <see cref="AzureServiceBusContextExtensions.GetAzureServiceBusSessionEventArgs"/>.
/// </summary>
internal interface IAzureServiceBusMessageActions
{
    /// <summary>
    /// Completes the current message, removing it from the broker.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Abandons the current message, releasing its lock so the broker can redeliver it.
    /// </summary>
    /// <param name="propertiesToModify">Application properties to apply on the abandoned message.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task AbandonAsync(
        IDictionary<string, object>? propertiesToModify = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves the current message to the entity's dead-letter sub-queue.
    /// </summary>
    /// <param name="deadLetterReason">The dead-letter reason carried with the message.</param>
    /// <param name="deadLetterErrorDescription">Optional human-readable description.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task DeadLetterAsync(
        string deadLetterReason,
        string? deadLetterErrorDescription = null,
        CancellationToken cancellationToken = default);
}
