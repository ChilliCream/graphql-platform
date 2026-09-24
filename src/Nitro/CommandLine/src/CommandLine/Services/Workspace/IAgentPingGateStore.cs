namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Manages one shared transport lease for each agent.
/// </summary>
internal interface IAgentPingGateStore
{
    /// <summary>
    /// Atomically claims the gate for <paramref name="agent"/>, stealing it if the
    /// current holder's lease has already expired. Returns false, claiming nothing,
    /// when the gate is currently held by an unexpired attempt.
    /// </summary>
    Task<bool> TryAcquireAsync(
        string agent,
        string attemptId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the matching gate's expiry to <paramref name="now"/> plus <paramref name="leaseDuration"/>.
    /// Returns false when the attempt does not own the gate or its lease has expired.
    /// </summary>
    Task<bool> TryRenewAsync(
        string agent,
        string attemptId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases the matching agent's gate only when it still belongs to
    /// <paramref name="attemptId"/>; a missing or reassigned gate is unchanged.
    /// </summary>
    Task ReleaseAsync(string agent, string attemptId, CancellationToken cancellationToken);
}
