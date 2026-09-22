namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Manages one shared transport lease for each harness, session id, and host tuple.
/// </summary>
internal interface ISessionPingGateStore
{
    /// <summary>
    /// Atomically claims the gate for <paramref name="generation"/>,
    /// stealing it if the current holder's lease has already expired.
    /// Returns false, claiming nothing, when the gate is currently held by
    /// an unexpired attempt.
    /// </summary>
    Task<bool> TryAcquireAsync(
        AgentSessionGeneration generation,
        string attemptId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the matching gate's expiry to <paramref name="now"/> plus <paramref name="leaseDuration"/>.
    /// Returns false when the attempt does not own the gate or its lease has expired.
    /// </summary>
    Task<bool> TryRenewAsync(
        AgentSessionGeneration generation,
        string attemptId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases the matching session gate only when it still belongs to
    /// <paramref name="attemptId"/>; a missing or reassigned gate is unchanged.
    /// </summary>
    Task ReleaseAsync(
        AgentSessionGeneration generation,
        string attemptId,
        CancellationToken cancellationToken);
}
