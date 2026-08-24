using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Agents;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

internal sealed class ListAgentCommand : Command
{
    public ListAgentCommand() : base("list")
    {
        Description = "List live agent participants: one row per harness session, including unbound sessions.";

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

        var participants = await sessionRegistry.ListParticipantsAsync(cancellationToken);

        if (role is not null)
        {
            var normalizedRole = AgentRole.Normalize(role);
            participants = participants.Where(p => p.MatchesRole(normalizedRole)).ToArray();
        }

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<AgentListRowResult>(participants.Select(ToRow).ToArray()));
            return ExitCodes.Success;
        }

        if (participants.Count == 0)
        {
            console.WriteLine("No live agent participants.");
            return ExitCodes.Success;
        }

        foreach (var participant in participants)
        {
            console.WriteLine(FormatLine(participant));
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// Renders the human-readable line for one participant: the bound actor
    /// (or <see cref="AgentParticipantRow.UnboundLabel"/>), the live state,
    /// the harness with its exact version when captured, the mutable role
    /// when set, and when it was last heard from. Full session id,
    /// cwd/workspace, and endpoint/host diagnostics are machine output only
    /// (<c>--output json</c>); <c>agent session list</c> is the lower-level
    /// surface for those on a human terminal.
    /// </summary>
    private static string FormatLine(AgentSessionParticipant participant)
    {
        var session = participant.Session;
        var actor = session.AgentName ?? AgentParticipantRow.UnboundLabel;
        var versionSuffix = session.HarnessVersion.Length > 0 ? $" {session.HarnessVersion}" : "";
        var roleSuffix = session.Role.Length > 0 ? $"  role {session.Role}" : "";

        return $"{actor}  {participant.State}  {session.Harness}{versionSuffix}{roleSuffix}"
            + $"  last heard {TaskDates.Format(session.LastBeatAt)}";
    }

    private static AgentListRowResult ToRow(AgentSessionParticipant participant) => new(
        participant.Session.Harness,
        participant.Session.SessionId,
        participant.Session.AgentName,
        participant.Session.Role,
        participant.Session.HarnessVersion,
        participant.State,
        participant.Session.StartedAt,
        participant.Session.LastBeatAt,
        participant.Session.Cwd,
        participant.Session.WorkspacePath,
        participant.Session.Host,
        participant.Session.EndpointKind,
        participant.Session.EndpointAddr);

    public sealed record AgentListRowResult(
        string Harness,
        string SessionId,
        string? Actor,
        string Role,
        string HarnessVersion,
        string State,
        DateTimeOffset StartedAt,
        DateTimeOffset LastHeardAt,
        string Cwd,
        string WorkspacePath,
        string Host,
        string EndpointKind,
        string EndpointAddr);
}
