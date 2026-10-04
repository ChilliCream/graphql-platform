namespace ChilliCream.Nitro.CommandLine.Services.Notify;

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
