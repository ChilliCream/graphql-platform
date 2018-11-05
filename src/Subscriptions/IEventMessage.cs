namespace HotChocolate.Subscriptions
{
    /// <summary>
    /// The event message of the pub/sub system.
    /// </summary>
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
        string Payload { get; }


    }
}

