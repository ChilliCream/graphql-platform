using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Session;

internal sealed class ListSessionCommand : Command
{
    public ListSessionCommand() : base("list")
    {
        Description = "List live harness sessions in this workspace's agent database.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent session list");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var sessions = services.GetRequiredService<IAgentSessionRegistry>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var views = await sessions.ListAsync(cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<SessionRowResult>(views.Select(ToRow).ToArray()));
            return ExitCodes.Success;
        }

        if (views.Count == 0)
        {
            console.WriteLine("No live sessions.");
            return ExitCodes.Success;
        }

        foreach (var view in views)
        {
            var claim = view.Session.AgentName is { Length: > 0 } agentName
                ? $"  claimed by {agentName} ({view.Session.BindingKind})"
                : "  unclaimed";

            // Distinguishes "no endpoint to ping at all" (endpoint_kind
            // 'none') from "an endpoint the notifier has no transport for"
            // (last_ping_result 'unsupported', e.g. copilot-extension) from an
            // ordinary ping outcome: the same diagnostic signal `doctor`
            // surfaces.
            var ping = view.Session.LastPingResult is { Length: > 0 } lastPingResult
                ? $"  last ping {lastPingResult}"
                : "";

            console.WriteLine(
                $"{view.Session.Harness}  {view.Session.SessionId}  {view.State}{claim}"
                + $"  last beat {TaskDates.Format(view.Session.LastBeatAt)}{ping}");
        }

        return ExitCodes.Success;
    }

    internal static SessionRowResult ToRow(AgentSessionView view) => new(
        view.Session.Harness,
        view.Session.SessionId,
        view.Session.AgentName,
        view.Session.BindingKind,
        view.State,
        view.Session.Host,
        view.Session.Pid,
        view.Session.ProcStart,
        view.Session.Cwd,
        view.Session.WorkspacePath,
        view.Session.EndpointKind,
        view.Session.EndpointAddr,
        view.Session.StartedAt,
        view.Session.LastBeatAt,
        view.Session.LastPingAt,
        view.Session.LastPingResult,
        view.Session.LastPingDetail);

    /// <summary>
    /// The public shape of <c>agent session list --output json</c>. Schema
    /// v6 breaking change: <see cref="ProcStart"/> is now the process's raw
    /// kernel start-tick count as a digit string (see <see
    /// cref="ChilliCream.Nitro.CommandLine.Services.Workspace.ProcStat.ReadStartTicks(int)"/>),
    /// not a DateTimeOffset. A consumer that parsed it as a timestamp must
    /// stop doing so; it carries no wall-clock meaning and is only
    /// meaningful compared for exact equality against another value from
    /// the same field.
    /// </summary>
    public sealed record SessionRowResult(
        string Harness,
        string SessionId,
        string? AgentName,
        string BindingKind,
        string State,
        string Host,
        int Pid,
        string ProcStart,
        string Cwd,
        string WorkspacePath,
        string EndpointKind,
        string EndpointAddr,
        DateTimeOffset StartedAt,
        DateTimeOffset LastBeatAt,
        DateTimeOffset? LastPingAt,
        string? LastPingResult,
        string? LastPingDetail);
}
