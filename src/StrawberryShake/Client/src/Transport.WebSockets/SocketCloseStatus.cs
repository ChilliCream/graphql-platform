namespace StrawberryShake.Transport
{
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
        InternalServerError
    }
}
