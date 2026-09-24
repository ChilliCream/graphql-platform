using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Delegates to <see cref="IAgentStore"/> except for
/// <see cref="ClaimAnnouncementAsync"/>, which throws <see cref="InvalidOperationException"/>.
/// </summary>
internal sealed class ThrowingAnnouncementAgentStore(IAgentStore inner) : IAgentStore
{
    public Task<AgentRow> LoginAsync(CancellationToken cancellationToken)
        => inner.LoginAsync(cancellationToken);

    public Task<AgentSessionStartResult> StartSessionAsync(
        AgentSessionStartRequest request, CancellationToken cancellationToken)
        => inner.StartSessionAsync(request, cancellationToken);

    public Task<bool> TouchSessionAsync(string harness, string sessionId, CancellationToken cancellationToken)
        => inner.TouchSessionAsync(harness, sessionId, cancellationToken);

    public Task<bool> TouchAsync(string name, CancellationToken cancellationToken)
        => inner.TouchAsync(name, cancellationToken);

    public Task<bool> EndSessionAsync(string harness, string sessionId, CancellationToken cancellationToken)
        => inner.EndSessionAsync(harness, sessionId, cancellationToken);

    public Task<AgentRow?> SetRoleAsync(string name, string role, CancellationToken cancellationToken)
        => inner.SetRoleAsync(name, role, cancellationToken);

    public Task<AgentRow?> FindAsync(string name, CancellationToken cancellationToken)
        => inner.FindAsync(name, cancellationToken);

    public Task<AgentRow?> FindBySessionAsync(string harness, string sessionId, CancellationToken cancellationToken)
        => inner.FindBySessionAsync(harness, sessionId, cancellationToken);

    public Task<IReadOnlyList<AgentRow>> ListAsync(CancellationToken cancellationToken)
        => inner.ListAsync(cancellationToken);

    public Task<bool> SetEndpointAsync(
        string name,
        string endpointKind,
        string endpointAddr,
        string? endpointSecret,
        CancellationToken cancellationToken)
        => inner.SetEndpointAsync(name, endpointKind, endpointAddr, endpointSecret, cancellationToken);

    public Task<int> ResetBlockBudgetAsync(string name, CancellationToken cancellationToken)
        => inner.ResetBlockBudgetAsync(name, cancellationToken);

    public Task<int> IncrementBlockBudgetAsync(string name, CancellationToken cancellationToken)
        => inner.IncrementBlockBudgetAsync(name, cancellationToken);

    public Task<bool> TryClaimPingCooldownAsync(
        string name, TimeSpan cooldown, string attemptId, CancellationToken cancellationToken)
        => inner.TryClaimPingCooldownAsync(name, cooldown, attemptId, cancellationToken);

    public Task WritePingResultAsync(
        string name, string attemptId, string result, string? detail, CancellationToken cancellationToken)
        => inner.WritePingResultAsync(name, attemptId, result, detail, cancellationToken);

    public Task ArmAnnouncementAsync(string name, CancellationToken cancellationToken)
        => inner.ArmAnnouncementAsync(name, cancellationToken);

    public Task<bool> ClaimAnnouncementAsync(string name, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Simulated announcement-claim failure.");

    public Task<bool> IsAnnouncementPendingAsync(string name, CancellationToken cancellationToken)
        => inner.IsAnnouncementPendingAsync(name, cancellationToken);

    public Task RearmIdlePushAsync(string name, CancellationToken cancellationToken)
        => inner.RearmIdlePushAsync(name, cancellationToken);

    public Task<bool> ClaimIdlePushAsync(string name, CancellationToken cancellationToken)
        => inner.ClaimIdlePushAsync(name, cancellationToken);

    public Task<bool> RecordHarnessVersionAsync(
        string name, string harnessVersion, CancellationToken cancellationToken)
        => inner.RecordHarnessVersionAsync(name, harnessVersion, cancellationToken);

    public Task<bool> DeleteAsync(string name, CancellationToken cancellationToken)
        => inner.DeleteAsync(name, cancellationToken);

    public Task<int> DeleteOfflineAsync(CancellationToken cancellationToken)
        => inner.DeleteOfflineAsync(cancellationToken);
}
