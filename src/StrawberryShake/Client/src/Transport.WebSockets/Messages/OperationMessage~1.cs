using System;
using StrawberryShake.Transport.Subscriptions;

namespace StrawberryShake.Transport.WebSockets.Messages
{
    /// <summary>
    /// The <see cref="OperationMessage"/> is used as a data transport structure to send messages
    /// over a <see cref="ISocketProtocol"/> to the <see cref="SocketOperation"/>
    /// </summary>
    public class OperationMessage<T>
        : OperationMessage
    {
        /// <summary>
        /// Creates a new instance of a <see cref="OperationMessage"/>
        /// </summary>
        /// <param name="type">The type of the message</param>
        /// <param name="payload">
        /// The payload of the message
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="type"/>
        /// </exception>
        public OperationMessage(string type, T payload)
            : base(type)
        {
            Payload = payload;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="OperationMessage"/>
        /// </summary>
        /// <param name="type">The type of the message</param>
        /// <param name="id">The id of the operation</param>
        /// <param name="payload">
        /// The payload of the message
        /// </param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="type"/>
        /// </exception>
        public OperationMessage(string type, string id, T payload)
            : base(type, id)
        {
            Payload = payload;
        }

        /// <summary>
        /// The payload of the message
        /// </summary>
        public T Payload { get; }
    }
}
