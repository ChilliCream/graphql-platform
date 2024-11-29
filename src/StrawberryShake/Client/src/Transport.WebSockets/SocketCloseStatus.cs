namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Represents the different status that a socket can be closed by
/// </summary>
public enum SocketCloseStatus
{
    None,
    NormalClosure,
    EndpointUnavailable,
    ProtocolError,
    InvalidMessageType,
    InvalidPayloadData,
    PolicyViolation,
    MessageTooBig,
    MandatoryExtension,
    InternalServerError,
}
