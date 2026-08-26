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
        Description = "List current actors and their durable harness sessions.";

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

        var identities = await sessionRegistry.ListIdentitiesAsync(cancellationToken);

        if (role is not null)
        {
            var normalizedRole = AgentRole.Normalize(role);
            identities = identities.Where(view => view.Identity.Role == normalizedRole).ToArray();
        }

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<AgentListRowResult>(identities.Select(ToRow).ToArray()));
            return ExitCodes.Success;
        }

        if (identities.Count == 0)
        {
            console.WriteLine("No actors.");
            return ExitCodes.Success;
        }

        foreach (var identity in identities)
        {
            console.WriteLine(FormatLine(identity));
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// Renders one durable session identity: actor, online/offline state,
    /// harness, optional role, and last-seen time. Connection diagnostics
    /// remain available in machine output when the session is online.
    /// </summary>
    private static string FormatLine(AgentSessionIdentityView view)
    {
        var identity = view.Identity;
        var session = view.Participant?.Session;
        var versionSuffix = session?.HarnessVersion.Length > 0 ? $" {session.HarnessVersion}" : "";
        var roleSuffix = identity.Role.Length > 0 ? $"  role {identity.Role}" : "";
        var lastSeenAt = view.LastSeenAt;

        return $"{identity.Actor}  {view.State}  {identity.Harness}{versionSuffix}{roleSuffix}"
            + $"  last heard {TaskDates.Format(lastSeenAt)}";
    }

    private static AgentListRowResult ToRow(AgentSessionIdentityView view) => new(
        view.Identity.Actor,
        view.Identity.Role,
        view.Identity.Harness,
        view.Identity.SessionId,
        view.Online,
        view.State,
        view.Participant?.Session.HarnessVersion ?? "",
        view.Participant?.Session.StartedAt,
        view.LastSeenAt,
        view.Participant?.Session.Cwd,
        view.Participant?.Session.WorkspacePath,
        view.Participant?.Session.Host,
        view.Participant?.Session.EndpointKind,
        view.Participant?.Session.EndpointAddr);

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
