namespace Mocha.Middlewares;

/// <summary>
/// Represents the context for an inbound message flowing through the receive middleware pipeline,
/// combining message metadata with execution capabilities and mutable headers.
/// </summary>
public interface IReceiveContext : IMessageContext, IExecutionContext
{
    /// <summary>
    /// Gets the mutable header collection for the received message, allowing middleware to modify
    /// headers during processing.
    /// </summary>
    new IHeaders Headers { get; }

    /// <inheritdoc />
    IReadOnlyHeaders IMessageContext.Headers => Headers;

    /// <summary>
    /// Populates this context from the given transport-level message envelope.
    /// </summary>
    /// <param name="envelope">
    /// The envelope containing the raw message data and transport metadata.
    /// </param>
    void SetEnvelope(MessageEnvelope envelope);
}
