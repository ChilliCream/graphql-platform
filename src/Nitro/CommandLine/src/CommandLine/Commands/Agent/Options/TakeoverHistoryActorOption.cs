namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class TakeoverHistoryActorOption : Option<string?>
{
    public TakeoverHistoryActorOption() : base("--actor")
    {
        Description = "Filter to takeovers involving this actor";
        Required = false;
    }
}
