namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Ping.Options;

internal sealed class PingWorkerDeadlineOption : Option<DateTimeOffset>
{
    public PingWorkerDeadlineOption() : base("--deadline")
    {
        Description = "Internal: the absolute UTC deadline this attempt's digest and transport work must "
            + "finish before, fixed when the notifier acquired the lease. Set by the notifier, not for "
            + "direct use.";
        Required = true;
    }
}
