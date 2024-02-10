using StrawberryShake.Properties;

namespace StrawberryShake.Transport.WebSockets.Messages;

/// <summary>
/// The <see cref="ErrorOperationMessage"/> is used to transport any connection error to the
/// socket operation
/// </summary>
public class ErrorOperationMessage : OperationMessage<IClientError>
{
    public ErrorOperationMessage(string message)
        : base(OperationMessageType.Error, new ClientError(message))
    {
    }

    public ErrorOperationMessage(IClientError error)
        : base(OperationMessageType.Error, error)
    {
    }

    /// <summary>
    /// Default connection error
    /// <remarks>
    /// Connection initialization failed. Could not connect to server
    /// </remarks>
    /// </summary>
    public static readonly ErrorOperationMessage ConnectionInitializationError =
        new(Resources.ErrorOperationMessage_ConnectionInitializationError);
}
