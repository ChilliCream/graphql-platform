using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

internal sealed class ListAgentCommand : Command
{
    /// <summary>
    /// The staleness threshold applied by <c>--stale</c>, mirroring
    /// <c>agent tasks stale</c>'s default of 30 days.
    /// </summary>
    private const int StaleDays = 30;

    public ListAgentCommand() : base("list")
    {
        Description = "List registered agents.";

        Options.Add(Opt<RoleAgentOption>.Instance);
        Options.Add(Opt<StaleAgentOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent list", "agent list --role \"backend\"", "agent list --stale");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var registry = services.GetRequiredService<IAgentRegistry>();
        var sessionRegistry = services.GetRequiredService<IAgentSessionRegistry>();
        var activityReader = services.GetRequiredService<IClaudeSessionActivityReader>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var role = parseResult.GetValue(Opt<RoleAgentOption>.Instance);
        var stale = parseResult.GetValue(Opt<StaleAgentOption>.Instance);
        var staleBefore = stale ? timeProvider.GetUtcNow() - TimeSpan.FromDays(StaleDays) : (DateTimeOffset?)null;

        var agents = await registry.ListAsync(role, staleBefore, cancellationToken);
        var sessions = await sessionRegistry.ListAsync(cancellationToken);
        var rows = agents
            .Select(agent => (Agent: agent, Presence: ComputePresence(agent, sessions, activityReader)))
            .ToArray();

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(
                new ListResult<AgentListRowResult>(rows.Select(r => ToRow(r.Agent, r.Presence)).ToArray()));
            return ExitCodes.Success;
        }

        if (rows.Length == 0)
        {
            console.WriteLine("No registered agents.");
            return ExitCodes.Success;
        }

        foreach (var (agent, presence) in rows)
        {
            var roleSuffix = agent.Role.Length > 0 ? $"  role {agent.Role}" : "";
            var clientSuffix = agent.Client.Length > 0 ? $"  client {agent.Client}" : "";
            var implicitSuffix = agent.Implicit ? "  (implicit)" : "";

            console.WriteLine(
                $"{agent.Name}  {FormatPresence(presence)}{roleSuffix}{clientSuffix}{implicitSuffix}"
                + $"  registered {TaskDates.Format(agent.RegisteredAt)}"
                + $"  last seen {TaskDates.Format(agent.LastSeenAt)}");
        }

        return ExitCodes.Success;
    }

    private static AgentPresence ComputePresence(
        AgentRecord agent, IReadOnlyList<AgentSessionView> sessions, IClaudeSessionActivityReader activityReader)
    {
        var mine = sessions.Where(v => v.Session.AgentName == agent.Name).ToArray();
        return AgentPresence.Compute(mine, activityReader);
    }

    /// <summary>
    /// Renders a presence for the human-readable line: the state, the
    /// Claude activity read-through in parentheses when known (only ever set
    /// for a single online claude-code session, see
    /// <see cref="AgentPresence.Compute"/>), or the live session count in
    /// parentheses when the agent's sessions disagree on state (the
    /// "surfaced, not hidden" multi-session conflict).
    /// </summary>
    private static string FormatPresence(AgentPresence presence)
    {
        var activitySuffix = presence.Activity is { Length: > 0 } activity ? $" ({activity})" : "";
        var conflictSuffix = presence.Conflicted ? $" ({presence.SessionCount} sessions)" : "";

        return presence.State + activitySuffix + conflictSuffix;
    }

    private static AgentListRowResult ToRow(AgentRecord agent, AgentPresence presence) => new(
        agent.Name,
        agent.Role,
        agent.Client,
        agent.Implicit,
        agent.RegisteredAt,
        agent.LastSeenAt,
        presence.State,
        presence.Conflicted,
        presence.SessionCount,
        presence.EndpointKind,
        presence.EndpointAddr,
        presence.Activity);

    public sealed record AgentListRowResult(
        string Name,
        string Role,
        string Client,
        bool Implicit,
        DateTimeOffset RegisteredAt,
        DateTimeOffset LastSeenAt,
        string Presence,
        bool PresenceConflict,
        int SessionCount,
        string? EndpointKind,
        string? EndpointAddr,
        string? Activity);
}
