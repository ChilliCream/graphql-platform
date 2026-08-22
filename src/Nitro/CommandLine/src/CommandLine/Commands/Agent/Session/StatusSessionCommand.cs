using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Session;

internal sealed class StatusSessionCommand : Command
{
    public StatusSessionCommand() : base("status")
    {
        Description = "Show the live harness sessions claimed by the resolved actor.";

        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent session status", "agent session status --actor codex");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var sessions = services.GetRequiredService<IAgentSessionRegistry>();
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var actor = MailActor.Resolve(
            parseResult.GetValue(Opt<MailActorOption>.Instance), environmentVariableProvider);

        var views = await sessions.ListAsync(cancellationToken);
        var mine = views.Where(v => v.Session.AgentName == actor).ToArray();
        var online = mine.Any(v => v.State == AgentSessionState.Online);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(
                new ObjectResult(
                    new SessionStatusResult(
                        actor, online, mine.Select(ListSessionCommand.ToRow).ToArray())));
            return ExitCodes.Success;
        }

        if (mine.Length == 0)
        {
            console.WriteLine($"{actor}  offline");
            return ExitCodes.Success;
        }

        foreach (var view in mine)
        {
            console.WriteLine(
                $"{actor}  {view.State}  {view.Session.Harness}  {view.Session.SessionId}"
                + $"  last beat {TaskDates.Format(view.Session.LastBeatAt)}");
        }

        return ExitCodes.Success;
    }

    public sealed record SessionStatusResult(
        string Actor, bool Online, IReadOnlyList<ListSessionCommand.SessionRowResult> Sessions);
}
