using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Subscriptions
{
    /// <summary>
    /// <see cref="IEventStream{TMessage}" /> is a stream of internal messages
    /// that is used to process stream results.
    /// </summary>
    public interface IEventStream<out TMessage>
        : IAsyncEnumerable<TMessage>
    {
    }
}
