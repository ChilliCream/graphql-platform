using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Opencode;

/// <summary>
/// Adapts the opencode shim's <c>session.idle</c> event: touches the
/// session's heartbeat so it stays live for <see cref="ActorWakeDispatcher"/>,
/// the sole claimant of the idle-push gate. Payload JSON on stdin, <c>{}</c>
/// on stdout, always - this event never delivers anything of its own; a
/// push, if any, arrives out of band through the dispatcher.
/// </summary>
internal sealed class SessionIdleHookCommand : Command
{
    public SessionIdleHookCommand() : base("session-idle")
    {
        Description = "Adapt opencode's session.idle event: refresh the session heartbeat.";

        this.SetOpencodeHookAction(
            (handler, payload, ct) => handler.HandleSessionIdleAsync(payload, false, ct));
    }
}
