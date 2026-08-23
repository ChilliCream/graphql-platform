using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Codex;

/// <summary>
/// Adapts Codex CLI's <c>UserPromptSubmit</c> hooks.json event: injects the
/// unread-mail digest via <c>additionalContext</c> (spike S1 confirmed Codex
/// supports the same contract Claude Code does). Payload JSON on stdin,
/// harness-shaped JSON on stdout, always.
/// </summary>
internal sealed class UserPromptSubmitHookCommand : Command
{
    public UserPromptSubmitHookCommand() : base("user-prompt-submit")
    {
        Description = "Adapt Codex CLI's UserPromptSubmit hook: inject the unread-mail digest.";

        Options.Add(Opt<DryRunHookOption>.Instance);

        this.SetCodexHookAction(
            "UserPromptSubmit",
            (handler, payload, dryRun, ct) => handler.HandleUserPromptSubmitAsync(payload, dryRun, ct));
    }
}
