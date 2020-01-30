using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Subscriptions
{
    /// <summary>
    /// The event stream represents the subscription to an event
    /// as a stream of messages.
    /// </summary>
    public interface IEventStream : IAsyncEnumerable<IEventMessage>
    {
        /// <summary>
        /// Completes the event stream and deletes the pub/sub system subscription.
        /// </summary>
        ValueTask CompleteAsync(
            CancellationToken cancellationToken = default);
    }
}
