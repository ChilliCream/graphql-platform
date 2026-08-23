namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Ping.Options;

internal sealed class PingWorkerAttemptOption : Option<string>
{
    public PingWorkerAttemptOption() : base("--attempt")
    {
        Description = "Internal: the attempt id this worker's result write is conditioned on. Set by "
            + "the notifier, not for direct use.";
        Required = true;
    }
}
