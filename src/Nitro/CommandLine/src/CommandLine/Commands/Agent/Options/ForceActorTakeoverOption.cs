namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class ForceActorTakeoverOption : Option<bool>
{
    public ForceActorTakeoverOption() : base("--force")
    {
        Description = "Take over even when the source actor still has a live session";
    }
}
