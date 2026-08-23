using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Copilot;

/// <summary>
/// Adapts Copilot CLI's <c>userPromptSubmitted</c> hooks-dir event. Always a
/// no-op response: spike S5 (redo, perles-net-k3j.4) live-verified that this
/// event's response body is silently dropped by Copilot 1.0.80, so no digest
/// is attempted here (see <see cref="Services.Hook.ICopilotHookHandler.HandleUserPromptSubmitAsync"/>).
/// Payload JSON on stdin, <c>{}</c> on stdout, always.
/// </summary>
internal sealed class UserPromptSubmitHookCommand : Command
{
    public UserPromptSubmitHookCommand() : base("user-prompt-submit")
    {
        Description = "Adapt Copilot CLI's userPromptSubmitted hook: a documented no-op, "
            + "this event's response body is dropped by Copilot.";

        Options.Add(Opt<DryRunHookOption>.Instance);

        this.SetCopilotHookAction(
            (handler, payload, dryRun, ct) => handler.HandleUserPromptSubmitAsync(payload, dryRun, ct));
    }
}
