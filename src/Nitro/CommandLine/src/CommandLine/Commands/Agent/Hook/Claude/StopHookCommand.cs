using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

/// <summary>
/// Adapts Claude Code's <c>Stop</c> hook: blocks the turn from ending while
/// unread mail not yet delivered on the gate channel exists, honoring
/// reentrancy and the per-turn block budget. Payload JSON on stdin,
/// <c>{decision, reason}</c> or <c>{}</c> on stdout, always.
/// </summary>
internal sealed class StopHookCommand : Command
{
    public StopHookCommand() : base("stop")
    {
        Description = "Adapt Claude Code's Stop hook: block the turn while unread mail is undelivered.";

        Options.Add(Opt<DryRunHookOption>.Instance);

        this.SetHookAction(
            "Stop",
            (handler, payload, dryRun, ct) => handler.HandleStopAsync(payload, dryRun, ct));
    }
}
