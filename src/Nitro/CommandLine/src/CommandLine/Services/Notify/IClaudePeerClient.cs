namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Sends one machine-generated digest to a local Claude Code interactive
/// session through its advertised peer-protocol endpoint.
/// </summary>
internal interface IClaudePeerClient
{
    Task<ClaudePeerSendOutcome> SendAsync(
        int pid,
        string sessionId,
        string message,
        CancellationToken cancellationToken);
}

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
    // to the validated local endpoint, which is the behavior live-verified
    // on 2.1.226 and 2.1.241.
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

/// <summary>
/// The typed outcome of one <see cref="IClaudePeerClient.SendAsync"/> call:
/// a stable <see cref="Reason"/>, whether the same attempt is worth
/// retrying, and a bounded, safe-to-log <see cref="Detail"/> that never
/// carries raw exception text or file contents.
/// </summary>
internal sealed record ClaudePeerSendOutcome(ClaudePeerSendReason Reason, bool Retryable, string? Detail)
{
    public static ClaudePeerSendOutcome Ok { get; } =
        new(ClaudePeerSendReason.Ok, Retryable: false, Detail: null);

    public static ClaudePeerSendOutcome Unsupported { get; } =
        new(ClaudePeerSendReason.Unsupported, Retryable: false, Detail: null);

    public static ClaudePeerSendOutcome EndpointGone { get; } =
        new(ClaudePeerSendReason.EndpointGone, Retryable: false, Detail: null);

    public static ClaudePeerSendOutcome InvalidAuth { get; } =
        new(ClaudePeerSendReason.InvalidAuth, Retryable: false, "peer authentication unavailable or invalid");

    public static ClaudePeerSendOutcome AccessDenied { get; } =
        new(ClaudePeerSendReason.AccessDenied, Retryable: false, "peer socket connect denied (access-denied)");

    public static ClaudePeerSendOutcome TransportError(string detail) =>
        new(ClaudePeerSendReason.TransportError, Retryable: true, detail);
}
