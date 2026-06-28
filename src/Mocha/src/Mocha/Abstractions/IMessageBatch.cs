namespace Mocha;

/// <summary>
/// An immutable batch of messages delivered to a batch handler.
/// </summary>
public interface IMessageBatch
{
    /// <summary>
    /// Gets the reason this batch was dispatched.
    /// </summary>
    BatchCompletionMode CompletionMode { get; }

    /// <summary>
    /// Gets the number of messages in the batch.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the consume context for a specific message in the batch.
    /// </summary>
    /// <param name="index">The zero-based index of the message.</param>
    /// <returns>The consume context for the message.</returns>
    IConsumeContext GetContext(int index);
}
