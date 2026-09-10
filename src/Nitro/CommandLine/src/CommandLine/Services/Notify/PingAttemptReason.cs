namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The stable reasons one <see cref="IPingSessionExecutor"/> attempt can
/// end in, spanning both the Claude peer and Codex thread transports.
/// </summary>
internal enum PingAttemptReason
{
    Ok,
    Unsupported,
    EndpointGone,

    /// <summary>
    /// The Claude peer endpoint required authentication this process could
    /// not supply, or the matching key was missing or invalid.
    /// </summary>
    InvalidAuth,

    /// <summary>
    /// The Claude peer socket connect failed with <c>SocketError.AccessDenied</c>.
    /// </summary>
    AccessDenied,
    Timeout,
    CapacityDropped,

    /// <summary>
    /// Any other transport failure: a Codex spawn failure or nonzero exit,
    /// a generic Claude peer I/O or protocol failure, or an unexpected
    /// exception this attempt caught.
    /// </summary>
    TransportError
}
