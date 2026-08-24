using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Ping.Options;

internal sealed class PingWorkerEndpointKindOption : Option<string>
{
    public PingWorkerEndpointKindOption() : base("--endpoint-kind")
    {
        Description = "Internal: the target endpoint kind. Set by the notifier, not for direct use.";
        Required = true;
        AcceptOnlyFromAmong(AgentSessionEndpointKind.CodexThread, AgentSessionEndpointKind.ClaudePeer);
    }
}
