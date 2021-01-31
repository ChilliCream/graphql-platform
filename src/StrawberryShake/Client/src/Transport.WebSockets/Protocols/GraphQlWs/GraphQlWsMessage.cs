using System.Buffers;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.Http.Subscriptions
{
    /// <summary>
    /// The <see cref="GraphQlWsMessage"/> is used as a data transport structure to send messages
    /// over a <see cref="GraphQlWsProtocol"/>
    /// </summary>
    internal ref struct GraphQlWsMessage
    {
        /// <summary>
        /// The Id of the operation this message belongs to.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// The identifier of the type of the message.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// The payload of the message
        /// </summary>
        public ReadOnlySequence<byte> Payload { get; set; }
    }
}
