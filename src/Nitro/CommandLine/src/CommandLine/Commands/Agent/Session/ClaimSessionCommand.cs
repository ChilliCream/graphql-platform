using ChilliCream.Nitro.CommandLine.Commands.Agent.Session.Options;
using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Session;

/// <summary>
/// Binds the resolved actor to this process's harness session. Zero-config
/// on Linux with Claude Code: an ancestor-walk self-identifies the session
/// and creates its row on first claim. Other platforms and harnesses need a
/// session row already created by <c>nitro agent hook</c> (a later bead).
/// </summary>
internal sealed class ClaimSessionCommand : Command
{
    public ClaimSessionCommand() : base("claim")
    {
        Description = "Bind the resolved actor to this process's harness session.";

        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<ForceRebindSessionOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent session claim", "agent session claim --actor codex --force-rebind");

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

        var actor = MailActor.TryResolve(
            parseResult.GetValue(Opt<MailActorOption>.Instance), environmentVariableProvider)
            ?? throw new ExitException(
                "No actor. Pass --actor or set NITRO_MAIL_ACTOR / NITRO_TASK_ACTOR.");
        var forceRebind = parseResult.GetValue(Opt<ForceRebindSessionOption>.Instance);

        var result = await sessions.SelfClaimAsync(actor, forceRebind, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(ToResult(result)));
            return ExitCodes.Success;
        }

        console.OkLine(
            result.Changed
                ? $"Claimed session '{result.Session.SessionId.EscapeMarkup()}' "
                    + $"({result.Session.Harness.EscapeMarkup()}) as '{actor.EscapeMarkup()}'."
                : $"Session '{result.Session.SessionId.EscapeMarkup()}' "
                    + $"({result.Session.Harness.EscapeMarkup()}) is already claimed by '{actor.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }

    private static SessionClaimResult ToResult(AgentSessionClaimResult result) => new(
        result.Session.Harness,
        result.Session.SessionId,
        result.Session.Host,
        result.Session.Pid,
        result.Session.ProcStart,
        result.Session.AgentName ?? "",
        result.Session.BindingKind,
        result.PreviousBindingKind,
        result.PreviousAgentName,
        result.Changed);

    /// <summary>
    /// The public shape of <c>agent session claim --output json</c>. Schema
    /// v6 breaking change: <see cref="ProcStart"/> is now the process's raw
    /// kernel start-tick count as a digit string (see <see
    /// cref="ChilliCream.Nitro.CommandLine.Services.Workspace.ProcStat.ReadStartTicks(int)"/>),
    /// not a DateTimeOffset.
    /// </summary>
    public sealed record SessionClaimResult(
        string Harness,
        string SessionId,
        string Host,
        int Pid,
        string ProcStart,
        string AgentName,
        string BindingKind,
        string PreviousBindingKind,
        string? PreviousAgentName,
        bool Changed);
}
