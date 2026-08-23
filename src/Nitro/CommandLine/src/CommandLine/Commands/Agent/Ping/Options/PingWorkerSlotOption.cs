namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Ping.Options;

internal sealed class PingWorkerSlotOption : Option<int>
{
    public PingWorkerSlotOption() : base("--slot")
    {
        Description = "Internal: the ping_leases slot already acquired for this attempt. Set by the "
            + "notifier, not for direct use.";
        Required = true;
    }
}
