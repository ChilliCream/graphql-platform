namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class StaleAgentOption : Option<bool>
{
    public StaleAgentOption() : base("--stale")
    {
        Description = "Only show agents not seen in the last 30 days";
        Required = false;
    }
}
