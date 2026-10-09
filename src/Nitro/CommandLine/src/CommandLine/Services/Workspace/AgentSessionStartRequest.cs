namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The harness-supplied fields for starting or resuming an agent session.
/// </summary>
internal sealed record AgentSessionStartRequest
{
    public required string Harness { get; init; }
    public required string SessionId { get; init; }
    public required string HarnessVersion { get; init; }
    public required string Cwd { get; init; }
    public required string WorkspacePath { get; init; }
    public required string EndpointKind { get; init; }

    /// <summary>
    /// Empty only when <see cref="EndpointKind"/> is <c>none</c>.
    /// </summary>
    public required string EndpointAddr { get; init; }

    /// <summary>
    /// The credential for endpoints that require one, or null when the
    /// endpoint has no credential.
    /// </summary>
    public string? EndpointSecret { get; init; }
}
