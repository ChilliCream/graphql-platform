namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Ping.Options;

internal sealed class PingActorArgument : Argument<string>
{
    public PingActorArgument() : base("actor")
    {
        Description = "The recipient agent name to ping";
    }
}
