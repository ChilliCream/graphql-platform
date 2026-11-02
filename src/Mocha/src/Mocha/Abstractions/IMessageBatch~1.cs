namespace Mocha;

/// <summary>
/// An immutable batch of messages delivered to an <see cref="IBatchEventHandler{TEvent}"/>.
/// Provides indexed access, count, enumeration, and batch metadata.
/// </summary>
/// <typeparam name="TEvent">The type of event in the batch.</typeparam>
public interface IMessageBatch<out TEvent> : IMessageBatch, IReadOnlyList<TEvent>
{
    /// <summary>
    /// Gets the number of messages in the batch.
    /// </summary>
    new int Count { get; }

    /// <summary>
    /// Gets the typed consume context for a specific message in the batch.
    /// </summary>
    /// <param name="index">The zero-based index of the message.</param>
    /// <returns>The consume context for the message.</returns>
    new IConsumeContext<TEvent> GetContext(int index);

    /// <inheritdoc />
    IConsumeContext IMessageBatch.GetContext(int index) => GetContext(index);
}
