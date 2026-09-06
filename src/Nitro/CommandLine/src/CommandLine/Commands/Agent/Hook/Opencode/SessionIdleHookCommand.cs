namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Opencode;

/// <summary>
/// Adapts the opencode shim's <c>session.idle</c> event: reserves unread
/// mail for a later idle-channel delivery when the session has gone idle
/// with mail still undelivered. Payload JSON on stdin, <c>{}</c> on
/// stdout, always - the reserved digest, if any, is delivered out of band
/// rather than through this response.
/// </summary>
internal sealed class SessionIdleHookCommand : Command
{
    public SessionIdleHookCommand() : base("session-idle")
    {
        Description = "Adapt opencode's session.idle event: reserve unread mail for idle delivery.";

        this.SetOpencodeHookAction(
            (handler, payload, ct) => handler.HandleSessionIdleAsync(payload, false, ct));
    }
}
