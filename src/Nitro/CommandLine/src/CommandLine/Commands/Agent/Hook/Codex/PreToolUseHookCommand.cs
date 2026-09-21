namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Codex;

/// <summary>
/// Adapts Codex CLI's <c>PreToolUse</c> hook: injects the telemetry skill pointer once per session.
/// Payload JSON on stdin, harness-shaped JSON on stdout, always.
/// </summary>
internal sealed class PreToolUseHookCommand : Command
{
    public PreToolUseHookCommand() : base("pre-tool-use")
    {
        Description = "Adapt Codex CLI's PreToolUse hook: inject the telemetry skill pointer once per session.";

        this.SetCodexHookAction(
            "PreToolUse",
            (handler, payload, ct) => handler.HandlePreToolUseAsync(payload, false, ct));
    }
}
