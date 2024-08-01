namespace StrawberryShake.Transport.WebSockets.Messages;

/// <summary>
/// The <see cref="OperationMessage"/> is used as a data transport structure to send messages
/// over a <see cref="ISocketProtocol"/> to the <see cref="SocketOperation"/>
/// </summary>
public abstract class OperationMessage<T> : OperationMessage
{
    /// <summary>
    /// Creates a new instance of a <see cref="OperationMessage"/>
    /// </summary>
    /// <param name="type">The type of the message</param>
    /// <param name="payload">
    /// The payload of the message
    /// </param>
    protected OperationMessage(OperationMessageType type, T payload)
        : base(type)
    {
        Payload = payload;
    }

    /// <summary>
    /// The payload of the message
    /// </summary>
    public T Payload { get; }
}
