namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Outcome classifications for a Claude peer send attempt.
/// </summary>
internal enum ClaudePeerSendReason
{
    // The complete protocol payload was written to the validated local
    // endpoint.
    Ok,
    Unsupported,
    EndpointGone,

    /// <summary>
    /// The required authentication key is missing, invalid, or unavailable
    /// for the target process.
    /// </summary>
    InvalidAuth,

    /// <summary>
    /// A peer socket operation reported <c>SocketError.AccessDenied</c>.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// Any other transport, protocol, or local I/O failure not covered by a
    /// more specific reason above.
    /// </summary>
    TransportError,

    /// <summary>
    /// The complete protocol payload was written to an endpoint that requires
    /// authentication and did not reject it, but the protocol sends no
    /// acknowledgement, so delivery is not confirmed.
    /// </summary>
    Unconfirmed,

    /// <summary>
    /// The endpoint transport did not connect, take the payload, and pass its rejection window
    /// within the time budget, so delivery is unknown.
    /// </summary>
    Timeout
}
