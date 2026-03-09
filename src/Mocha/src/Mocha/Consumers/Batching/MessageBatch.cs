using System.Collections;

namespace Mocha;

/// <summary>
/// An immutable batch of messages delivered to an <see cref="IBatchEventHandler{TEvent}"/>.
/// Implements <see cref="IReadOnlyList{T}"/> for LINQ and foreach support.
/// </summary>
/// <typeparam name="TEvent">The type of event in the batch.</typeparam>
internal sealed class MessageBatch<TEvent> : IMessageBatch<TEvent>
{
    internal MessageBatch(List<BufferedEntry<TEvent>> entries, BatchCompletionMode completionMode)
    {
        if (entries.Count == 0)
        {
            throw new ArgumentException("Batch must contain at least one message.", nameof(entries));
        }

        Entries = entries;
        CompletionMode = completionMode;
    }

    internal List<BufferedEntry<TEvent>> Entries { get; }

    /// <summary>
    /// Gets the reason this batch was dispatched.
    /// </summary>
    public BatchCompletionMode CompletionMode { get; }

    /// <summary>
    /// Gets the number of messages in the batch.
    /// </summary>
    public int Count => Entries.Count;

    /// <summary>
    /// Gets the message at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the message.</param>
    public TEvent this[int index] => Entries[index].Context.Message;

    /// <summary>
    /// Gets the consume context for a specific message in the batch.
    /// </summary>
    /// <param name="index">The zero-based index of the message.</param>
    /// <returns>The consume context for the message.</returns>
    public IConsumeContext<TEvent> GetContext(int index) => Entries[index].Context;

    public IEnumerator<TEvent> GetEnumerator()
    {
        for (var i = 0; i < Entries.Count; i++)
        {
            yield return Entries[i].Context.Message;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
