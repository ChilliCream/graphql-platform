using System;

namespace HotChocolate.Subscriptions
{
    /// <summary>
    /// The event message of the pub/sub system.
    /// </summary>
    [Obsolete("Use HotChocolate.Subscriptions.IEventStream<TMessage>.")]
    public interface IEventMessage
    {
        /// <summary>
        /// Gets the event that yielded this message.
        /// </summary>
        /// <value>The event.</value>
        IEventDescription Event { get; }

        /// <summary>
        /// Gets the message payload.
        /// </summary>
        /// <value></value>
        object Payload { get; }
    }
}
