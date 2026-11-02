using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Claude;

/// <summary>
/// Adapts the <c>UserPromptSubmit</c> hook of a harness speaking Claude Code's
/// hook contract: resets the Stop gate's per-turn block budget and injects the
/// unread-mail digest. Payload JSON on stdin, harness-shaped JSON on stdout, always.
/// </summary>
internal sealed class UserPromptSubmitHookCommand : Command
{
    public UserPromptSubmitHookCommand(ClaudeHooksTemplate template) : base("user-prompt-submit")
    {
        Description = $"Adapt {AgentHarnessDisplay.Name(template.Harness)}'s UserPromptSubmit hook: "
            + "reset the block budget and inject the unread-mail digest.";

        this.SetHookAction(
            template.Harness,
            "UserPromptSubmit",
            (handler, payload, ct) => handler.HandleUserPromptSubmitAsync(payload, skipSessionFileLookup: false, ct));
    }
}
