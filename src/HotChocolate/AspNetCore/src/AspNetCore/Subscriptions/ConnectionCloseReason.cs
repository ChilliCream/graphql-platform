namespace HotChocolate.AspNetCore.Subscriptions;

public enum ConnectionCloseReason
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
    InternalServerError
}
