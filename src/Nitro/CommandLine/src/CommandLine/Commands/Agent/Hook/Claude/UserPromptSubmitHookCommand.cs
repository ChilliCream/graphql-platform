using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

/// <summary>
/// Adapts Claude Code's <c>UserPromptSubmit</c> hook: resets the Stop
/// gate's per-turn block budget and injects the unread-mail digest.
/// Payload JSON on stdin, harness-shaped JSON on stdout, always.
/// </summary>
internal sealed class UserPromptSubmitHookCommand : Command
{
    public UserPromptSubmitHookCommand() : base("user-prompt-submit")
    {
        Description = "Adapt Claude Code's UserPromptSubmit hook: reset the block budget and "
            + "inject the unread-mail digest.";

        Options.Add(Opt<DryRunHookOption>.Instance);

        this.SetHookAction(
            "UserPromptSubmit",
            (handler, payload, dryRun, ct) => handler.HandleUserPromptSubmitAsync(payload, dryRun, ct));
    }
}
