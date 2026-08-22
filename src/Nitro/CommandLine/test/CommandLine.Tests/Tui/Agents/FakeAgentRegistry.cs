using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// An in-memory <see cref="IAgentRegistry"/> exercising the surface
/// <see cref="ChilliCream.Nitro.CommandLine.Tui.Agents.AgentsMode"/> consumes
/// (<see cref="ListAsync"/>) and the surface
/// <see cref="ChilliCream.Nitro.CommandLine.Tui.Agents.AgentDetailModel"/>
/// consumes (<see cref="GetAsync"/>). Every other member throws
/// <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class FakeAgentRegistry : IAgentRegistry
{
    public List<AgentRecord> Agents { get; } = [];

    public Task<IReadOnlyList<AgentRecord>> ListAsync(
        string? role,
        DateTimeOffset? staleBefore,
        CancellationToken cancellationToken)
    {
        IEnumerable<AgentRecord> query = Agents;

        if (role is not null)
        {
            query = query.Where(a => a.Role == role);
        }

        if (staleBefore is { } threshold)
        {
            query = query.Where(a => a.LastSeenAt < threshold);
        }

        var result = query.OrderBy(a => a.Name, StringComparer.Ordinal).ToList();

        return Task.FromResult<IReadOnlyList<AgentRecord>>(result);
    }

    public Task<AgentRecord> RegisterAsync(string name, string role, string client, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<AgentRecord> TouchAsync(string name, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<AgentRecord?> GetAsync(string name, CancellationToken cancellationToken)
        => Task.FromResult(Agents.FirstOrDefault(a => a.Name == name));

    public Task<AgentRecord> EnsureImplicitAsync(string name, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
