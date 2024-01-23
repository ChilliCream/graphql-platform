namespace HotChocolate.AspNetCore.Subscriptions;

public enum ConnectionCloseReason
{
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
