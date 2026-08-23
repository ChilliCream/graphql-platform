namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Ping.Options;

internal sealed class PingWorkerHarnessOption : Option<string>
{
    public PingWorkerHarnessOption() : base("--harness")
    {
        Description = "Internal: the target session's harness. Set by the notifier, not for direct use.";
        Required = true;
    }
}
