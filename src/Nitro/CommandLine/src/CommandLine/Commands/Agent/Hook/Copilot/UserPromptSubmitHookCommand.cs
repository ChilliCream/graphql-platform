namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Copilot;

/// <summary>
/// Adapts Copilot CLI's <c>userPromptSubmitted</c> hooks-dir event. Always a
/// no-op response because Copilot drops this event's response body, so no digest
/// is attempted here (see <see cref="Services.Hook.ICopilotHookHandler.HandleUserPromptSubmitAsync"/>).
/// Payload JSON on stdin, <c>{}</c> on stdout, always.
/// </summary>
internal sealed class UserPromptSubmitHookCommand : Command
{
    public UserPromptSubmitHookCommand() : base("user-prompt-submit")
    {
        Description = "Adapt Copilot CLI's userPromptSubmitted hook: a documented no-op, "
            + "this event's response body is dropped by Copilot.";

        this.SetCopilotHookAction(
            (handler, payload, ct) => handler.HandleUserPromptSubmitAsync(payload, false, ct));
    }
}
