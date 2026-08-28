namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The stable reasons a <see cref="IClaudePeerClient.SendAsync"/> call can
/// end in. Distinct from <see cref="ClaudePeerSendOutcome.Retryable"/>: a
/// reason is a fixed classification, retryability is a caller-facing hint
/// about whether the same attempt is worth repeating.
/// </summary>
internal enum ClaudePeerSendReason
{
    // Claude Code's raw peer endpoint closes with EOF and no inline
    // delivery receipt. Ok means the complete protocol payload was written
    // to the validated local endpoint.
    Ok,
    Unsupported,
    EndpointGone,

    /// <summary>
    /// The endpoint requires authentication this process could not supply:
    /// no key belongs to the target process's exact generation, a key file
    /// fails its permission check, or Windows requires a key that is
    /// absent. Never a downgrade from a mismatch to unauthenticated.
    /// </summary>
    InvalidAuth,

    /// <summary>
    /// The peer socket connect itself failed with <c>SocketError.AccessDenied</c>.
    /// This reflects only what the OS reported for that one connect call; it
    /// is not proof of any particular sandboxing mechanism.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// Any other transport, protocol, or local I/O failure not covered by a
    /// more specific reason above.
    /// </summary>
    TransportError
}
