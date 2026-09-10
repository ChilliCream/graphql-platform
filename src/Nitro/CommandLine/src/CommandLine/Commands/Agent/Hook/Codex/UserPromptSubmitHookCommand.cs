namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Codex;

/// <summary>
/// Adapts Codex CLI's <c>UserPromptSubmit</c> hooks.json event: injects the
/// unread-mail digest via <c>additionalContext</c>. Payload JSON on stdin,
/// harness-shaped JSON on stdout, always.
/// </summary>
internal sealed class UserPromptSubmitHookCommand : Command
{
    public UserPromptSubmitHookCommand() : base("user-prompt-submit")
    {
        Description = "Adapt Codex CLI's UserPromptSubmit hook: inject the unread-mail digest.";

        this.SetCodexHookAction(
            "UserPromptSubmit",
            (handler, payload, ct) => handler.HandleUserPromptSubmitAsync(payload, false, ct));
    }
}
