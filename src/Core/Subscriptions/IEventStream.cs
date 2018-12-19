using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    /// <summary>
    /// The event stream represents the subscription to an event
    /// as a stream of messages.
    /// </summary>
    public interface IEventStream
        : IDisposable
    {
        /// <summary>
        /// Defines if this stream is completed.
        /// A completed event stream does not yield any new events.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Reads the next event from the current event stream.
        /// </summary>
        Task<IEventMessage> ReadAsync();

        /// <summary>
        /// Reads the next event from the current event stream.
        /// </summary>
        Task<IEventMessage> ReadAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Completes the event stream and deletes
        /// the pub/sub system subscription.
        /// </summary>
        Task CompleteAsync();
    }
}
