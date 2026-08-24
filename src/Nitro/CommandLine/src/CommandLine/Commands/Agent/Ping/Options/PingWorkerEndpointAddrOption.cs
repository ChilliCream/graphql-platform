namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Ping.Options;

internal sealed class PingWorkerEndpointAddrOption : Option<string>
{
    public PingWorkerEndpointAddrOption() : base("--endpoint-addr")
    {
        Description = "Internal: the target endpoint address. Set by the notifier, not for direct use.";
        Required = true;
    }
}
