namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Ping.Options;

internal sealed class PingWorkerSessionIdOption : Option<string>
{
    public PingWorkerSessionIdOption() : base("--session-id")
    {
        Description = "Internal: the target session's id. Set by the notifier, not for direct use.";
        Required = true;
    }
}
