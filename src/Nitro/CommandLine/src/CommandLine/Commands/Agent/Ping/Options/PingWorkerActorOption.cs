namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Ping.Options;

internal sealed class PingWorkerActorOption : Option<string>
{
    public PingWorkerActorOption() : base("--actor")
    {
        Description = "Internal: the bound actor whose unread mail to digest. Set by the notifier, "
            + "not for direct use.";
        Required = true;
    }
}
