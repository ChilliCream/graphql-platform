namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Opencode;

/// <summary>
/// Adapts the opencode shim's <c>session.created</c> event: registers this
/// session's presence row and endpoint details. Payload JSON on stdin,
/// <c>{}</c> on stdout, always.
/// </summary>
internal sealed class SessionCreatedHookCommand : Command
{
    public SessionCreatedHookCommand() : base("session-created")
    {
        Description = "Adapt opencode's session.created event: register this session's presence row.";

        this.SetOpencodeHookAction(
            (handler, payload, ct) => handler.HandleSessionCreatedAsync(payload, false, ct));
    }
}
