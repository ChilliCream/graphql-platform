using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

internal sealed class ListAgentCommand : Command
{
    public ListAgentCommand() : base("list")
    {
        Description = "List the actors this workspace knows, with their session when they have one.";

        Options.Add(Opt<RoleAgentOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent list", "agent list --role \"orchestrator\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var sessionRegistry = services.GetRequiredService<IAgentSessionRegistry>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var role = parseResult.GetValue(Opt<RoleAgentOption>.Instance);

        // Actors are the unit: an actor allocated by `agent login` has no
        // session until a hook binds one, and must still be listed.
        var agents = services.GetRequiredService<IAgentRegistry>();
        var identities = await sessionRegistry.ListIdentitiesAsync(cancellationToken);
        var byActor = identities.ToDictionary(view => view.Identity.Actor, StringComparer.Ordinal);
        var rows = (await agents.ListAsync(role: null, staleBefore: null, cancellationToken))
            .Select(agent => new AgentListRow(agent, byActor.GetValueOrDefault(agent.Name)))
            .ToArray();

        if (role is not null)
        {
            var normalizedRole = AgentRole.Normalize(role);
            rows = rows.Where(row => row.Role == normalizedRole).ToArray();
        }

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<AgentListRowResult>(rows.Select(ToRow).ToArray()));
            return ExitCodes.Success;
        }

        if (rows.Length == 0)
        {
            console.WriteLine("No actors.");
            return ExitCodes.Success;
        }

        foreach (var row in rows)
        {
            console.WriteLine(FormatLine(row));
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// Renders one durable session identity: actor, online/offline state,
    /// harness, optional role, and last-seen time. Connection diagnostics
    /// remain available in machine output when the session is online.
    /// </summary>
    private static string FormatLine(AgentListRow row)
    {
        var session = row.View?.Participant?.Session;
        var versionSuffix = session?.HarnessVersion.Length > 0 ? $" {session.HarnessVersion}" : "";
        var roleSuffix = row.Role.Length > 0 ? $"  role {row.Role}" : "";
        var harness = row.View?.Identity.Harness ?? "no session";

        return $"{row.Actor}  {row.State}  {harness}{versionSuffix}{roleSuffix}"
            + $"  last heard {TaskDates.Format(row.LastSeenAt)}";
    }

    /// <summary>
    /// One actor, with the durable session identity bound to it when a hook
    /// has bound one.
    /// </summary>
    private sealed record AgentListRow(AgentRecord Agent, AgentSessionIdentityView? View)
    {
        public string Actor => Agent.Name;

        public string Role => View?.Identity.Role is { Length: > 0 } role ? role : Agent.Role;

        public string State => View?.State ?? "offline";

        public DateTimeOffset LastSeenAt => View?.LastSeenAt ?? Agent.LastSeenAt;
    }

    private static AgentListRowResult ToRow(AgentListRow row) => new(
        row.Actor,
        row.Role,
        row.View?.Identity.Harness ?? "",
        row.View?.Identity.SessionId ?? "",
        row.View?.Online ?? false,
        row.State,
        row.View?.Participant?.Session.HarnessVersion ?? "",
        row.View?.Participant?.Session.StartedAt,
        row.LastSeenAt,
        row.View?.Participant?.Session.Cwd,
        row.View?.Participant?.Session.WorkspacePath,
        row.View?.Participant?.Session.Host,
        row.View?.Participant?.Session.EndpointKind,
        row.View?.Participant?.Session.EndpointAddr);

    public sealed record AgentListRowResult(
        string Actor,
        string Role,
        string Harness,
        string SessionId,
        bool Online,
        string State,
        string HarnessVersion,
        DateTimeOffset? StartedAt,
        DateTimeOffset LastHeardAt,
        string? Cwd,
        string? WorkspacePath,
        string? Host,
        string? EndpointKind,
        string? EndpointAddr);
}
