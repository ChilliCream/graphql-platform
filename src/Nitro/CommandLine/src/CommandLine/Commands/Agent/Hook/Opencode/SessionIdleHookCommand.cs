using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Opencode;

/// <summary>
/// Handles an Opencode <c>session.idle</c> payload from stdin to refresh the session
/// heartbeat used by <see cref="ActorWakeDispatcher"/>. Writes a neutral JSON response.
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
