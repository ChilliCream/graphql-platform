namespace Mocha;

/// <summary>
/// Represents the consume context for a typed batch of messages.
/// </summary>
/// <typeparam name="TMessage">The message type contained in the batch.</typeparam>
public interface IBatchConsumeContext<out TMessage> :
    IBatchConsumeContext,
    IConsumeContext<IMessageBatch<TMessage>>
{
    /// <summary>
    /// Gets the typed message batch.
    /// </summary>
    new IMessageBatch<TMessage> Message { get; }

    /// <inheritdoc />
    IMessageBatch IConsumeContext<IMessageBatch>.Message => Message;
}
