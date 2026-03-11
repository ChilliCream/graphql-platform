namespace StrawberryShake.Transport.WebSockets.Messages;

/// <summary>
/// The kinds of an operation message
/// </summary>
public enum OperationMessageType
{
    Error,
    Data,
    Cancelled,
    Complete
}
