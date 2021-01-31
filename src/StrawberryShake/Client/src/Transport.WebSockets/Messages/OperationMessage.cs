using System;
using StrawberryShake.Transport.Subscriptions;

namespace StrawberryShake.Transport.WebSockets.Messages
{
    /// <summary>
    /// The <see cref="OperationMessage"/> is used as a data transport structure to send messages
    /// over a <see cref="ISocketProtocol"/> to the <see cref="SocketOperation"/>
    /// </summary>
    public class OperationMessage
    {
        /// <summary>
        /// Creates a new instance of a <see cref="OperationMessage"/>
        /// </summary>
        /// <param name="type">The type of the message</param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="type"/>
        /// </exception>
        public OperationMessage(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                throw ThrowHelper.Argument_IsNullOrEmpty(nameof(type));
            }

            Type = type;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="OperationMessage"/>
        /// </summary>
        /// <param name="type">The type of the message</param>
        /// <param name="id">The id of the operation</param>
        /// <exception cref="ArgumentException">
        /// Is thrown when <paramref name="type"/>
        /// </exception>
        public OperationMessage(string type, string id)
            : this(type)
        {
            Id = id;
        }

        /// <summary>
        /// The Id of the operation this message belongs to.
        /// </summary>
        public string? Id { get; }

        /// <summary>
        /// The identifier of the type of the message.
        /// </summary>
        public string Type { get; }
    }
}
