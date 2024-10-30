namespace StrawberryShake.Transport.WebSockets.Messages;

/// <summary>
/// The kinds of a operation message
/// </summary>
public enum OperationMessageType
{
    Error,
    Data,
    Cancelled,
    Complete,
}
